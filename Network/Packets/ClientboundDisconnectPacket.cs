using Database.Network.Packets.Enums;

namespace Database.Network.Packets;

public class ClientboundDisconnectPacket(DisconnectReason reason) : OutboundPacket {
    public new static ushort Id { get; } = ushort.MaxValue - 1;
    public override byte[] Package() => [(byte) reason];
}