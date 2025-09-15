using Database.Storage.Schemas;

namespace Database.Storage.Implementation;

public class CredentialsSchema : DataRecord {
    [SchemaProperty("id")]
    public int Id { get; set; }

    [SchemaProperty("username")]
    public string Username { get; set; }

    [SchemaProperty("password")]
    public string Password { get; set; }

    [SchemaProperty("salt")]
    public byte[] Salt { get; set; }
}