namespace PoeWhisperMonitor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Runtime.CompilerServices;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.Utilities;

    using TrackingStreamLib;

    internal sealed class StreamTracker : DisposableReactiveObject
    {
        private readonly TrackingStream trackingStream;

        public ISubject<string> Lines { get; } = new Subject<string>();

        public StreamTracker([NotNull] Stream baseStream)
        {
            Guard.ArgumentNotNull(() => baseStream);
            if (!baseStream.CanSeek)
            {
                throw new ArgumentException("Stream must support Seek operation");
            }
            if (!baseStream.CanRead)
            {
                throw new ArgumentException("Stream must support Read operation");
            }

            trackingStream = new TrackingStream(baseStream);
            trackingStream.Position = trackingStream.Length;

            Observable
                .FromEventPattern<EventHandler, EventArgs>(h => trackingStream.StreamChanged += h, h => trackingStream.StreamChanged -= h)
                .Select(x => ReadNextPackOfLines())
                .Where(x => x.Any())
                .SelectMany(x => x)
                .Subscribe(Lines)
                .AddTo(Anchors);

            Anchors.Add(trackingStream);

            trackingStream.StartTracking();
        }

        private string[] ReadNextPackOfLines()
        {
            var length = trackingStream.Length;
            var position = trackingStream.Position;

            if (length <= position)
            {
                return new string[0];
            }

            using (var reader = new StreamLinesReader(trackingStream))
            {
                var lines = reader.ReadLines().ToArray();

                return lines;
            }
        }
    }
}