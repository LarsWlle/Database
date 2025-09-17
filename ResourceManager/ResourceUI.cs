using System.Runtime.InteropServices;
using Database.Handling;
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

public static class ResourceUI {
    private static int _width, _height;
    private static Thread? _rescaleThread;
    private static Thread? _keyboardThread;
    private static bool _isUsableMemoized;
    private static bool _isUsableSetMemoized;
    public static bool IsVisible;
    public static bool IsUsable {
        get {
            if (_isUsableSetMemoized) return _isUsableMemoized;

            _isUsableMemoized = CanOperatingSystemSupport();
            _isUsableSetMemoized = true;
            return _isUsableMemoized;
        }
    }

    public static void TriggerKeyEvent(ConsoleKey c) {
        OnKeyInput.Invoke(c);
    }

    public delegate void OnKeyInputDelegate(ConsoleKey key);
    public static event OnKeyInputDelegate OnKeyInput = key => {
        switch (key) {
            case ConsoleKey.Escape:
                SwitchToLogs();
                break;
        }
    };

    public static void SetupThread() {
        if (_rescaleThread != null && _keyboardThread != null) return; // Already set up
        _width = Console.BufferWidth;
        _height = Console.BufferHeight;
        _rescaleThread = new Thread(() => {
            while (true) {
                if (!IsVisible) {
                    Thread.Sleep(1000);
                    continue;
                }

                if (Console.BufferWidth != _width || Console.BufferHeight != _height) {
                    _width = Console.BufferWidth;
                    _height = Console.BufferHeight;

                    Rerender();
                }


                Thread.Sleep(100);
            }
        });
        _rescaleThread.Start();
    }

    private static void Rerender() {
        if (!IsVisible) return;
        int width = _width, height = _height; // Store again, if resizes mid-render it will give weird results

        string[] lines = new string[height];
        lines[0] = "".PadRight(width);
        lines[1] = " \e[30m\e[43m [ESC] Go Back | [PageUp] Scroll Up | [PageDown] Scroll Down".ToFixedLength(width);

        for (int i = 2; i < lines.Length - 1; i++)
            lines[i] = " \e[43m \e[0m" + "".ToFixedLength(width - 4) + "\e[43m \e[0m ";

        lines[height - 1] = " \e[30m\e[43m Total: ".ToFixedLength(width) + " \e[0m";


        Console.Out.Write(string.Join("\r\n\e[0m", lines));
    }

    public static void SwitchToScreen() {
        IsVisible = true;
        StdInListener.Instance.IsOnAlternateBuffer = true;
        _width = Console.BufferWidth;
        _height = Console.BufferHeight;
        Console.Out.Write("\e[?1049h");
        Rerender();
    }

    public static void SwitchToLogs() {
        Console.Out.Write("\e[?1049l");
        IsVisible = false;
        StdInListener.Instance.IsOnAlternateBuffer = false;
        Logger.Info("Back to logs!");
    }

    private static bool CanOperatingSystemSupport() {
        if (_isUsableSetMemoized) return _isUsableMemoized;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return true; // Ansi terminals on Linux/macos are supported
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;

        Version version = Environment.OSVersion.Version;
        return version is { Major: 10, Build: >= 10586 };
    }
}