using Database.Network.Packets.Enums;
using Database.Util;

namespace Database.Network.Packets.Data;

public class ClientboundTransactionResponsePacket(ushort transactionId, TransactionResponseCode err) : OutboundPacket {
    public new static ushort Id { get; } = 11;
    public override byte[] Package() => [..transactionId.ParseToBytes(), (byte) err];
}