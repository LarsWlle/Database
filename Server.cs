using System.Net;
using System.Net.Sockets;
using Database.Authentication;
using Database.Network;
using Database.Network.Packets;
using Database.Network.Packets.Data;
using Database.Network.Packets.Handshake;
using Database.Storage;
using Database.Storage.Implementation;
using Database.Storage.Schemas;
using Database.Util;
using DatabaseTesting.Packets.Enums;

namespace Database;

public class Server {
    public const uint MAX_PACKET_SIZE = 2048;
    public const uint PROTOCOL_VERSION = 1;

    private readonly TcpListener _listener;
    private int _port;
    private Thread _thread;
    private readonly List<Client> _clients;
    private readonly Encryption _encryption;
    private readonly List<User> _users;

    #region DataFiles
    public Dictionary<string, DataFile<DataRecord>> DataFiles { get; set; }

    public readonly CredentialsStorageFile CredentialsFile;
    #endregion

    public bool IsRunning { get; private set; }

    public List<ushort> AuthlessPackets = [
        ServerboundKeyExchangePacket.Id,
        ServerboundLoginPacket.Id,
        ServerboundDisconnectPacket.Id
    ];

    public Dictionary<uint, Func<InboundPacket>> PacketHandlers { get; set; } = new() {
        // Handshake (0 - 9)
        { ServerboundKeyExchangePacket.Id, () => new ServerboundKeyExchangePacket() },
        { ServerboundWaitingForInfoPacket.Id, () => new ServerboundWaitingForInfoPacket() },
        { ServerboundLoginPacket.Id, () => new ServerboundLoginPacket() },

        // Data (10 - 100)
        { ServerboundRegisterSchemaPacket.Id, () => new ServerboundRegisterSchemaPacket() },

        // Other
        { ServerboundDisconnectPacket.Id, () => new ServerboundDisconnectPacket() }
    };

    public Server(int port) {
        this._listener = new TcpListener(IPAddress.Any, port);
        this._port = port;
        this.CredentialsFile = new CredentialsStorageFile();
        this.CredentialsFile.LoadIndex();
        this._clients = [];
        this._encryption = new Encryption();
        this._users = [
            new User {
                Id = 0,
                Username = "root",
                Password = "pwd",
                PasswordSalt = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]
            }
        ]; // TODO: Fix from file
        Logger.Debug($"ServerPublicKey={string.Join(", ", this._encryption.GetPublicKey())}");
    }

    public void Listen() {
        this.IsRunning = true;
        this._thread = new Thread(() => {
            try {
                this._listener.Start();
                Logger.Info("Ready for new clients!");

                try {
                    while (this.IsRunning) {
                        TcpClient client = this._listener.AcceptTcpClient();
                        Client clientListener = new(client, this, this._encryption.GetPublicKey(), this._encryption);
                        clientListener.Init();
                        this._clients.Add(clientListener);
                        Logger.Info("New client has connected!");
                    }
                } finally {
                    this._listener.Stop();
                    Logger.Info("Stopping server");
                }
            } catch (Exception e) {
                Logger.Error($"Error on listening thread: {e}");
            }
        });

        this._thread.Start();
    }

    public void FindAndHandlePacket(Client client, uint id, byte[] data) {
        if (!this.PacketHandlers.TryGetValue(id, out Func<InboundPacket>? value)) return;
        InboundPacket handler = value.Invoke();
        Logger.Debug($"Handling Packet: {handler.GetType().Name}");
        handler.Handle(client, data);
    }

    public void RemoveClient(Client client) {
        this._clients.Remove(client);
    }

    public void Stop() {
        Logger.Info("Received a command to stop this server!");
        int size = this._clients.Count;
        this._clients.RemoveAll(client => {
            client.SendPacket(new ClientboundDisconnectPacket(DisconnectReason.SERVER_STOPPED));
            client.CloseConnection();
            return true;
        });

        Logger.Info($"Disconnected {size} clients!");
        this.IsRunning = false;
        this._thread.Interrupt();
        Logger.Info("Listening thread has been interrupted and will no longer wait for new clients!");

        this._listener.Stop();
        Logger.Info("Stopped TCP Listener");

        Environment.Exit(0);
    }

    public User? GetUser(string username) {
        return this._users.SingleOrDefault(u => u.Username == username);
    }

    public void RegisterDataFile<T>(string name, SchemaDefinitionBuilder<T> definition) where T : DataRecord, new() {
        DataFile<T> file = new("data", name, definition);
        if (!file.Exists()) file.Create();

        this.DataFiles.Add(name, (file as DataFile<DataRecord>)!);
        Logger.Debug($"New data file has been registered (Name={name})");
    }
}