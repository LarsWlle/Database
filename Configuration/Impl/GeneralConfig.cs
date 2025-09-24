namespace Database.Configuration.Impl;

public class GeneralConfig : AbstractConfig {
    public override string FilePath { get; } = "server.conf";
    public override string DefaultValue { get; } = """
                                                   [server]
                                                       port=5000
                                                       max_connections=500
                                                       
                                                   [logs]
                                                       enable_debug_logs=true
                                                   """;

    public int Port => (int) GetLong("server", "port");
    public int MaxConnections => (int) GetLong("server", "max_connections");
    public bool DebugLogsEnabled => GetBool("logs", "enable_debug_logs");
}