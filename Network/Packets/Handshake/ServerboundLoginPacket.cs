using System.Text;
using Database.Authentication;
using Database.Util;

namespace Database.Network.Packets.Handshake;

public class ServerboundLoginPacket : InboundPacket {
    public new static ushort Id { get; } = 5;

    public override void Handle(Client client, byte[] packet) {
        ushort usernameLength = packet.Take(2).ToArray().ParseToNumber<ushort>();
        string username = Encoding.UTF8.GetString(packet.Skip(2).Take(usernameLength).ToArray());

        ushort pwdLength = packet.Skip(usernameLength + 2).Take(2).ToArray().ParseToNumber<ushort>();
        byte[] pwd = packet.Skip(usernameLength + 2).Take(pwdLength).ToArray();


        LoginFailedReason result = client.AttemptLogin(username, pwd);
        client.SendPacket(new ClientboundLoginResponsePacket(result));
    }
}