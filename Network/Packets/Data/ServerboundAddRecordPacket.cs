using System.Text;
using Database.Network.Packets.Enums;
using Database.Storage;
using Database.Storage.Schemas;
using Database.Util;

namespace Database.Network.Packets.Data;

public class ServerboundAddRecordPacket : InboundPacket {
    public new static ushort Id { get; } = 12;

    /*
        +-------+--------------------+----------------------+
        | Index |        Name        |        Length        |
        +-------+--------------------+----------------------+
        |     0 | TransactionId      | 2                    |
        |     1 | RegisterNameLength | 1                    |
        |     2 | RegisterName       | #RegisterNameLength  |
        |     3 | RecordLength       | 2                    |
        |     4 | Record             | DataRecordSerializer |
        +-------+--------------------+----------------------+
     */
    public override void Handle(Client client, byte[] packet) {
        ushort transactionId = packet.Take(2).ParseToNumber<ushort>();

        ushort nameLength = packet.Skip(2).Take(2).ParseToNumber<ushort>();
        string name = Encoding.UTF8.GetString(packet.Skip(4).Take(nameLength).ToArray());

        if (Server.FORBIDDEN_DATAFILE_REQUESTS.Contains(name)) {
            this.Respond(transactionId, TransactionResponseCode.SchemaEditingNotAllowed, client);
            return;
        }

        ushort recordLength = packet.Skip(4 + nameLength).ParseToNumber<ushort>();
        byte[] recordAsBytes = packet.Skip(6 + nameLength).Take(recordLength).ToArray();

        Dictionary<string, DataFile<DataRecord>> datafiles = client.Server.DataFiles;
        if (!datafiles.TryGetValue(name, out DataFile<DataRecord>? file)) {
            this.Respond(transactionId, TransactionResponseCode.SchemaNotFound, client);
            return;
        }

        DataType type = file.Definition.Build()[file.Definition.GetIndex()].Type;
        byte[] indexValue = file.Definition.IndexedValueFromByteArray(recordAsBytes);
        object parsed = file.ParseObject(indexValue, type);
        if (file.DoesIndexExist(parsed)) {
            this.Respond(transactionId, TransactionResponseCode.IndexAlreadyExists, client);
            return;
        }

        file.Add(recordAsBytes);
        this.Respond(transactionId, TransactionResponseCode.Added, client);
    }

    private void Respond(ushort transactionId, TransactionResponseCode err, Client client) {
        client.SendPacket(new ClientboundTransactionResponsePacket(transactionId, err));
    }
}