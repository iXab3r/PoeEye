using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using PInvoke;
using PoeShared.Scaffolding;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable CommentTypo

namespace PoeShared.Native;

public partial class UnsafeNative
{
    private static Process CurrentProcess = Process.GetCurrentProcess();
    private static IntPtr CurrentProcessHandle = Process.GetCurrentProcess().Handle;
    private static bool CurrentProcessIs32BitProcessOn64BitOs = Is32BitProcessOn64BitOs(CurrentProcessHandle);

    private enum ProcessInfoClass
    {
        ProcessBasicInformation = 0, // 0, q: PROCESS_BASIC_INFORMATION, PROCESS_EXTENDED_BASIC_INFORMATION
        ProcessQuotaLimits, // qs: QUOTA_LIMITS, QUOTA_LIMITS_EX
        ProcessIoCounters, // q: IO_COUNTERS
        ProcessVmCounters, // q: VM_COUNTERS, VM_COUNTERS_EX
        ProcessTimes, // q: KERNEL_USER_TIMES
        ProcessBasePriority, // s: KPRIORITY
        ProcessRaisePriority, // s: ULONG
        ProcessDebugPort, // q: HANDLE
        ProcessExceptionPort, // s: HANDLE
        ProcessAccessToken, // s: PROCESS_ACCESS_TOKEN
        ProcessLdtInformation, // 10
        ProcessLdtSize,
        ProcessDefaultHardErrorMode, // qs: ULONG
        ProcessIoPortHandlers, // (kernel-mode only)
        ProcessPooledUsageAndLimits, // q: POOLED_USAGE_AND_LIMITS
        ProcessWorkingSetWatch, // q: PROCESS_WS_WATCH_INFORMATION[]; s: void
        ProcessUserModeIopl,
        ProcessEnableAlignmentFaultFixup, // s: BOOLEAN
        ProcessPriorityClass, // qs: PROCESS_PRIORITY_CLASS
        ProcessWx86Information,
        ProcessHandleCount, // 20, q: ULONG, PROCESS_HANDLE_INFORMATION
        ProcessAffinityMask, // s: KAFFINITY
        ProcessPriorityBoost, // qs: ULONG
        ProcessDeviceMap, // qs: PROCESS_DEVICEMAP_INFORMATION, PROCESS_DEVICEMAP_INFORMATION_EX
        ProcessSessionInformation, // q: PROCESS_SESSION_INFORMATION
        ProcessForegroundInformation, // s: PROCESS_FOREGROUND_BACKGROUND
        ProcessWow64Information, // q: ULONG_PTR
        ProcessImageFileName, // q: UNICODE_STRING
        ProcessLuidDeviceMapsEnabled, // q: ULONG
        ProcessBreakOnTermination, // qs: ULONG
        ProcessDebugObjectHandle, // 30, q: HANDLE
        ProcessDebugFlags, // qs: ULONG
        ProcessHandleTracing, // q: PROCESS_HANDLE_TRACING_QUERY; s: size 0 disables, otherwise enables
        ProcessIoPriority, // qs: ULONG
        ProcessExecuteFlags, // qs: ULONG
        ProcessResourceManagement,
        ProcessCookie, // q: ULONG
        ProcessImageInformation, // q: SECTION_IMAGE_INFORMATION
        ProcessCycleTime, // q: PROCESS_CYCLE_TIME_INFORMATION
        ProcessPagePriority, // q: ULONG
        ProcessInstrumentationCallback, // 40
        ProcessThreadStackAllocation, // s: PROCESS_STACK_ALLOCATION_INFORMATION, PROCESS_STACK_ALLOCATION_INFORMATION_EX
        ProcessWorkingSetWatchEx, // q: PROCESS_WS_WATCH_INFORMATION_EX[]
        ProcessImageFileNameWin32, // q: UNICODE_STRING
        ProcessImageFileMapping, // q: HANDLE (input)
        ProcessAffinityUpdateMode, // qs: PROCESS_AFFINITY_UPDATE_MODE
        ProcessMemoryAllocationMode, // qs: PROCESS_MEMORY_ALLOCATION_MODE
        ProcessGroupInformation, // q: USHORT[]
        ProcessTokenVirtualizationEnabled, // s: ULONG
        ProcessConsoleHostProcess, // q: ULONG_PTR
        ProcessWindowInformation, // 50, q: PROCESS_WINDOW_INFORMATION
        ProcessHandleInformation, // q: PROCESS_HANDLE_SNAPSHOT_INFORMATION // since WIN8
        ProcessMitigationPolicy, // s: PROCESS_MITIGATION_POLICY_INFORMATION
        ProcessDynamicFunctionTableInformation,
        ProcessHandleCheckingMode,
        ProcessKeepAliveCount, // q: PROCESS_KEEPALIVE_COUNT_INFORMATION
        ProcessRevokeFileHandles, // s: PROCESS_REVOKE_FILE_HANDLES_INFORMATION
        MaxProcessInfoClass
    }
    
