namespace Database.Configuration;

public class ConfigManager {
    private readonly Dictionary<string, AbstractConfig> _configs = new();

    public void Register(string name, AbstractConfig config) {
        config.Initialize();
        config.ParseTree();
        _configs.Add(name, config);
    }

    public T GetConfig<T>(string configName) where T : AbstractConfig => (T) _configs[configName];
}