using Database.Authentication;

namespace Database.Network.Packets.Handshake;

public class ClientboundLoginResponsePacket(LoginFailedReason reason) : OutboundPacket {
    public new static ushort Id { get; } = 6;
    public override byte[] Package() => [(byte) reason];
}