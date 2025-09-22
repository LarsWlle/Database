using System.Text;
using Database.Util;

namespace Database.Storage.Schemas;

public class SchemaDefinitionBuilder<TRecord> where TRecord : DataRecord, new() {
    private readonly List<SchemaDefinition> _fields = [];
    private byte _idx;
    private bool _hasSetIndex;

    public int SchemaLength => this._fields.Sum(field => field.Length) + 2;
    public TRecord CreateEmptyRecord => new();

    public byte[] IndexedValueFromByteArray(byte[] data) {
        int indexedLength = this._fields[this._idx].Length;
        int toSkip = this._fields.Take(this._idx).Sum(f => f.Length);
        return data.Skip(toSkip).Take(indexedLength).ToArray();
    }

    public byte GetIndex() => this._idx;

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

    /*
        +-------+-----------------+------------+
        | Index |      Name       |   Length   |
        +-------+-----------------+------------+
        | 0     | SchemaLength    | 4          |
        | 1     | IndexedColumn   | 1          |
        | 2     | FieldCount      | 1          |
        +-------+-----------------+------------+
        |         Fields (#FieldCount)         |
        +-------+-----------------+------------+
        | x + 1 | FieldNameLength | 2          |
        | x + 2 | FieldName       | NameLength |
        | x + 3 | FieldLength     | 4          |
        | x + 4 | FieldType       | 1          |
        +-------+-----------------+------------+
     */
    public static SchemaDefinitionBuilder<TRecord> FromBytes(byte[] bytes) {
        byte indexedColumn = bytes.First();
        byte fieldCount = bytes.Skip(1).First();

        SchemaDefinitionBuilder<TRecord> builder = new();

        int ptr = 2;
        for (int i = 0; i < fieldCount; i++) {
            ushort nameLength = bytes.Skip(ptr).Take(2).ParseToNumber<ushort>();
            string name = Encoding.UTF8.GetString(bytes.Skip(ptr + 2).Take(nameLength).ToArray());
            uint length = bytes.Skip(ptr + nameLength + 2).Take(4).ParseToNumber<uint>();
            DataType type = (DataType) bytes.Skip(ptr + nameLength + 6).First();

            builder = builder.AddField(name, type, (int) length);

            ptr += nameLength + 7;
        }

        builder = builder.SetIndex(indexedColumn);

        return builder;
    }
}