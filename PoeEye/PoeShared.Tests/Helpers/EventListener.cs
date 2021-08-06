using System;
using System.Collections.Generic;

namespace PoeShared.Tests.Helpers
{
    public sealed class EventListener<T> where T : EventArgs
    {
        private readonly List<T> savedArgs = new();

        public EventListener(object raiser, string eventName)
        {
            var eventInfo = raiser.GetType().GetEvent(eventName);
            var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, "EventHandler");
            eventInfo.AddEventHandler(raiser, handler);
        }

        public IList<T> SavedArgs => savedArgs;

        private void EventHandler(object sender, T args)
        {
            savedArgs.Add(args);
        }
    }
}