using Database.Storage;
using Database.Storage.Schemas;
using Database.Util;

namespace Database.ConsoleCommands.Handlers;

public class SchemaCommand : IHandler {
    public void Handle(string[] args, Server server) {
        if (args.Length < 1) {
            Logger.Warning("Expected at least 1 selector, '*' or a schema name.");
            return;
        }


        if (args[0] == "*") {
            Logger.Debug($"Writing {server.DataFiles.Count} schemas to stdout!");
            foreach ((string? key, DataFile<DataRecord>? value) in server.DataFiles) this.LogAsTable(key, value);
            return;
        }

        if (!server.DataFiles.TryGetValue(args[0], out DataFile<DataRecord>? file)) {
            Logger.Warning("DataFile does not exist!");
            return;
        }

        this.LogAsTable(args[0], file);
    }

    private void LogAsTable(string name, DataFile<DataRecord> file) {
        List<List<string>> table = [];
        int counter = 0;
        file.Definition.Build().ForEach(def => {
            table.Add([counter.ToString(), def.Name, def.Length.ToString(), def.Type.ToString(), def.IsIndex ? "Y" : "N"]);
            counter++;
        });

        Logger.Info(
            string.Join(
                "\r\n",
                table.ToAsciiTable(name, "Index", "Name", "Length", "Type", "IsIndex").Select(line => line.ToFixedLength(Console.BufferWidth - 33))
            )
        );
    }

    public List<string> GetHelp() => [
        "Schema Command",
        "",
        "Subcommands:",
        "* schema *",
        "* schema <name>",
        "",
        "Global Subcommands:",
        "* schema help",
        "* schema ?"
    ];
}