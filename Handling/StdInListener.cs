using Database.ConsoleCommands;
using Database.ResourceManager;

namespace Database.Handling;

public class StdInListener {
    private readonly List<char> buffer = [];
    private readonly Task _task;

    public static StdInListener Instance { get; set; } = null!;

    public bool IsOnAlternateBuffer { get; set; } = false;

    public StdInListener(CommandListener cmdListener, ResourceUI resourceUI) {
        Instance = this;
        this._task = new Task(() => {
            while (true) {
                ConsoleKeyInfo key = Console.ReadKey();
                if (this.IsOnAlternateBuffer) {
                    resourceUI.TriggerKeyEvent(key.Key);
                    continue;
                }

                if (key.Key == ConsoleKey.Enter) {
                    cmdListener.TriggerCommand(new string(this.buffer.ToArray()));
                    this.buffer.Clear();
                    continue;
                }

                this.buffer.Add(key.KeyChar);
            }
        });
        this._task.Start();
    }
}