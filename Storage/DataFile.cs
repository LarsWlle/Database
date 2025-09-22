using System.Reflection;
using System.Text;
using Database.Storage.Schemas;
using Database.Util;
using DotNetEnv;

namespace Database.Storage;

public class DataFile<TRecord>(string dirName, string fileName, SchemaDefinitionBuilder<TRecord> definition) where TRecord : DataRecord, new() {
    /// <summary>
    ///     Relative path of current working directory
    /// </summary>
    public string DirName { get; } = dirName;
    public string FileName { get; } = fileName;

    public string DataFilePath => Path.Combine(this.DirName, fileName);
    public string IndexFilePath => this.DataFilePath + ".index";
    public SchemaDefinitionBuilder<TRecord> Definition { get; } = definition;

    private readonly Dictionary<object, long> _index = new();
    private readonly Encryption _encryption = new();

    public bool Exists() {
        if (!Directory.Exists(this.DirName)) return false;
        return File.Exists(this.DirName + "\\" + this.FileName) && File.Exists(this.DirName + "\\" + this.FileName + ".index");
    }

    public bool DoesIndexExist(object key) => this._index.ContainsKey(key);

    public void Create() {
        if (this.Exists()) return;

        string filepath = this.DirName + "\\" + this.FileName;
        if (!Directory.Exists(this.DirName)) Directory.CreateDirectory(this.DirName);
        if (!File.Exists(filepath)) File.Create(filepath).Close();
        if (!File.Exists(filepath + ".index")) File.Create(filepath + ".index").Close();

        this.WriteDefault();
    }

    private void WriteDefault() {
        // Schema Length (int)
        // IndexedColumn (byte)
        // Field count (byte)
        // Fields
        //      -> NameLength (short)
        //      -> Name
        //      -> Length
        //      -> DataType

        File.WriteAllBytes(this.DataFilePath, this.Definition.BuildBytes());
    }

    public void LoadIndex() {
        if (!File.Exists(this.IndexFilePath)) {
            this.Create();
            return;
        }

        foreach (string line in File.ReadLines(this.IndexFilePath)) {
            string[] parts = line.Split('=');
            if (parts.Length != 2) continue;
            if (!long.TryParse(parts[1], out long pos)) continue;
            this._index[parts[0]] = pos;
        }

        Logger.Debug($"Loaded {this._index.Count} records");
    }

    public void Add<T>(T obj) where T : DataRecord {
        try {
            if (!this.Exists()) this.Create();

            //(byte[] result, int idxPtrOffset, object idxValue) record = obj.BuildRecord(this.Definition);
            (byte[] encryptedData, object idxValue) record = obj.BuildEncryptedRecord(this.Definition, this._encryption);

            FileStream fs = new(this.DataFilePath, FileMode.Append, FileAccess.Write);
            long position = fs.Position;
            fs.Close();

            File.AppendAllBytes(this.DataFilePath, record.encryptedData);
            this.AddIndex(position, record.idxValue);
        } catch (Exception e) {
            Logger.Fatal($"Couldn't write to storage: {e}");
        }
    }

    public void Add(byte[] data) {
        string? secret = Environment.GetEnvironmentVariable("SECRET");
        if (secret == null) throw new EnvVariableNotFoundException("Secret not found to encrypt data! Add a \"SECRET\" environment variable!", "SECRET");

        (byte[] data, byte[] nonce, byte[] tag) encrypted = this._encryption.Encrypt(secret.HexStringToBytes(), data);

        byte[] array = [
            ..encrypted.nonce.Length.ParseToBytes(), ..encrypted.nonce,
            ..encrypted.data.Length.ParseToBytes(), ..encrypted.data,
            ..encrypted.tag.Length.ParseToBytes(), ..encrypted.tag
        ];

        FileStream fs = new(this.DataFilePath, FileMode.Append, FileAccess.Write);
        long position = fs.Position;
        fs.Close();

        File.AppendAllBytes(this.DataFilePath, array);
        byte[] indexedArray = this.Definition.IndexedValueFromByteArray(data);
        object indexedObject = this.ParseObject(indexedArray, this.Definition.Build()[this.Definition.GetIndex()].Type);
        this.AddIndex(position, indexedObject);
    }

