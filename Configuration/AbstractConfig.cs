using System.Text;
using System.Text.RegularExpressions;

namespace Database.Configuration;

public abstract class AbstractConfig : IConfigFile {
    public abstract string FilePath { get; }
    public abstract string DefaultValue { get; }

    public string GetFilePath() => IConfigFile.DirectoryName + "\\" + FilePath;

    private readonly Dictionary<string, ConfigSection> _sections = [];

    public void ParseTree() {
        _sections.Clear();
        IEnumerable<string> lines = File.ReadLines(IConfigFile.DirectoryName + "\\" + FilePath);
        ConfigSection? currentSection = null;

        foreach (string line in lines) {
            if (line.Trim().Equals(string.Empty) && currentSection != null) {
                _sections.Add(currentSection.Name, currentSection);
                currentSection = null;
                continue;
            }

            Match match = Regex.Match(line, IConfigFile.SectionRegex);
            if (match.Success) {
                string name = match.Groups["name"].Value;
                if (!Regex.IsMatch(name, IConfigFile.SectionAllowedCharsRegex)) {
                    Logger.Fatal($"Couldn't parse config file: section name is not allowed (name = {name}), only letters from a-Z allowed!");
                    Environment.Exit(1);
                    return;
                }

                currentSection = new ConfigSection(name);
                continue;
            }

            if (currentSection == null) {
                Logger.Fatal($"Property is not in a section! (property = {line})");
                Environment.Exit(1);
                return;
            }

            Match propertyResult = Regex.Match(line, IConfigFile.PropertyRegex);
            if (propertyResult.Groups.Count != 2) {
                Logger.Fatal($"Property does not have a correct format! (property = {line})");
                Environment.Exit(1);
                return;
            }

            string propertyName = propertyResult.Groups[0].Value;
            string propertyValue = propertyResult.Groups[1].Value;

            if (propertyValue.StartsWith('"') && propertyValue.EndsWith('"')) {
                currentSection.Put(propertyName, propertyValue);
            } else if (propertyValue is "true" or "false") {
                // Handle bool
                currentSection.Put(propertyName, bool.Parse(propertyValue));
            } else {
                // Parse to string with try catch if is not number
                bool isDouble = double.TryParse(propertyValue, out double value);
                if (isDouble) {
                    currentSection.Put(propertyName, value);
                    continue;
                }

                bool isInt = long.TryParse(propertyValue, out long intVal);
                if (isInt) {
                    currentSection.Put(propertyName, intVal);
                    continue;
                }

                Logger.Fatal($"Property type is not recognized! (property = {line})");
                Environment.Exit(1);
                return;
            }
        }
    }

    public void Initialize() {
        if (!Directory.Exists(IConfigFile.DirectoryName))
            Directory.CreateDirectory(IConfigFile.DirectoryName);

        if (!File.Exists(GetFilePath())) {
            FileStream stream = File.Create(GetFilePath());
            stream.Write(Encoding.UTF8.GetBytes(DefaultValue));
            stream.Close();
        }
    }

    public object Get(string section, string property) {
        if (!_sections.TryGetValue(section, out ConfigSection? sectionConfig))
            throw new KeyNotFoundException($"Section {section} not found!");

        object? found = sectionConfig.Get(property);
        if (found == null)
            throw new KeyNotFoundException($"Property ({property}) in section ({section}) not found!");

        return found;
    }

    public string GetString(string section, string property) => (string) Get(section, property);

    public long GetLong(string section, string property) => (long) Get(section, property);

    public double GetDouble(string section, string property) => (double) Get(section, property);

    public bool GetBool(string section, string property) => (bool) Get(section, property);

    public string[] GetKeys(bool isDeep, string section = "") {
        if (!isDeep) {
            if (section == "" || _sections.ContainsKey(section)) return [];

            return _sections[section].GetKeys();
        }

        List<string> result = [];
        IEnumerable<object> _ = _sections.Values.Select<ConfigSection, object>(sect => {
            result.AddRange(sect.GetKeys());
            return null!;
        });
        return result.ToArray();
    }

    public void SetKey(string section, string property, string value) {
        throw new NotImplementedException();
    }

    public void SetKey(string section, string property, object value) {
        throw new NotImplementedException();
    }

    public void SetKey(string section, string property, long value) {
        throw new NotImplementedException();
    }

    public void SetKey(string section, string property, double value) {
        throw new NotImplementedException();
    }

    public void SetKey(string section, string property, bool value) {
        throw new NotImplementedException();
    }
}