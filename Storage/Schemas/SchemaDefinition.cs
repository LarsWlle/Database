using System.Text;
using Database.Util;

namespace Database.Storage.Schemas;

public class SchemaDefinition {
    public string Name { get; set; }
    public DataType Type { get; set; }
    public int Length { get; set; }

    public bool IsIndex { get; set; } = false;

    public byte[] ToByteArray() {
        byte[] name = Encoding.UTF8.GetBytes(this.Name);
        return [
            ..((ushort) name.Length).ParseToBytes(),
            ..name,
            ..this.Length.ParseToBytes(),
            (byte) this.Type
        ];
    }
}