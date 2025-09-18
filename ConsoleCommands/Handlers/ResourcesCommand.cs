namespace Database.ConsoleCommands.Handlers;

public class ResourcesCommand : IHandler {
    public void Handle(string[] args, Server server) {
        if (!server.ResourceUI.IsUsable) {
            Logger.Fatal("InvalidOperatingSystem: Operating system does not support alternative buffers.");
            return;
        }

        server.ResourceUI.SetupThread();
        server.ResourceUI.SwitchToScreen();
    }

    public List<string> GetHelp() => [
        "Resources Command",
        "",
        "Warning:",
        "* Will not work if your operating system does not support alternative screen buffers.",
        "",
        "Global Subcommands:",
        "* resources help",
        "* resources ?"
    ];
}