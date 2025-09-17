using System.Runtime.InteropServices;
using Database.ConsoleCommands;
using Database.Handling;
using Database.ResourceManager;
using DotNetEnv;

namespace Database;

internal class Program {
    private static void Main(string[] args) {
        Env.Load();


        if (!ResourceUI.IsUsable) {
            Logger.Warning("ResourceUI will be disabled: operating system does not support this!");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Version version = Environment.OSVersion.Version;
                Logger.Warning($"Windows might not be up-to-date: RequiredVersion=(Major=10, Build=\">=10586\") FoundVersion(Major={version.Major}, Minor={version.Minor}, Build={version.Build}, Revision={version.Revision})");
            }
        }

        Logger.Info($"Environment variables: {string.Join(", ", Environment.GetEnvironmentVariables().Keys)}");
        Server server = new(5000);
        server.Listen();
        CommandListener cmdListener = new(server);
        StdInListener stdInListener = new(cmdListener);

        while (true) { }
    }
}