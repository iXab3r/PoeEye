using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using DynamicData;
using log4net.Appender;
using log4net.Core;
using PoeShared;
using PoeShared.Scaffolding;
using ReactiveUI;
using CommonLogging = Common.Logging;

namespace PoeEye.PoeTrade.Shell.ViewModels
{
    internal sealed class SplashWindowViewModel : DisposableReactiveObject
    {
        private string status;
        private readonly ReadOnlyObservableCollection<string> logMessages;

        public SplashWindowViewModel()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            ApplicationName = $"{executingAssembly.GetName().Name} v{executingAssembly.GetName().Version}";

            Status = $"please wait...";

            var messages = new SourceList<string>();
            messages
                .Connect()
                .ObserveOnDispatcher()
                .Bind(out logMessages)
                .Subscribe()
                .AddTo(Anchors);
            
            var appender = new ObservableAppender();
            appender.Events
                    .Where(x => x.Level == Level.Info)
                    .Sample(TimeSpan.FromMilliseconds(250))
                    .ObserveOnDispatcher()
                    .Subscribe(evt =>
                    {
                        Status = evt.RenderedMessage;
                        messages.Add(evt.RenderedMessage);
                    })
                    .AddTo(Anchors);

            SharedLog.AddAppender(appender).AddTo(Anchors);
        }

        public string Status
        {
            get => status;
            set => this.RaiseAndSetIfChanged(ref status, value);
        }

        public ReadOnlyObservableCollection<String> LogMessages => logMessages;

        public string ApplicationName { get; }

        internal class ObservableAppender : AppenderSkeleton
        {
            private readonly ISubject<LoggingEvent> events = new Subject<LoggingEvent>();

            public IObservable<LoggingEvent> Events => events;

            public ObservableAppender()
            {
            }

            protected override void Append(LoggingEvent loggingEvent)
            {
                events.OnNext(loggingEvent);
            }
        }
    }
}