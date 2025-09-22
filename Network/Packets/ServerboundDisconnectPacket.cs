using Database.Network.Packets.Enums;

namespace Database.Network.Packets;

public class ServerboundDisconnectPacket : InboundPacket {
    public new static ushort Id { get; } = ushort.MaxValue;

    public override void Handle(Client client, byte[] packet) {
        Console.WriteLine($"{string.Join(", ", packet)} = packet from disconnect serverbound");
        client.HandleDisconnect((DisconnectReason) packet[0]);
        Logger.Info($"Client disconnected with reason {(DisconnectReason) packet[0]}");
    }
}