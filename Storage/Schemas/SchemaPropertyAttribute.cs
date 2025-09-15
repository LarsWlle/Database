namespace Database.Storage;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SchemaPropertyAttribute(string name) : Attribute {
    public string Name { get; private set; } = name;
}