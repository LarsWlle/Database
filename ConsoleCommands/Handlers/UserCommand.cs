using System.Security.Cryptography;
using System.Text;
using Database.Storage.Implementation;

namespace Database.ConsoleCommands.Handlers;

public class UserCommand : IHandler {
    // Options:
    /**
     * 1. user list
     * 2. user add [username] [password]
     */
    public void Handle(string[] args, Server server) {
        if (args.Length <= 0) {
            Logger.Error("No arguments provided, consider using the help subcommand");
            return;
        }

        switch (args[0].ToLower()) {
            case "add":
                if (args.Length < 3) {
                    Logger.Error("Invalid argument count, use `user help`");
                    return;
                }

                string username = args[1];
                string password = args[2];
                byte[] salt = RandomNumberGenerator.GetBytes(32);
                server.CredentialsFile.Add(new CredentialsSchema {
                    Id = new Random().Next(),
                    Username = username,
                    Password = Encoding.ASCII.GetBytes(password),
                    Salt = salt
                });
                Logger.Info("User has been added!");
                break;
            case "list":
                List<CredentialsSchema> users = server.CredentialsFile.All<CredentialsSchema>();
                Logger.Info("Users:");
                users.ForEach(usr => { Logger.Info($"* User[Username={usr.Username}, Password={usr.Password}, Id={usr.Id}]"); });
                break;
        }
    }

    public List<string> GetHelp() => [
        "User Command",
        "",
        "Subcommands:",
        "* user list",
        "* user add [username] [password]",
        "",
        "Global Subcommands:",
        "* user help",
        "* user ?"
    ];
}