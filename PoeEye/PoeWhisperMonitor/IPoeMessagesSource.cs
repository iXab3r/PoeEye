using System;
using PoeWhisperMonitor.Chat;

namespace PoeWhisperMonitor
{
    internal interface IPoeMessagesSource : IDisposable
    {
        int MaxLinesBufferSize { get; set; }

        IObservable<PoeMessage> Messages { get; }
    }
}