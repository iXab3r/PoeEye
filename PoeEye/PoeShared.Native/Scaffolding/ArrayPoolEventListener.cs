using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using ByteSizeLib;
using PoeShared.Logging;

namespace PoeShared.Scaffolding;

public sealed class ArrayPoolEventListener : DisposableReactiveObjectWithLogger
{
    public ArrayPoolEventListener()
    {
        var guid = Guid.Parse("0866B2B8-5CEF-5DB9-2612-0C0FFD814A44");
        var eventSource = EventSource
            .GetSources()
            .FirstOrDefault(x => x.Guid == guid);
        if (eventSource == null)
        {
            throw new InvalidOperationException($"Failed to find matching source:\n\t{EventSource.GetSources().Select(x => new {x.Name, x.Guid}).DumpToTable()}");
        }

        var listener = new CustomEventListener(Log, eventSource.Name).AddTo(Anchors);
        listener.EnableEvents(eventSource, EventLevel.Verbose);
    }

    private class CustomEventListener : EventListener
    {
        public CustomEventListener(IFluentLog log, string name)
        {
            Log = log.WithSuffix($"Listener {name}");
        }

        private IFluentLog Log { get; }
        
        [ThreadStatic] private static bool isProcessing;

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (isProcessing)
            {
                return;
            }
            isProcessing = true;
            try
            {
                // bufferId + bufferSize
                if (eventData.EventName == null || eventData.Payload == null || eventData.Payload.Count < 2)
                {
                    return;
                }

                if (!Enum.TryParse<BufferEvent>(eventData.EventName, out var eventType))
                {
                    return;
                }

                if (eventData.Payload[0] is not int bufferId || eventData.Payload[1] is not int bufferSize)
                {
                    //malformed data
                    return;
                }

                const int minBufferSize = 1_000_000; 
                const int minBufferSizeToLogStackTrace = minBufferSize * 4; 
                if (bufferSize < minBufferSize)
                {
                    return;
                }
                
                if (eventType is BufferEvent.BufferRented or BufferEvent.BufferReturned)
                {
                    return;
                }

                var message = new ToStringBuilder(this);
                message.Append($"{eventData.EventName} (ID: {eventData.EventId})");
                
                if (eventData is {Payload: not null, PayloadNames: not null})
                {
                    for (var i = 0; i < eventData.Payload.Count; i++)
                    {
                        var payloadName = eventData.PayloadNames[i];
                        var payloadValue = eventData.Payload[i];
                        message.AppendParameter(payloadName, payloadValue);
                    }
                }

                var footer = new StringBuilder();
                if (eventType is BufferEvent.BufferAllocated && bufferSize > minBufferSizeToLogStackTrace)
                {
                    //footer.Append(new StackTrace());
                }
                Log.Info($"{message}{(footer.Length == 0 ? string.Empty : footer.ToString())}");
                
            }
            finally
            {
                isProcessing = false;
            }
        }
    }
    
    /// <summary>The reason for a BufferAllocated event.</summary>
    internal enum BufferEvent : int
    {
        BufferRented,
        BufferAllocated,
        BufferReturned,
        BufferTrimmed,
        BufferTrimPoll,
        BufferDropped,
    }
    
    /// <summary>The reason for a BufferAllocated event.</summary>
    internal enum BufferAllocatedReason : int
    {
        /// <summary>The pool is allocating a buffer to be pooled in a bucket.</summary>
        Pooled,
        /// <summary>The requested buffer size was too large to be pooled.</summary>
        OverMaximumSize,
        /// <summary>The pool has already allocated for pooling as many buffers of a particular size as it's allowed.</summary>
        PoolExhausted
    }

    /// <summary>The reason for a BufferDropped event.</summary>
    internal enum BufferDroppedReason : int
    {
        /// <summary>The pool is full for buffers of the specified size.</summary>
        Full,
        /// <summary>The buffer size was too large to be pooled.</summary>
        OverMaximumSize,
    }
}