    private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;
    
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool SetDefaultDllDirectories(uint directoryFlags);
    
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr AddDllDirectory(string directory);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool RemoveDllDirectory(IntPtr lpCookie);
    
    [DllImport("kernel32.dll", SetLastError=true)]
    public static extern uint GetModuleFileName
    (
        [In]
        IntPtr hModule,

        [Out]
        StringBuilder lpFilename,

        [In]
        [MarshalAs(UnmanagedType.U4)]
        int nSize
    );

    /// <summary>
    /// In the Windows API (with some exceptions discussed in the following paragraphs),
    /// the maximum length for a path is MAX_PATH, which is defined as 260 characters.
    /// A local path is structured in the following order: drive letter, colon, backslash, name components separated by backslashes, and a terminating null character.
    /// For example, the maximum path on drive D is "D:\some 256-character path string<NUL>" where "<NUL>" represents the invisible terminating null character for the current system codepage.
    /// (The characters < > are used here for visual clarity and cannot be part of a valid path string.)
    /// </summary>
    public const int MAX_PATH = 260;

    public static void SetDefaultDllDirectories()
    {
        if (SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS))
        {
            Log.Info("DLLs will be searched for in all possible directories.");
            return;
        }

        Log.Warn("Failed to set default DLL directories.");
        throw new Win32Exception(Kernel32.GetLastError(), "Failed to set default DLL directories.");
    }

    public static void CreateHardLink(string lpFileName, string lpExistingFileName)
    {
        if (CreateHardLink(lpFileName, lpExistingFileName, IntPtr.Zero))
        {
            return;
        }

        var lastError = Kernel32.GetLastError();
        Exception winException = lastError != Win32ErrorCode.NERR_Success ? new System.ComponentModel.Win32Exception(error: (int) lastError) : null;
        throw new InvalidStateException($"Failed to create hardlink {lpFileName} => {lpExistingFileName}", winException);
    }

    public static int GetParentProcessId(int processId)
    {
        using var openedProcess = OpenProcess(processId, Kernel32.ProcessAccess.PROCESS_VM_READ | Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION);
        ProcessBasicInformation processInfo = default;
        var argReturnLength = 0U;
        var result = NtQueryInformationProcess(openedProcess.DangerousGetHandle(), ProcessInfoClass.ProcessBasicInformation, ref processInfo, Marshal.SizeOf(processInfo), ref argReturnLength);
        if (result != 0)
        {
            throw new System.ComponentModel.Win32Exception(RtlNtStatusToDosError(result));
        }
        return (int)processInfo.Reserved3;
    }

    /// <summary>
    ///   Reads command line of a target process
    ///   This is refactored version of code from https://github.com/VbScrub/ProcessCommandLineDemo
    /// </summary>
    /// <param name="processId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="Win32Exception"></exception>
    public static string GetCommandLine(int processId)
    {
        using var openedProcess = OpenProcess(processId, Kernel32.ProcessAccess.PROCESS_VM_READ | Kernel32.ProcessAccess.PROCESS_QUERY_INFORMATION);
        var is64BitPeb = Is64BitPeb(openedProcess);
            
        ProcessBasicInformation processInfo = default;
        var argReturnLength = 0U;
        var result = NtQueryInformationProcess(openedProcess.DangerousGetHandle(), ProcessInfoClass.ProcessBasicInformation, ref processInfo, Marshal.SizeOf(processInfo), ref argReturnLength);
        if (result != 0)
        {
            throw new System.ComponentModel.Win32Exception(RtlNtStatusToDosError(result));
        }

        if (processInfo.PebBaseAddress == IntPtr.Zero)
        {
            throw new ApplicationException($"{nameof(processInfo.PebBaseAddress)} is not set for processId: {processId}, structure: {processInfo.DumpToString()}");
        }

        // Get pointer from the ProcessParameters member of the PEB (PEB has different structure on x86 vs x64 so different structures needed for each)
        var pebBuffer = ReadMemory(
            openedProcess, 
            processInfo.PebBaseAddress,
            Marshal.SizeOf(is64BitPeb ? typeof(Peb64) : typeof(Peb32)));
        using var pebBytesPtr = new SafeGCHandle(GCHandle.Alloc(pebBuffer, GCHandleType.Pinned));
            
        IntPtr processParametersPtr;
        if (is64BitPeb)
        {
            var peb64 = pebBytesPtr.ToStructure<Peb64>();
            processParametersPtr = peb64.ProcessParameters;
            if (processParametersPtr == IntPtr.Zero)
            {
                throw new ApplicationException($"{nameof(Peb64.ProcessParameters)} is not set for processId: {processId}, 64-bit structure: {peb64.DumpToString()}");
            }
        }
        else
        {
            var peb32 = pebBytesPtr.ToStructure<Peb32>();
            processParametersPtr = peb32.ProcessParameters;
            if (processParametersPtr == IntPtr.Zero)
            {
                throw new ApplicationException($"{nameof(Peb64.ProcessParameters)} is not set for processId: {processId}, 32-bit structure: {peb32.DumpToString()}");
            }
        }

        // Now that we've got the pointer from the ProcessParameters member, we read the RTL_USER_PROCESS_PARAMETERS structure that is stored at that location in the target process' memory
        var userProcessParametersBuffer = ReadMemory(openedProcess, processParametersPtr, Marshal.SizeOf(typeof(RtlUserProcessParameters)));
        using var procParamsBytesPtr = new SafeGCHandle(GCHandle.Alloc(userProcessParametersBuffer, GCHandleType.Pinned));
        var procParams = procParamsBytesPtr.ToStructure<RtlUserProcessParameters>();
        var cmdLineUnicodeString = procParams.CommandLine;
            
        // The Buffer member of the UNICODE_STRING structure points to the actual command line string we want, so we read from the location that points to
        var cmdLineBytes = ReadMemory(openedProcess, cmdLineUnicodeString.Buffer, cmdLineUnicodeString.Length);
        return Encoding.Unicode.GetString(cmdLineBytes);
    }

    private static byte[] ReadMemory(SafeHandle handle, IntPtr address, int bytesToRead)
    {
        var buffer = new byte[bytesToRead];
        using var gcHandle = new SafeGCHandle(GCHandle.Alloc(buffer));
        if (!ReadProcessMemory(handle.DangerousGetHandle(), address, buffer, bytesToRead, out var _))
        {
            var lastError = Kernel32.GetLastError();
            throw new Win32Exception(lastError, $"Failed to read process memory, process handle: {handle}, error code: {lastError}");
        }
            
        return buffer;
    }

    /// <summary>
    /// If we're on a 64 bit OS then the target process will have a 64 bit PEB if we are calling this function from a 64 bit process (regardless of
    /// whether or not the target process is 32 bit or 64 bit).
    /// If we are calling this function from a 32 bit process and the target process is 32 bit then we will get a 32 bit PEB, even on a 64 bit OS. 
    /// The one situation we can't handle is if we are calling this function from a 32 bit process and the target process is 64 bit. For that we need to use the
    /// undocumented NtWow64QueryInformationProcess64 and NtWow64ReadVirtualMemory64 APIs
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static bool Is64BitPeb(SafeHandle process)
    {
        if (!Environment.Is64BitOperatingSystem)
        {
            return false;
        }

        var result = false;
        if (CurrentProcessIs32BitProcessOn64BitOs)
        {
            if (!Is32BitProcessOn64BitOs(process.DangerousGetHandle()))
            {
                // TODO: Use NtWow64ReadVirtualMemory64 to read from 64 bit processes when we are a 32 bit process instead of throwing this exception
                throw new InvalidOperationException("This function cannot be used against a 64 bit process when the calling process is 32 bit");
            }
        }
        else
        {
            result = true;
        }

        return result;
    }

    private static bool Is32BitProcessOn64BitOs(IntPtr processHandle)
    {
        var isWow64 = false;
        if (MethodExistsInDll("kernel32.dll", "IsWow64Process"))
        {
            IsWow64Process(processHandle, out isWow64);
        }

        return isWow64;
    }

    private static bool MethodExistsInDll(string moduleName, string methodName)
    {
        var moduleHandle = GetModuleHandle(moduleName);
        if (moduleHandle == IntPtr.Zero)
        {
            return false;
        }

        return GetProcAddress(moduleHandle, methodName) != IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UnicodeString
    {
        public ushort Length;

        public ushort MaximumLength;

        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RtlUserProcessParameters
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Reserved1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public IntPtr[] Reserved2;

        public UnicodeString ImagePathName;
        public UnicodeString CommandLine;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Peb32
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Reserved1;

        public byte BeingDebugged;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] Reserved2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public IntPtr[] Reserved3;

        public IntPtr Ldr;
        public IntPtr ProcessParameters;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public IntPtr[] Reserved4;

        public IntPtr AtlThunkSListPtr;
        public IntPtr Reserved5;
        public uint Reserved6;
        public IntPtr Reserved7;
        public uint Reserved8;
        public uint AtlThunkSListPtr32;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 45)]
        public IntPtr[] Reserved9;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public byte[] Reserved10;

        public IntPtr PostProcessInitRoutine;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] Reserved11;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public IntPtr[] Reserved12;

        public uint SessionId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Peb64
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Reserved1;

        public byte BeingDebugged;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
        public byte[] Reserved2;

        public IntPtr LoaderData;
        public IntPtr ProcessParameters;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 520)]
        public byte[] Reserved3;

        public IntPtr PostProcessInitRoutine;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 136)]
        public byte[] Reserved4;

        public uint SessionId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public IntPtr[] Reserved2;

        public IntPtr UniqueProcessID;
        public IntPtr Reserved3;
    }

    [DllImport("ntdll.dll", EntryPoint = "RtlNtStatusToDosError", SetLastError = true)]
    private static extern int RtlNtStatusToDosError(int ntStatus);

    [DllImport("kernel32.dll", EntryPoint = "IsWow64Process", SetLastError = true)]
    private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandle", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string methodName);

    [DllImport("ntdll.dll", EntryPoint = "NtQueryInformationProcess", SetLastError = true)]
    private static extern int NtQueryInformationProcess(IntPtr handle, ProcessInfoClass processInformationClass, ref ProcessBasicInformation processInformation, int processInformationLength,
        ref uint returnLength);

    [DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int  dwSize, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAlloc(
        IntPtr lpAddress,
        uint dwSize,
        AllocationType flAllocationType,
        MemoryProtection flProtect
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualFree(
        IntPtr lpAddress,
        uint dwSize,
        FreeType dwFreeType
    );
    
    /// <summary>
    ///  After first call with 0, max level is returned in EAX 
    ///  Level = 0..EAX,
    ///  Buffer = 16+ bytes
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetCpuIdDelegate(uint level, byte[] buffer);

    [Flags]
    public enum AllocationType : uint
    {
        COMMIT = 0x1000,
        RESERVE = 0x2000,
        RESET = 0x80000,
        LARGE_PAGES = 0x20000000,
        PHYSICAL = 0x400000,
        TOP_DOWN = 0x100000,
        WRITE_WATCH = 0x200000
    }
    
    [Flags]
    public enum FreeType
    {
        DECOMMIT = 0x4000,
        RELEASE = 0x8000,
    }

    [Flags]
    public enum MemoryProtection : uint
    {
        EXECUTE = 0x10,
        EXECUTE_READ = 0x20,
        EXECUTE_READWRITE = 0x40,
        EXECUTE_WRITECOPY = 0x80,
        NOACCESS = 0x01,
        READONLY = 0x02,
        READWRITE = 0x04,
        WRITECOPY = 0x08,
        GUARD_Modifierflag = 0x100,
        NOCACHE_Modifierflag = 0x200,
        WRITECOMBINE_Modifierflag = 0x400
    }
    
    public static T GetProcAddressOrThrow<T>(Kernel32.SafeLibraryHandle dllHandle, string procName) where T : class
    {
        if (dllHandle.IsInvalid)
        {
            throw new InvalidStateException($"DLL handle is invalid: {dllHandle}");
        }
        if (dllHandle.IsClosed)
        {
            throw new InvalidStateException($"DLL handle is closed: {dllHandle}");
        }
        var ptr = Kernel32.GetProcAddress(dllHandle, procName);
        if (ptr == IntPtr.Zero)
        {
            throw new InvalidStateException($"Failed to find function {procName}");
        }
        var result = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
        if (result is not T typed)
        {
            throw new InvalidStateException($"Failed to cast delegate {result} to {typeof(T)}");
        }
        return typed;
    }
}