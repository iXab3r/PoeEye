namespace PoeShared.Scaffolding;

/// <summary>
/// Common Windows (Win32) error codes often reused as process exit codes.
/// These mirror values from <c>winerror.h</c>/<c>GetLastError</c> for clearer diagnostics.
/// </summary>
public enum WindowsExitCodes
{
    /// <summary>Successful termination.</summary>
    Ok = 0,

    /// <summary>The system cannot find the file specified. (ERROR_FILE_NOT_FOUND)</summary>
    FileNotFound = 2,

    /// <summary>The system cannot find the path specified. (ERROR_PATH_NOT_FOUND)</summary>
    PathNotFound = 3,

    /// <summary>Access is denied. (ERROR_ACCESS_DENIED)</summary>
    AccessDenied = 5,

    /// <summary>The parameter is incorrect. (ERROR_INVALID_PARAMETER)</summary>
    InvalidParameter = 87,

    /// <summary>Not enough storage is available to process this command. (ERROR_NOT_ENOUGH_MEMORY)</summary>
    NotEnoughMemory = 8,

    /// <summary>The data is invalid. (ERROR_INVALID_DATA)</summary>
    InvalidData = 13,

    /// <summary>Not enough storage is available to complete this operation. (ERROR_OUTOFMEMORY)</summary>
    OutOfMemory = 14,

    /// <summary>The directory is not empty. (ERROR_DIR_NOT_EMPTY)</summary>
    DirNotEmpty = 145,

    /// <summary>The wait operation timed out. (WAIT_TIMEOUT)</summary>
    WaitTimeout = 258,

    /// <summary>This operation returned because the timeout period expired. (ERROR_TIMEOUT)</summary>
    Timeout = 1460,

    /// <summary>The operation was cancelled by the user. (ERROR_CANCELLED)</summary>
    Cancelled = 1223,

    /// <summary>Cannot create a file when that file already exists. (ERROR_ALREADY_EXISTS)</summary>
    AlreadyExists = 183,

    /// <summary>The pipe has been ended. (ERROR_BROKEN_PIPE)</summary>
    BrokenPipe = 109,

    /// <summary>The semaphore timeout period has expired. (ERROR_SEM_TIMEOUT)</summary>
    SemaphoreTimeout = 121
}