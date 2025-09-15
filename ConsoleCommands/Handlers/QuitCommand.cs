namespace Database.ConsoleCommands.Handlers;

public class QuitCommand : IHandler {
    public void Handle(string[] args, Server server) {
        Logger.Info("Quitting process! Disconnecting all clients...");
        server.Stop();
    }

    public List<string> GetHelp() => [
        "Quit Command",
        "",
        "Aliases:",
        "* quit",
        "* stop",
        "",
        "Global Subcommands:",
        "* quit help",
        "* stop help",
        "* quit ?",
        "* stop ?"
    ];
}