namespace Database.ConsoleCommands;

public interface IHandler {
    public void Handle(string[] args, Server server);

    public List<string> GetHelp();
}