namespace PoeWhisperMonitor
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using JetBrains.Annotations;

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
                .Select(x => x.Select(ToProcessInfo).Where(y => !default(PoeProcessInfo).Equals(y)).ToArray())
                .WithPrevious((prev, curr) => new {IsNew = !(prev ?? new PoeProcessInfo[0]).SequenceEqual(curr, PoeProcessInfo.ExecutableComparer), curr})
                .Where(x => x.IsNew)
                .Select(x => x.curr)
                .Do(LogProcesses)
                .Subscribe(processeSubject)
                .AddTo(Anchors);
        }

        public IObservable<PoeProcessInfo[]> ActiveProcesses => processeSubject;

        private Process[] GetPathOfExileProcesses()
        {
            try
            {
                var result = Process.GetProcessesByName(PathOfExileProcessName).OrderBy(x => x.Id).ToArray();
                return result;
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                return new Process[0];
            }
        }

        private void LogProcesses(PoeProcessInfo[] processes)
        {
            Log.Instance.Debug($"[PoeTracker] Processes list have changed(count: {processes.Length}): \r\n\t{string.Join("\r\n\t", processes)}");
        }

        [CanBeNull]
        private PoeProcessInfo ToProcessInfo(Process process)
        {
            try
            {
                var result = new PoeProcessInfo
                {
                    ProcessId = process.Id,
                    Executable = new FileInfo(process.MainModule.FileName)
                };

                return result;
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                return default(PoeProcessInfo);
            }
            
        }
    }
}