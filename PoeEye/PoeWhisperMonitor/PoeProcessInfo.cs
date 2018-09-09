using System;
using System.Collections.Generic;
using System.IO;

namespace PoeWhisperMonitor
{
    internal struct PoeProcessInfo
    {
        public int ProcessId { get; set; }

        public FileInfo Executable { get; set; }

        public IntPtr MainWindow { get; set; }

        private sealed class ExecutableEqualityComparer : IEqualityComparer<PoeProcessInfo>
        {
            public bool Equals(PoeProcessInfo x, PoeProcessInfo y)
            {
                return x.ProcessId == y.ProcessId;
            }

            public int GetHashCode(PoeProcessInfo obj)
            {
                unchecked
                {
                    return (obj.ProcessId*397) ^ (obj.Executable?.GetHashCode() ?? 0);
                }
            }
        }

        public static IEqualityComparer<PoeProcessInfo> ExecutableComparer { get; } = new ExecutableEqualityComparer();

        public override string ToString()
        {
            return $"[PoE] '{Executable?.FullName}' (PID 0x{ProcessId:X8}, HWND 0x{MainWindow.ToInt64():X8})";
        }
    }
}