    private void AddIndex(long position, object value) {
        if (!this.Exists()) this.Create();
        this._index[value] = position;
        using StreamWriter sw = new(this.IndexFilePath, true);
        if (value is string str && str.Contains('=')) throw new ArgumentException("Value cannot contain '='"); // TODO: handle with error packet
        sw.WriteLine($"{value}={position}");
    }

    /// <summary>
    ///     Do not use with large datasets, will load everything in memory
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public List<T> All<T>() where T : DataRecord, new() {
        List<T> result = new();
        foreach ((object? key, long value) in this._index) {
            this.GetRecord(value, out T outVar);
            result.Add(outVar);
        }

        return result;
    }

    private void GetRecord<T>(long ptrOffset, out T _result) where T : DataRecord, new() {
        byte[] data = File.ReadAllBytes(this.DataFilePath).Skip((int) ptrOffset).ToArray(); // TODO: fix it's using the long and not casting to int
        T result = new();

        int nonceLength = data.Take(4).ToArray().ParseToNumber<int>(); // Skip 2 (length), take 4 (nonce length)
        byte[] nonce = data.Skip(4).Take(nonceLength).ToArray();

        int actualDataLength = data.Skip(4 + nonceLength).Take(4).ToArray().ParseToNumber<int>(); // Skip length + nonce length + nonce, take 4 (text len)
        byte[] actualData = data.Skip(8 + nonceLength).Take(actualDataLength).ToArray();

        int tagLength = data.Skip(8 + nonceLength + actualDataLength).Take(4).ToArray().ParseToNumber<int>();
        byte[] tag = data.Skip(12 + nonceLength + actualDataLength).Take(tagLength).ToArray();


        string? secret = Environment.GetEnvironmentVariable("SECRET");
        if (secret == null) throw new EnvVariableNotFoundException("Secret not found to encrypt data! Add a \"SECRET\" environment variable!", "SECRET");

        byte[] decrypted = this._encryption.Decrypt(secret.HexStringToBytes(), nonce, actualData, tag);

        int readPointer = 0;
        this.Definition.Build().ForEach(def => {
            PropertyInfo reflectionsField = result.GetType()
                .GetProperties()
                .First(prop => prop.GetCustomAttribute<SchemaPropertyAttribute>()?.Name == def.Name);

            object parsed = this.ParseObject(decrypted.Skip(readPointer).Take(def.Length).ToArray(), def.Type);
            reflectionsField.SetValue(result, parsed);
            readPointer += def.Length;
        });
        _result = result;
    }

    /// <summary>
    ///     If none are found, the result will be empty and false will be returned
    /// </summary>
    public bool GetByIndexValue<T>(object value, out T result) where T : DataRecord, new() {
        if (!this._index.TryGetValue(value, out long pos)) {
            result = new T();
            return false;
        }

        this.GetRecord(pos, out result);
        return true;
    }

    public object ParseObject(byte[] obj, DataType type) {
        switch (type) {
            case DataType.ByteArray: return obj;
            case DataType.Boolean: return obj[0] == 1;
            case DataType.Byte:
            case DataType.SByte:
            case DataType.Char: return obj[0];
            case DataType.Int16: return obj.ParseToNumber<short>();
            case DataType.UInt16: return obj.ParseToNumber<ushort>();
            case DataType.Int32: return obj.ParseToNumber<int>();
            case DataType.UInt32: return obj.ParseToNumber<uint>();
            case DataType.Int64: return obj.ParseToNumber<long>();
            case DataType.UInt64: return obj.ParseToNumber<ulong>();
            case DataType.Float: return BitConverter.ToSingle(obj);
            case DataType.Double: return BitConverter.ToDouble(obj);
            case DataType.Decimal:
                int[] bits = new int[4];
                for (int i = 0; i < 4; i++)
                    bits[i] = BitConverter.ToInt32(obj, i * 4);
                return new decimal(bits);
            case DataType.String: return Encoding.ASCII.GetString(obj).TrimEnd(' ');
            default: return obj;
        }
    }

    public bool IsValidDataRecord(byte[] data) => data.Length == this.Definition.Build().Sum(def => def.Length);
}