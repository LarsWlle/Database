namespace Database.Network;

public class Packet {
    public static ushort Id { get; protected set; } = 0;
}

public abstract class OutboundPacket : Packet {
    public abstract byte[] Package();
}

public abstract class InboundPacket : Packet {
    public abstract void Handle(Client client, byte[] packet);
}