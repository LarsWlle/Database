using DatabaseTesting.Packets.Enums;

namespace Database.Network.Packets;

public class ServerboundDisconnectPacket : InboundPacket {
    public new static ushort Id { get; } = ushort.MaxValue;

    public override void Handle(Client client, byte[] packet) {
        client.HandleDisconnect((DisconnectReason) packet[0]);
    }
}