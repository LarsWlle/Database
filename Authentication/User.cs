namespace Database.Authentication;

public class User {
    public int Id { get; set; }
    public string Username { get; set; }
    public byte[] Password { get; set; }
    public byte[] PasswordSalt { get; set; }
}