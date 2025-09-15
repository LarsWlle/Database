namespace Database.Storage;

public enum DataType : byte {
    ByteArray = 0, // Default
    Boolean = 1,
    Byte = 2,
    SByte = 3,
    Char = 4,
    Int16 = 5,
    UInt16 = 6,
    Int32 = 7,
    UInt32 = 8,
    Int64 = 9,
    UInt64 = 10,
    Float = 11, // float
    Double = 12,
    Decimal = 13,
    String = 14
}