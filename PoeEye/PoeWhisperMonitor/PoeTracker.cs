namespace PoeWhisperMonitor
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using PoeShared;
    using PoeShared.Scaffolding;

    internal sealed class PoeTracker : DisposableReactiveObject
    {
        private static readonly string PathOfExileProcessName = "PathOfExile";

        private static readonly TimeSpan RecheckTimeout = TimeSpan.FromSeconds(10);

        private readonly ISubject<PoeProcessInfo[]> processeSubject = new Subject<PoeProcessInfo[]>();

        public PoeTracker()
        {
            Observable
                .Timer(DateTimeOffset.Now, RecheckTimeout)
                .Select(_ => GetPathOfExileProcesses())
                .Select(x => x.Select(ToProcessInfo).ToArray())
                .WithPrevious((prev, curr) => new {IsNew = !(prev ?? new PoeProcessInfo[0]).SequenceEqual(curr, PoeProcessInfo.ExecutableComparer), curr})
                .Where(x => x.IsNew)
                .Select(x => x.curr)
                .Do(LogProcesses)
                .Subscribe(processeSubject);
        }

        public IObservable<PoeProcessInfo[]> ActiveProcesses => processeSubject;

        private Process[] GetPathOfExileProcesses()
        {
            var result = Process.GetProcessesByName(PathOfExileProcessName).OrderBy(x => x.Id).ToArray();
            return result;
        }

        private void LogProcesses(PoeProcessInfo[] processes)
        {
            Log.Instance.Debug($"[PoeTracker] Processes list have changed(count: {processes.Length}): \r\n\t{string.Join("\r\n\t", processes)}");
        }

        public PoeProcessInfo ToProcessInfo(Process process)
        {
            var result = new PoeProcessInfo
            {
                ProcessId = process.Id,
                Executable = new FileInfo(process.MainModule.FileName)
            };

            return result;
        }
    }
}