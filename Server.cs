using System.Net;
using System.Net.Sockets;
using Database.Authentication;
using Database.Network;
using Database.Network.Packets;
using Database.Network.Packets.Data;
using Database.Network.Packets.Enums;
using Database.Network.Packets.Handshake;
using Database.ResourceManager;
using Database.Storage;
using Database.Storage.Implementation;
using Database.Storage.Schemas;
using Database.Util;

namespace Database;

public class Server {
    public const uint MAX_PACKET_SIZE = 2048;
    public const uint PROTOCOL_VERSION = 1;

    public ResourceUI ResourceUI;
    public readonly List<User> Users;

    private readonly TcpListener _listener;
    private int _port;
    private Thread _thread;
    private readonly List<Client> _clients;
    public readonly Encryption Encryption;

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

    public static readonly string[] FORBIDDEN_DATAFILE_REQUESTS = ["internal/credentials"];

    public Dictionary<uint, Func<InboundPacket>> PacketHandlers { get; set; } = new() {
        // Handshake (0 - 9)
        { ServerboundKeyExchangePacket.Id, () => new ServerboundKeyExchangePacket() },
        { ServerboundWaitingForInfoPacket.Id, () => new ServerboundWaitingForInfoPacket() },
        { ServerboundLoginPacket.Id, () => new ServerboundLoginPacket() },

        // Data (10 - 100)
        { ServerboundRegisterSchemaPacket.Id, () => new ServerboundRegisterSchemaPacket() },
        { ServerboundAddRecordPacket.Id, () => new ServerboundAddRecordPacket() },

        // Other
        { ServerboundDisconnectPacket.Id, () => new ServerboundDisconnectPacket() }
    };

    public Server(int port) {
        _listener = new TcpListener(IPAddress.Any, port);
        _port = port;
        DataFiles = new Dictionary<string, DataFile<DataRecord>>();
        CredentialsFile = new CredentialsStorageFile();
        CredentialsFile.LoadIndex();
        _clients = [];
        Encryption = new Encryption();
        ResourceUI = new ResourceUI(this);
        Users = CredentialsFile.All<CredentialsSchema>().Select(credschem => new User {
            Id = credschem.Id,
            Username = credschem.Username,
            Password = credschem.Password,
            PasswordSalt = credschem.Salt
        }).ToList();
        Logger.Debug($"ServerPublicKey={string.Join(", ", Encryption.GetPublicKey())}");

        LoadDataFiles();
    }

    public void Listen() {
        IsRunning = true;
        _thread = new Thread(() => {
            try {
                _listener.Start();
                Logger.Info("Ready for new clients!");

                try {
                    while (IsRunning) {
                        TcpClient client = _listener.AcceptTcpClient();
                        Client clientListener = new(client, this, Encryption.GetPublicKey(), Encryption);
                        clientListener.Init();
                        _clients.Add(clientListener);
                        Logger.Info("New client has connected!");
                    }
                } catch (SocketException e) when (e.SocketErrorCode == SocketError.Interrupted) {
                    Logger.Info("Listener got interrupted, handling exception!");
                } finally {
                    _listener.Stop();
                    Logger.Info("Stopping server");
                }
            } catch (Exception e) {
                Logger.Error($"Error on listening thread: {e}");
            }
        });

        _thread.Start();
    }

    public void FindAndHandlePacket(Client client, uint id, byte[] data) {
        if (!PacketHandlers.TryGetValue(id, out Func<InboundPacket>? value)) {
            Logger.Warning($"Tried handling a packet with id {id}, but isn't registered!");
            return;
        }

        InboundPacket handler = value.Invoke();
        Logger.Debug($"Handling Packet: {handler.GetType().Name}");
        handler.Handle(client, data);
    }

    public void RemoveClient(Client client) {
        _clients.Remove(client);
    }

    public void Stop() {
        Logger.Info("Received a command to stop this server!");
        int size = _clients.Count;
        _clients.RemoveAll(client => {
            client.SendPacket(new ClientboundDisconnectPacket(DisconnectReason.SERVER_STOPPED));
            client.CloseConnection();
            return true;
        });

        Logger.Info($"Disconnected {size} clients!");
        IsRunning = false;
        _thread.Interrupt();
        Logger.Info("Listening thread has been interrupted and will no longer wait for new clients!");

        _listener.Stop();
        Logger.Info("Stopped TCP Listener");
    }

    public User? GetUser(string username) {
        return Users.SingleOrDefault(u => u.Username == username);
    }

    public void RegisterDataFile<T>(string name, SchemaDefinitionBuilder<T> definition) where T : DataRecord, new() {
        DataFile<T> file = new("data", name, definition);
        if (!file.Exists()) file.Create();

        DataFiles.Add(name, (file as DataFile<DataRecord>)!);
        Logger.Debug($"New data file has been registered (Name={name})");
    }

    public void LoadDataFiles() {
        foreach (string file in Directory.GetFiles("data", "*", SearchOption.AllDirectories)) {
            if (file.EndsWith(".index")) return;
            FileStream reader = File.OpenRead(file);
            byte[] schemaLength = new byte[4];
            reader.ReadExactly(schemaLength, 0, 4);
            int bytesToRead = schemaLength.ParseToNumber<int>();

            byte[] data = new byte[bytesToRead];
            reader.ReadExactly(data, 0, bytesToRead);
            SchemaDefinitionBuilder<DataRecord> definition = SchemaDefinitionBuilder<DataRecord>.FromBytes(data);

            DataFile<DataRecord> datafile = new("data", file, definition);
            DataFiles.Add(file.Split('\\').Last(), datafile);
            Logger.Debug($"Loaded data file: {file}");
        }
    }

    public Client? GetCorrespondingClient(User user) {
        return _clients.SingleOrDefault(c => c.User != null && c.User.Id == user.Id);
    }
}