using System;
using ReactiveUI;

namespace PoeShared.Scaffolding
{
    public sealed class Fallback<T> : ReactiveObject where T : class
    {
        private T defaultValue;

        private T actualValue;

        public bool HasValue => CheckValue();

        public T Value => HasValue ? actualValue : defaultValue;

        public T DefaultValue => defaultValue;

        public Fallback<T> SetValue(T value)
        {
            actualValue = value;
            Raise();
            return this;
        }

        public Fallback<T> SetDefaultValue(T value)
        {
            defaultValue = value;
            Raise();
            return this;
        }

        public Fallback<T> ResetValue()
        {
            return SetValue(default(T));
        }

        private bool CheckValue()
        {
            return default(T) != actualValue;
        }

        private void Raise()
        {
            this.RaisePropertyChanged(nameof(DefaultValue));
            this.RaisePropertyChanged(nameof(Value));
            this.RaisePropertyChanged(nameof(HasValue));
        }
    }
}