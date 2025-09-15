using System.Net.Sockets;
using System.Reflection;
using Database.Authentication;
using Database.Network.Packets.Handshake;
using Database.Network.Packets.Util;
using Database.Util;
using DatabaseTesting.Packets.Enums;

namespace Database.Network;

public class Client(TcpClient client, Server server, byte[] serverPubKey, Encryption encryption) {
    private byte[] _clientPublicKey = [];
    private bool _hasExchangedKeys;
    private Thread _thread;
    private bool _running;
    private bool _hasAuthenticated;
    private User? _user;
    private bool _holdup;

    public readonly Server Server = server;

    public void Init() {
        this._running = true;
        this._thread = new Thread(() => {
            try {
                Logger.Info("Created a new thread for new client");
                this.Listen();
            } catch (Exception e) {
                Logger.Error($"Couldn't listen to client: {e}");
            }
        });
        this._thread.Start();
    }

    private void Listen() {
        NetworkStream stream = client.GetStream();
        while (client.Connected && this._running) {
            byte[] buffer = new byte[Server.MAX_PACKET_SIZE];
            if (!stream.CanRead || !stream.DataAvailable) {
                Task.Delay(10);
                continue;
            }

            try {
                int bytesRead = stream.Read(buffer, 0, (int) Server.MAX_PACKET_SIZE);
                if (bytesRead == 0) continue;
                byte[] length = buffer.Take(2).ToArray();
                byte[] data = buffer.Skip(2).Take(length.ParseToNumber<ushort>()).ToArray();

                if (!this._hasExchangedKeys) {
                    int packetId = data.Take(2).ToArray().ParseToNumber<int>();
                    if (packetId != ServerboundKeyExchangePacket.Id) continue;
                    new ServerboundKeyExchangePacket().Handle(this, data.Skip(2).ToArray());
                    this.SendPacket(new ClientboundKeyExchangePacket(encryption), true);
                    this.SendPacket(new ClientboundServerInformationPacket());
                    continue;
                }

                int nonceLength = data.Skip(2).Take(4).ParseToNumber<int>(); // Skip 2 (length), take 4 (nonce length)
                byte[] nonce = data.Skip(6).Take(nonceLength).ToArray();

                int actualDataLength = data.Skip(6 + nonceLength).Take(4).ParseToNumber<int>(); // Skip length + nonce length + nonce, take 4 (text len)
                byte[] actualData = data.Skip(10 + nonceLength).Take(actualDataLength).ToArray();

                int tagLength = data.Skip(10 + nonceLength + actualDataLength).Take(4).ParseToNumber<int>();
                byte[] tag = data.Skip(14 + nonceLength + actualDataLength).Take(tagLength).ToArray();

                Logger.Debug($"EncryptionDump: nonceLen={nonceLength}, actualDataLength={actualDataLength}, tagLength={tagLength}, clientPublicKey={string.Join(", ", this._clientPublicKey)}");

                byte[] decryptedData = encryption.DecryptPacket(this._clientPublicKey, nonce, actualData, tag);
                int packetId2 = decryptedData.Take(2).ParseToNumber<int>();

                Logger.Packet("Packet received!", false, 0, packetId2);

                if (server.AuthlessPackets.Contains((ushort) packetId2) && !this._hasAuthenticated) return;

                Logger.Debug($"Received a packet from client (OriginalLength={length.ParseToNumber<int>()}, OriginalBuffer=[{string.Join(", ", data)}], " +
                             $"Length={decryptedData.Length}, OriginalBuffer=[{string.Join(", ", decryptedData)}])");
                this.Server.FindAndHandlePacket(this, (uint) packetId2, decryptedData.Skip(2).ToArray());
            } catch (Exception e) {
                Logger.Error($"Error while reading packet: {e.Message}");
                Logger.Error($"Stacktrace:\n{e.StackTrace}");
            }
        }

        Logger.Info("Client connection has been destroyed");
    }

    public void SetClientPublicKey(byte[] clientPublicKey) {
        this._clientPublicKey = clientPublicKey;
        this._hasExchangedKeys = true;
        Logger.Debug("Exchange has happened");
    }

    public void SendPacket(OutboundPacket packet, bool ignoreEncryption = false) {
        byte[] data = packet.Package();
        ushort id = (ushort) (packet.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Static)?.GetValue(packet) ?? 0);
        byte[] resultData;
        if (ignoreEncryption) {
            resultData = data;
        } else {
            (byte[] cipherText, byte[] nonce, byte[] tag) actualData = encryption.EncryptPacket(this._clientPublicKey, data);
            resultData = [
                ..actualData.nonce.Length.ParseToBytes(), ..actualData.nonce,
                ..actualData.cipherText.Length.ParseToBytes(), ..actualData.cipherText,
                ..actualData.tag.Length.ParseToBytes(), ..actualData.tag
            ];
        }

        byte[] result = [
            ..((ushort) (2 + resultData.Length)).ParseToBytes(),
            ..id.ParseToBytes(),
            ..resultData
        ];
        client.GetStream().Write(result, 0, result.Length);
        Logger.Packet("Packet sent!", true, 0, id);
    }

    public void HandleDisconnect(DisconnectReason reason) {
        Logger.Info($"Client connection will be terminated: {reason}");
        this._running = false;
        this._thread.Interrupt();
        this.Server.RemoveClient(this);
    }

    public LoginFailedReason AttemptLogin(string username, byte[] password) {
        User? user = this.Server.GetUser(username);
        if (user == null) return LoginFailedReason.INVALID_USERNAME;

        bool isCorrectPassword = encryption.CompareHash(user.Password, password, user.PasswordSalt);
        if (!isCorrectPassword) return LoginFailedReason.INVALID_PASSWORD;
        this._user = user;
        this._hasAuthenticated = true;
        Logger.Info($"New Login (Username={user.Username}, EndPoint={client.Client.RemoteEndPoint})");

        return LoginFailedReason.SUCCESSFUL;
    }

    public void SetHoldup(bool holdup) {
        switch (holdup) {
            case true when this._holdup:
                Logger.Warning("Setting a holdup while a holdup is already active, two holdups at once?");
                break;
            case false when !this._holdup:
                Logger.Warning("Deleting a holdup while no holdup is active");
                break;
            case true when !this._holdup:
                this.SendPacket(new ClientboundBuildupPacket());
                break;
            case false when this._holdup:
                this.SendPacket(new ClientboundBuildupReleasePacket());
                break;
        }

        this._holdup = holdup;
    }

    public void CloseConnection() {
        this._running = false;
        client.Close();
    }
}