namespace Database.Configuration;

public interface IConfigFile {
    public const string DirectoryName = "config";
    public const string SectionRegex = @"\[(?<name>[^\]]+)\]";
    public const string SectionAllowedCharsRegex = @"^[A-Za-z]+";
    public const string PropertyRegex = """(?: {1,4}|\t)(([a-z]|[A-Z]){1,})=("*"|[0-9]+|false|true)""";
    protected string FilePath { get; }

    internal void ParseTree();

    object Get(string section, string property);
    string GetString(string section, string property);
    long GetLong(string section, string property);
    double GetDouble(string section, string property);
    bool GetBool(string section, string property);

    string[] GetKeys(bool isDeep, string section = "");

    void SetKey(string section, string property, object value);
    void SetKey(string section, string property, string value);
    void SetKey(string section, string property, long value);
    void SetKey(string section, string property, double value);
    void SetKey(string section, string property, bool value);
}