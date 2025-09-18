using System.Runtime.InteropServices;
using Database.Authentication;
using Database.Handling;
using Database.Network;
using Database.Util;

namespace Database.ResourceManager;

/*
 *                             +-------------------------------------------------------------+
 * GreenBG, FullWidth          | [ESC] Back | [PageUp] ScrollUp | [PageDown] ScrollDown    | |
 *                             | |                                                         | |
 *
 *
 * Table, (bottom row = total, sticky), all rows are scrollable (if doesn't fit)
 */

public class ResourceUI {
    private readonly Server _server;

    #region UI Variables
    private readonly int _scrollCounter = 0;
    #endregion

    private int _width, _height;
    private Thread? _rescaleThread;
    private Thread? _rerenderThread;
    private bool _isUsableMemoized;
    private bool _isUsableSetMemoized;
    public bool IsVisible;
    public bool IsUsable {
        get {
            if (this._isUsableSetMemoized) return this._isUsableMemoized;

            this._isUsableMemoized = this.CanOperatingSystemSupport();
            this._isUsableSetMemoized = true;
            return this._isUsableMemoized;
        }
    }

    public void TriggerKeyEvent(ConsoleKey c) {
        this.OnKeyInput.Invoke(c);
    }

    public delegate void OnKeyInputDelegate(ConsoleKey key);
    public event OnKeyInputDelegate OnKeyInput;

    public ResourceUI(Server server) {
        this._server = server;
        this.OnKeyInput = key => {
            switch (key) {
                case ConsoleKey.Escape:
                    this.SwitchToLogs();
                    break;
            }
        };
    }

    public void SetupThread() {
        if (this._rescaleThread != null && this._rerenderThread != null) return; // Already set up
        this._width = Console.BufferWidth;
        this._height = Console.BufferHeight;
        this._rescaleThread = new Thread(() => {
            while (true) {
                if (!this.IsVisible) {
                    Thread.Sleep(1000);
                    continue;
                }

                if (Console.BufferWidth != this._width || Console.BufferHeight != this._height) {
                    this._width = Console.BufferWidth;
                    this._height = Console.BufferHeight;

                    this.Rerender();
                }


                Thread.Sleep(100);
            }
        });
        this._rescaleThread.Start();

        this._rerenderThread = new Thread(() => {
            while (true) {
                if (!this.IsVisible) {
                    Thread.Sleep(1000);
                    continue;
                }

                this.Rerender();
                Thread.Sleep(1000);
            }
        });
        this._rerenderThread.Start();
    }

    private void Rerender() {
        if (!this.IsVisible) return;
        int width = this._width, height = this._height; // Store again, if resizes mid-render it will give weird results

        string[] lines = new string[height];
        lines[0] = "".PadRight(width);
        lines[1] = " \e[30m\e[43m [ESC] Go Back | [PageUp] Scroll Up | [PageDown] Scroll Down".ToFixedLength(width);

        for (int i = 2; i < lines.Length - 1; i++) {
            if (this._server.Users.Count <= i - 2 + this._scrollCounter) {
                lines[i] = " \e[43m \e[0m" + "".ToFixedLength(width - 4) + "\e[43m \e[0m ";
                continue;
            }

            User user = this._server.Users[i - 2 + this._scrollCounter];
            Client? client = this._server.GetCorrespondingClient(user);

            if (client == null) {
                lines[i] = (" \e[43m \e[0m" + $"{user.Username}\t\t NO ACTIVE CONNECTION").ToFixedLength(width - 1) + "\e[43m ";
                continue;
            }

            string username = user.Username.ToFixedLength(50);
            lines[i] = " \e[43m \e[0m" + $"{username} ACTIVE {client.TransactionPacketCounter.ToString().ToFixedLength(5)}".ToFixedLength(width - 4) + "\e[43m ";
        }

        lines[height - 1] = (" \e[30m\e[43m " + "Username".ToFixedLength(50)).ToFixedLength(width) + " \e[0m";


        Console.Clear();
        Console.Out.Write(string.Join("\r\n\e[0m", lines));
    }

    public void SwitchToScreen() {
        this.IsVisible = true;
        StdInListener.Instance.IsOnAlternateBuffer = true;
        this._width = Console.BufferWidth;
        this._height = Console.BufferHeight;
        Console.Out.Write("\e[?1049h");
        this.Rerender();
    }

    public void SwitchToLogs() {
        Console.Out.Write("\e[?1049l");
        this.IsVisible = false;
        StdInListener.Instance.IsOnAlternateBuffer = false;
        Logger.Info("Back to logs!");
    }

    private bool CanOperatingSystemSupport() {
        if (this._isUsableSetMemoized) return this._isUsableMemoized;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return true; // Ansi terminals on Linux/macos are supported
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;

        Version version = Environment.OSVersion.Version;
        return version is { Major: 10, Build: >= 10586 };
    }
}