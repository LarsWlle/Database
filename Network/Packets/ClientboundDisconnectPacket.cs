using DatabaseTesting.Packets.Enums;

namespace Database.Network.Packets;

public class ClientboundDisconnectPacket(DisconnectReason reason) : OutboundPacket {
    public override byte[] Package() => [(byte) reason];
}