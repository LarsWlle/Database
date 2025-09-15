using Database.Util;

namespace Database.Network.Packets.Handshake;

public class ClientboundKeyExchangePacket(Encryption encryption) : OutboundPacket {
    public new static ushort Id { get; } = 2;

    public override byte[] Package() => [
        ..encryption.GetPublicKey().Length.ParseToBytes(),
        ..encryption.GetPublicKey()
    ];
}