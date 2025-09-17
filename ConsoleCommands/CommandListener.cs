using Database.ConsoleCommands.Handlers;

namespace Database.ConsoleCommands;

public class CommandListener(Server server) {
    private readonly Dictionary<string, IHandler> _handlers = new() {
        { "user", new UserCommand() },
        { "quit", new QuitCommand() },
        { "stop", new QuitCommand() },
        { "resources", new ResourcesCommand() }
    };

    private void Handle(string command, string[] args) {
        if (!this._handlers.TryGetValue(command, out IHandler? handler)) return;

        if (args.Length != 0 && (args[0] == "help" || args[0] == "?")) {
            handler.GetHelp().ForEach(Logger.Info);
            return;
        }

        handler.Handle(args, server);
    }

    public void TriggerCommand(string s) {
        string[] splitted = s.Split(" ");
        this.Handle(splitted[0], splitted.Skip(1).ToArray());
    }
}