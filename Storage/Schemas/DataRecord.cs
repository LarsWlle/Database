using System.Reflection;
using System.Text;
using Database.Util;
using DotNetEnv;

namespace Database.Storage.Schemas;

public class DataRecord {
    public (byte[] result, int idxPtrOffset, object idxValue) BuildRecord<T>(SchemaDefinitionBuilder<T>? builder) where T : DataRecord, new() {
        if (builder == null) throw new NullReferenceException("Builder is null");
        List<SchemaDefinition> schema = builder.Build();

        byte[] result = new byte[schema.Sum(def => def.Length)];
        Dictionary<string, object> data = this.GetData();
        int ptr = 0;
        int idxPtr = -1;
        object idxValue = null!;
        schema.ForEach(def => {
            if (!data.TryGetValue(def.Name, out object? value)) throw new KeyNotFoundException("Not all properties have been set according to the SchemaDefinition.");

            byte[] toPush = new byte[def.Length];
            switch (value) {
                case byte b: toPush = b.ParseToBytes(); break;
                case sbyte sb: toPush = sb.ParseToBytes(); break;
                case short s: toPush = s.ParseToBytes(); break;
                case ushort us: toPush = us.ParseToBytes(); break;
                case int i: toPush = i.ParseToBytes(); break;
                case uint ui: toPush = ui.ParseToBytes(); break;
                case long l: toPush = l.ParseToBytes(); break;
                case ulong ul: toPush = ul.ParseToBytes(); break;
                case bool b: toPush = b ? [1] : [0]; break;
                case char c: toPush = [(byte) c]; break;
                case float f: toPush = BitConverter.GetBytes(f); break;
                case double d: toPush = BitConverter.GetBytes(d); break;
                case decimal d:
                    int[] bits = decimal.GetBits(d);
                    byte[] bytes = new byte[16];
                    Buffer.BlockCopy(bits, 0, bytes, 0, 16);
                    break;
                case string s: toPush = Encoding.UTF8.GetBytes(s.PadRight(def.Length)); break;
                case byte[] arr:
                    Array.Copy(arr, 0, toPush, 0, def.Length);
                    break;
            }

            byte[] finalToPush = new byte[def.Length];
            int count = Math.Min(toPush.Length, def.Length);
            Array.Copy(toPush, finalToPush, count);
            Array.Copy(finalToPush, 0, result, ptr, def.Length);

            if (def.IsIndex) {
                idxPtr = ptr;
                idxValue = data[def.Name];
            }

            ptr += def.Length;
        });

        return (result, idxPtr, idxValue);
    }

    /// <summary>
    ///     Format of result:
    ///     <code>
    /// [uint32] NonceLength
    /// [byte[NonceLength]] Nonce
    /// [uint32] DataLength
    /// [byte[DataLength]] Data 
    /// [uint32] TagLength
    /// [byte[TagLength]] Tag
    /// </code>
    /// </summary>
    /// <param name="builder">The schema definition builder</param>
    /// <typeparam name="T">The type of DataRecord</typeparam>
    /// <returns></returns>
    public (byte[] encryptedData, object idxValue) BuildEncryptedRecord<T>(SchemaDefinitionBuilder<T>? builder, Encryption encryption) where T : DataRecord, new() {
        if (builder == null) throw new NullReferenceException("Builder is null");
        (byte[] result, int idxPtrOffset, object idxValue) unencrypted = this.BuildRecord(builder);
        string? secret = Environment.GetEnvironmentVariable("SECRET");
        if (secret == null) throw new EnvVariableNotFoundException("Secret not found to encrypt data! Add a \"SECRET\" environment variable!", "SECRET");

        (byte[] data, byte[] nonce, byte[] tag) encrypted = encryption.Encrypt(secret.HexStringToBytes(), unencrypted.result);

        byte[] array = [
            ..encrypted.nonce.Length.ParseToBytes(), ..encrypted.nonce,
            ..encrypted.data.Length.ParseToBytes(), ..encrypted.data,
            ..encrypted.tag.Length.ParseToBytes(), ..encrypted.tag
        ];

        return (array, unencrypted.idxValue);
    }

    private Dictionary<string, object> GetData() {
        Dictionary<string, object?> data = new();
        PropertyInfo[] properties = this.GetType().GetProperties();
        foreach (PropertyInfo property in properties) {
            SchemaPropertyAttribute? attr = property.GetCustomAttribute<SchemaPropertyAttribute>();
            if (attr == null) continue;

            data.Add(attr.Name, property.GetValue(this));
        }

        return data;
    }

    private DataType GetDataType(Type type) {
        if (type == typeof(bool)) return DataType.Boolean;
        if (type == typeof(byte)) return DataType.Byte;
        if (type == typeof(sbyte)) return DataType.SByte;
        if (type == typeof(char)) return DataType.Char;
        if (type == typeof(short)) return DataType.Int16;
        if (type == typeof(ushort)) return DataType.UInt16;
        if (type == typeof(int)) return DataType.Int32;
        if (type == typeof(uint)) return DataType.UInt32;
        if (type == typeof(long)) return DataType.Int64;
        if (type == typeof(ulong)) return DataType.UInt64;
        if (type == typeof(float)) return DataType.Float;
        if (type == typeof(double)) return DataType.Double;
        if (type == typeof(decimal)) return DataType.Decimal;
        if (type == typeof(string)) return DataType.String;
        return DataType.ByteArray;
    }
}