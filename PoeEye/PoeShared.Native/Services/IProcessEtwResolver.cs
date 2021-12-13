using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using PoeShared.Logging;
using PoeShared.Modularity;
using PoeShared.Scaffolding;

namespace PoeShared.Services
{
    public class ProcessEtwResolver : DisposableReactiveObject
    {
        private readonly NamedLock statsLock = new NamedLock("StatsLock");
        private readonly ConcurrentDictionary<ProcessKey, ProcessDataHolder> processData = new();
        private readonly Thread etwThread;
        private readonly TraceEventSession etwSession;

        public ProcessEtwResolver(
            
            IAppArguments appArguments)
        {
            Log = GetType().PrepareLogger().WithSuffix(() => SessionKey);
            var appKey = string.IsNullOrEmpty(appArguments.AppName) 
                ? Process.GetCurrentProcess().ProcessName 
                : appArguments.AppName;
            SessionKey = $"{appKey}-{(appArguments.IsDebugMode ? "DEBUG" : null)}";
            Log.Debug(() => $"ETW tracker created");
            Log.Debug(() => $"Creating ETW session");
            etwSession = new TraceEventSession(SessionKey).AddTo(Anchors);
            Log.Debug(() => $"Created ETW session: {etwSession}");
            etwThread = new Thread(EtwLoop)
            {
                IsBackground = true,
                Name = "ETW"
            };
            Log.Debug(() => $"Starting ETW thread");
            etwThread.Start();
            Log.Debug(() => $"Started ETW thread");
            Observable.Timer(DateTimeOffset.Now, TimeSpan.FromSeconds(1))
                .Select(x => etwThread.IsAlive && etwSession.IsActive)
                .DistinctUntilChanged()
                .Subscribe(x => IsActive = x)
                .AddTo(Anchors);
        }

        public bool IsActive { get; private set; }

        public bool TryGetProcessDataById(int processId, out IProcessEtwData result)
        {
            if (!processData.TryGetValue(new ProcessKey(processId), out var data))
            {
                result = default;
                return false;
            }
            result = data;
            return true;
        }
        
        public void AddProcessById(int processId)
        {
            var processKey = new ProcessKey(processId);
            Log.Debug(() => $"Trying to track process {processKey}");
            processData.AddOrUpdate(processKey, key =>
            {
                Log.Debug(() => $"Adding new tracker for process {processKey}");
                return new ProcessDataHolder(key);
            }, (key, holder) =>
            {
                Log.Debug(() => $"Process {processKey} is already tracked, data: {holder}");
                return holder;
            });
        }

        public void RemoveProcessById(int processId)
        {
            var processKey = new ProcessKey(processId);
            Log.Debug(() => $"Trying to untrack process {processKey}");
            if (processData.TryRemove(processKey, out var data))
            {
                Log.Debug(() => $"Process {processKey} is not longer tracked, latest data: {data}");
            }
            else
            {
                Log.Debug(() => $"Process {processKey} is not tracked anyways");
            }
        }
        
        public string SessionKey { get; }
        
        private IFluentLog Log { get; }

        private static bool IsMatch(ProcessKey processKey, TraceEvent etwEvent)
        {
            return etwEvent.ProcessID == processKey.ProcessId;
        }

        private static ProcessKey CreateKey(TraceEvent etwEvent)
        {
            return new ProcessKey(etwEvent.ProcessID);
        }
        
        private void EtwLoop()
        {
            try
            {
                Log.Debug(() => $"ETW loop thread started");

                var etwFlags = KernelTraceEventParser.Keywords.NetworkTCPIP;
                if (!etwSession.EnableKernelProvider(etwFlags))
                {
                    throw new InvalidStateException($"Failed to enable ETW session kernel provider using flags {etwFlags}");
                }
                
                UpdateDataIfNeeded(etwSession.Source.Kernel.Observe<TcpIpSendTraceData>(), (data, holder) =>
                {
                    var connectionData = holder.Connections.GetOrAdd(data.connid, arg => new ProcessEtwNetworkConnectionData(arg));
                    connectionData.Destination = data.daddr;
                    connectionData.Source = data.saddr;
                    connectionData.DestinationPort = data.dport;
                    connectionData.SourcePort = data.dport;
                }).AddTo(Anchors);
                
                UpdateDataIfNeeded(etwSession.Source.Kernel.Observe<TcpIpConnectTraceData>(), (data, holder) =>
                {
                    var connectionData = holder.Connections.GetOrAdd(data.connid, arg => new ProcessEtwNetworkConnectionData(arg));
                    connectionData.Destination = data.daddr;
                    connectionData.Source = data.saddr;
                    connectionData.DestinationPort = data.dport;
                    connectionData.SourcePort = data.dport;
                }).AddTo(Anchors);

                etwSession.Source.Kernel.TcpIpRecv += data =>
                {

                };
                
                UpdateDataIfNeeded(etwSession.Source.Kernel.Observe<TcpIpTraceData>(x => x.Contains("Disconnect")), (data, holder) =>
                {
                    holder.Connections.TryRemove(data.connid, out var removed);
                }).AddTo(Anchors);

                Log.Debug(() => $"Starting ETW processing");
                etwSession.Source.Process();
                Log.Debug(() => $"Completed ETW processing");
            }
            catch (Exception e)
            {
                Log.Error("Error in ETW loop thread", e);
            }
            finally
            {
                Log.Debug(() => $"ETW loop thread completed");
            }
        }

        private IDisposable UpdateDataIfNeeded<T>(IObservable<T> dataSource, Action<T, ProcessDataHolder> updateFunc) where T : TraceEvent
        {
            return dataSource
                .Subscribe(data =>
                {
                    var key = CreateKey(data);
                    if (!processData.TryGetValue(key, out var existingData))
                    {
                        return;
                    }
                    Log.WithSuffix($"ETW: {typeof(T)}").Debug(() => $"Event received: {data}");
                    using var updateLock = existingData.Rent();
                    updateFunc(data, existingData);
                });
        }

        private sealed record ProcessKey
        {
            public ProcessKey(int processId)
            {
                ProcessId = processId;
            }

            public int ProcessId { get; init; }
        }

        private sealed record ProcessDataHolder : IProcessEtwData
        {
            private readonly NamedLock gate;
            public ConcurrentDictionary<ulong, ProcessEtwNetworkConnectionData> Connections { get; } = new();
            IDictionary<ulong, ProcessEtwNetworkConnectionData> IProcessEtwData.Connections => Connections;

            public ProcessDataHolder(ProcessKey key)
            {
                Key = key;
                gate = new NamedLock($"ProcessData#{key}");
            }

            public ProcessKey Key { get; }

            public int ProcessId => Key.ProcessId;

            public string ProcessName { get; set; }

            public IDisposable Rent()
            {
                return gate.Enter();
            }
        }
    }
}