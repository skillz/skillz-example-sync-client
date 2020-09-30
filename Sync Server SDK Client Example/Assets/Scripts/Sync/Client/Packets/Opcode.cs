namespace Servers
{
    /// <summary>
    /// Opcodes for the example game server protocol.
    /// </summary>
    /// <remarks>
    /// There is currently a bug in Flatbuffers where setting a default value in
    /// the schema does not work. As a result, you have to manually add the
    /// opcode when building one of the Packet instances because Flatbuffer
    /// will default to a value of 0. This value is used as a sentinel to represent
    /// an invalid opcode.
    /// </remarks>
    public enum Opcode : short
    {
        Invalid = 0,

        Connect = 1,

        KeepAlive = 2,

        Forfeit = 3,

        AppPaused = 4,

        AppResumed = 5,

        MatchSuccess = 6,

        OpponentPaused = 7,

        OpponentResumed = 8,

        OpponentConnectionStatus = 9,

        PlayerReconnected = 10,

        MatchOver = 11,

        GameState = 12,

        PlayerInput = 13,

        Chat = 14,
    }
}