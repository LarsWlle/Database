using Database.Storage.Schemas;

namespace Database.Storage.Implementation;

public class CredentialsStorageFile() : DataFile<CredentialsSchema>(
    "data\\internal",
    "credentials",
    new SchemaDefinitionBuilder<CredentialsSchema>()
        .AddField("id", DataType.Int32, 4)
        .AddField("username", DataType.String, 50)
        .AddField("password", DataType.String, 32)
        .AddField("salt", DataType.ByteArray, 32)
        .SetIndex(0)
) { }