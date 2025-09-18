using Database.Util;

namespace Database.Network.Packets.Handshake;

public class ClientboundServerInformationPacket : OutboundPacket {
    public new static ushort Id { get; } = 4;

    public override byte[] Package() => [
        ..((ushort) Server.MAX_PACKET_SIZE).ParseToBytes(), // Max Packet Size
        ..((ushort) Server.PROTOCOL_VERSION).ParseToBytes() // Protocol Version
    ];
}