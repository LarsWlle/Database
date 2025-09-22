namespace Database.Network.Packets.Enums;

[Flags]
public enum TransactionResponseCode : byte {
    // Successful operations
    Added = 1,
    Updated = 2, // TODO: create packet/functionality
    Deleted = 3, // TODO: create packet/functionality

    // Errors
    SchemaNotFound = 10 | Error,
    SchemaEditingNotAllowed = 11 | Error,
    IndexAlreadyExists = 12 | Error,

    Error = 0b1000_0000
}