using Database.ConsoleCommands.Handlers;

namespace Database.ConsoleCommands;

public class CommandListener {
    private readonly Thread _thread;
    private readonly Server _server;

    private readonly Dictionary<string, IHandler> _handlers = new() {
        { "user", new UserCommand() },
        { "quit", new QuitCommand() },
        { "stop", new QuitCommand() }
    };

    public CommandListener(Server server) {
        this._server = server;
        this._thread = new Thread(() => {
            while (true) {
                string cmd = Console.ReadLine() ?? "";
                string[] splitted = cmd.Split(" ");
                this.Handle(splitted[0], splitted.Skip(1).ToArray());
            }
        });
        this._thread.Start();
    }

    private void Handle(string command, string[] args) {
        if (!this._handlers.ContainsKey(command)) return;
        IHandler handler = this._handlers[command];
        if (args.Length != 0 && (args[0] == "help" || args[0] == "?")) {
            handler.GetHelp().ForEach(Logger.Info);
            return;
        }

        handler.Handle(args, this._server);
    }
}