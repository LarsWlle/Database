namespace DatabaseTesting.Packets.Enums;

public enum DisconnectReason {
    /// <summary>
    ///     Mismatching protocol versions
    /// </summary>
    INVALID_PROTOCOL = 1,

    /// <summary>
    ///     Client program, not server!
    /// </summary>
    PROGRAM_QUIT = 2,

    /// <summary>
    ///     Sent if a connection is no longer needed by the client (stopped manually before program exit)
    /// </summary>
    CONNECTION_NO_LONGER_NEEDED = 3,

    /// <summary>
    ///     Sent to the client when the server receives a stop/quit command
    /// </summary>
    SERVER_STOPPED = 4
}