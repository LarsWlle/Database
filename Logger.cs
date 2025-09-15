namespace Database;

public class Logger {
    private static string Gray(string msg) => $"\x1b[90m{msg}\x1b[0m";
    private static string Cyan(string msg) => $"\x1b[96m{msg}\x1b[0m";
    private static string Green(string msg) => $"\x1b[92m{msg}\x1b[0m";
    private static string Yellow(string msg) => $"\x1b[33m{msg}\x1b[0m";
    private static string Magenta(string msg) => $"\x1b[35m{msg}\x1b[0m";
    private static string Red(string msg) => $"\x1b[31m{msg}\x1b[0m";
    private static string BrightRed(string msg) => $"\x1b[91m{msg}\x1b[0m";


    public static void Info(string msg) {
        Log($"{Gray("[")}{Cyan(GetFormattedDate())}{Gray("]")} {Gray("[")}{Green("INFO")}{Gray("]")} {msg}");
    }

    public static void Warning(string msg) {
        Log($"{Gray("[")}{Cyan(GetFormattedDate())}{Gray("]")} {Gray("[")}{Yellow("WARNING")}{Gray("]")} {msg}");
    }

    public static void Debug(string msg) {
        Log($"{Gray("[")}{Cyan(GetFormattedDate())}{Gray("]")} {Gray("[")}{Magenta("DEBUG")}{Gray("]")} {msg}");
    }

    public static void Error(string msg) {
        Log($"{Gray("[")}{Cyan(GetFormattedDate())}{Gray("]")} {Gray("[")}{BrightRed("ERROR")}{Gray("]")} {msg}");
    }

    public static void Fatal(string msg) {
        Log($"{Gray("[")}{Cyan(GetFormattedDate())}{Gray("]")} {Gray("[")}{Red("FATAL")}{Gray("]")} {msg}");
    }

    public static void Packet(string msg, bool isClientBound, int connectionId, int packetId) {
        string flow = Gray($"[{(isClientBound ? "S->C" : "C->S")}]");
        Log(
            $"{Gray("[")}{Cyan(GetFormattedDate())}{Gray("]")} {Gray("[")}{Magenta("PACKET")}{Gray("]")} {flow} {Gray(connectionId.ToString())} {Gray(packetId.ToString())} {msg}"
        );
    }


    private static void Log(string msg) {
        Console.WriteLine(msg);
    }

    private static string GetFormattedDate() => DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss.fff");
}