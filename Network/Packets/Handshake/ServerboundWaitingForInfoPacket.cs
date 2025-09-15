namespace Database.Network.Packets.Handshake;

public class ServerboundWaitingForInfoPacket : InboundPacket {
    public new static ushort Id { get; } = 3;

    public override void Handle(Client client, byte[] packet) {
        client.SendPacket(new ClientboundServerInformationPacket());
    }
}