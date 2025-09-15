namespace Database.Network.Packets.Util;

public class ClientboundBuildupReleasePacket : OutboundPacket {
    public new static ushort Id { get; } = 101;
    public override byte[] Package() => [];
}