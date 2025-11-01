namespace PoeShared.Scaffolding;

/// Unix/POSIX-style exit codes based on <c>sysexits.h</c>.
/// These are widely used on Unix-like systems for expressive CLI exits.
/// </summary>
public enum UnixExitCodes
{
    /// <summary>Successful termination.</summary>
    Ok = 0,

    /// <summary>Command line usage error (bad flags/arity).</summary>
    Usage = 64,

    /// <summary>Data format error (invalid input payload).</summary>
    DataErr = 65,

    /// <summary>Input file not found / cannot be opened.</summary>
    NoInput = 66,

    /// <summary>User specified does not exist.</summary>
    NoUser = 67,

    /// <summary>Host specified does not exist / cannot resolve.</summary>
    NoHost = 68,

    /// <summary>Service unavailable (dependency down, license missing).</summary>
    Unavailable = 69,

    /// <summary>Software/internal error (unhandled exception).</summary>
    Software = 70,

    /// <summary>Operating system error (e.g., syscall failure).</summary>
    OSErr = 71,

    /// <summary>Critical system file missing.</summary>
    OSFile = 72,

    /// <summary>Cannot create output (permission/readonly).</summary>
    CantCreate = 73,

    /// <summary>I/O error during processing.</summary>
    IOError = 74,

    /// <summary>Temporary failure; retry may succeed.</summary>
    TempFail = 75,

    /// <summary>Remote protocol error.</summary>
    Protocol = 76,

    /// <summary>Permission denied.</summary>
    NoPerm = 77,

    /// <summary>Configuration error (invalid/missing config).</summary>
    Config = 78,

    /// <summary>Operation timed out (conventional shell value).</summary>
    Timeout = 124,

    /// <summary>Operation cancelled by signal (e.g., SIGINT). Typically 128+2 = 130.</summary>
    Cancelled = 130,

    /// <summary>Process killed (e.g., SIGKILL). Typically 128+9 = 137.</summary>
    Killed = 137
}