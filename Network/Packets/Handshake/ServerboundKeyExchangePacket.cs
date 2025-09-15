using Database.Util;

namespace Database.Network.Packets.Handshake;

public class ServerboundKeyExchangePacket : InboundPacket {
    public new static ushort Id { get; } = 1;

    public override void Handle(Client client, byte[] data) {
        int len = data.Take(4).ToArray().ParseToNumber<int>();
        byte[] key = data.Skip(4).Take(len).ToArray();

        Logger.Debug($"Received public key from client: {string.Join(", ", key)}");
        client.SetClientPublicKey(key);
    }
}