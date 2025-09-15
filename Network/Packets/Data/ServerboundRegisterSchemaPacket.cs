using System.Text;
using Database.Storage.Schemas;
using Database.Util;

namespace Database.Network.Packets.Data;

public class ServerboundRegisterSchemaPacket : InboundPacket {
    public new static ushort Id { get; } = 10;

    public override void Handle(Client client, byte[] packet) {
        ushort nameLength = packet.Take(2).ParseToNumber<ushort>();
        string name = Encoding.UTF8.GetString(packet.Skip(2).Take(nameLength).ToArray());
        ushort builderLength = packet.Skip(nameLength + 2).Take(2).ParseToNumber<ushort>();
        SchemaDefinitionBuilder<DataRecord> builder = SchemaDefinitionBuilder<DataRecord>.FromBytes(packet.Skip(4 + nameLength).Take(builderLength).ToArray());
        client.Server.RegisterDataFile(name, builder);
    }
}