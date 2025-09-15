using System.Text;
using Database.Util;

namespace Database.Storage.Schemas;

public class SchemaDefinitionBuilder<TRecord> where TRecord : DataRecord, new() {
    private readonly List<SchemaDefinition> _fields = [];
    private byte _idx;
    private bool _hasSetIndex;
    public int SchemaLength => this._fields.Sum(field => field.Length) + 2;
    public TRecord CreateEmptyRecord => new();

    public SchemaDefinitionBuilder<TRecord> AddField(string name, DataType type, int length) {
        this._fields.Add(new SchemaDefinition {
            Name = name,
            Type = type,
            Length = length
        });
        return this;
    }

    public SchemaDefinitionBuilder<TRecord> SetIndex(int idx) {
        this._fields[idx].IsIndex = true;
        this._idx = (byte) idx;
        this._hasSetIndex = true;
        return this;
    }

    public List<SchemaDefinition> Build() {
        if (!this._hasSetIndex) throw new InvalidOperationException("Could not build definition: no index has been set!");
        return this._fields;
    }

    /// <summary>
    ///     <code>
    ///     Schema Length (int)<br />
    ///     IndexedColumn (byte)<br />
    ///     Field count (byte)<br />
    ///     Fields<br />
    ///         -> NameLength (short)<br />
    ///         -> Name<br />
    ///         -> Length<br />
    ///         -> DataType<br />
    /// </code>
    /// </summary>
    /// <returns></returns>
    public byte[] BuildBytes() {
        int schemaLength = 2;
        List<byte> fields = [];
        this._fields.ForEach(field => {
            byte[] parsedField = field.ToByteArray();
            fields.AddRange(parsedField);
            schemaLength += parsedField.Length;
        });

        return [
            ..schemaLength.ParseToBytes(),
            this._idx,
            (byte) this._fields.Count,
            ..fields
        ];
    }

    public static SchemaDefinitionBuilder<TRecord> FromBytes(byte[] bytes) {
        uint schemaLength = bytes.Take(4).ParseToNumber<uint>();
        byte indexedColumn = bytes.Skip(4).First();
        byte fieldCount = bytes.Skip(5).First();

        SchemaDefinitionBuilder<TRecord> builder = new SchemaDefinitionBuilder<TRecord>().SetIndex(indexedColumn);

        int ptr = 5;
        for (int i = 0; i < fieldCount; i++) {
            ushort nameLength = bytes.Skip(ptr).Take(2).ParseToNumber<ushort>();
            string name = Encoding.UTF8.GetString(bytes.Skip(ptr + 2).Take(nameLength).ToArray());
            uint length = bytes.Skip(ptr + nameLength + 2).ParseToNumber<uint>();
            DataType type = (DataType) bytes.Skip(ptr + nameLength + 6).First();

            builder = builder.AddField(name, type, (int) length);

            ptr += nameLength + 7;
        }

        return builder;
    }
}