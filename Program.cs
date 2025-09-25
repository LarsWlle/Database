using System.Runtime.InteropServices;
using Database.Configuration;
using Database.Configuration.Impl;
using Database.ConsoleCommands;
using Database.Handling;
using DotNetEnv;

namespace Database;

internal class Program {
    private static void Main(string[] args) {
        Env.Load();

        ConfigManager configManager = new();
        GeneralConfig generalConfig = new();
        configManager.Register("general", generalConfig);


        Logger.Info($"Environment variables: {string.Join(", ", Environment.GetEnvironmentVariables().Keys)}");
        Server server = new(generalConfig.Port);

        Logger.ResourceUI = server.ResourceUI;

        if (!server.ResourceUI.IsUsable) {
            Logger.Warning("ResourceUI will be disabled: operating system does not support this!");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Version version = Environment.OSVersion.Version;
                Logger.Warning($"Windows might not be up-to-date: RequiredVersion=(Major=10, Build=\">=10586\") FoundVersion(Major={version.Major}, Minor={version.Minor}, Build={version.Build}, Revision={version.Revision})");
            }
        }


        server.Listen();
        Logger.Info($"Running on port {generalConfig.Port}");
        CommandListener cmdListener = new(server);
        StdInListener stdInListener = new(cmdListener, server.ResourceUI);

        Console.CancelKeyPress += (sender, e) => {
            HandleQuit(server);
            e.Cancel = true;
        };
        AppDomain.CurrentDomain.ProcessExit += (sender, e) => { HandleQuit(server); };

        while (true) { } // TODO: fix something not so bad
    }

    private static void HandleQuit(Server server) {
        Logger.Info("Received process exit!");
        server.Stop();
    }
}