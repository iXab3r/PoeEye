namespace PoeWhisperMonitor
{
    using System.Collections.Generic;
    using System.IO;

    internal struct PoeProcessInfo
    {
        public int ProcessId { get; set; }

        public FileInfo Executable { get; set; }

        private sealed class ExecutableEqualityComparer : IEqualityComparer<PoeProcessInfo>
        {
            public bool Equals(PoeProcessInfo x, PoeProcessInfo y)
            {
                return Equals(x.Executable?.FullName, y.Executable?.FullName);
            }

            public int GetHashCode(PoeProcessInfo obj)
            {
                return obj.Executable?.GetHashCode() ?? 0;
            }
        }

        public static IEqualityComparer<PoeProcessInfo> ExecutableComparer { get; } = new ExecutableEqualityComparer();

        public override string ToString()
        {
            return $"[PoE] '{Executable?.FullName}' (PID 0x{ProcessId:X8})";
        }
    }
}