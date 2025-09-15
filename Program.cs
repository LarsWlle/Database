using Database.ConsoleCommands;
using DotNetEnv;

namespace Database;

internal class Program {
    private static void Main(string[] args) {
        Env.Load();
        Logger.Info($"Environment variables: {string.Join(", ", Environment.GetEnvironmentVariables().Keys)}");
        Server server = new(5000);
        server.Listen();
        CommandListener cmdListener = new(server);

        while (true) { }
    }
}