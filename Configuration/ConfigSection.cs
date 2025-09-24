namespace Database.Configuration;

public class ConfigSection(string name) {
    private readonly Dictionary<string, object> _properties = new();
    public string Name { get; set; } = name;

    public string[] GetKeys() => _properties.Keys.ToArray();

    public void Put(string key, object value) => _properties.Add(key, value);

    public object? Get(string key) => _properties.GetValueOrDefault(key);
}