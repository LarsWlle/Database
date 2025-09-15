namespace Database.Network.Packets.Util;

public class ClientboundBuildupPacket : OutboundPacket {
    public new static ushort Id { get; } = 100;
    public override byte[] Package() => [];
}