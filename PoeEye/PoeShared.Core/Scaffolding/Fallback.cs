using System;
using ReactiveUI;

namespace PoeShared.Scaffolding
{
    public sealed class Fallback<T> : ReactiveObject
    {
        private T actualValue;

        public bool HasValue => CheckValue();

        public T Value => HasValue ? actualValue : DefaultValue;

        public T DefaultValue { get; private set; }

        public Fallback<T> SetValue(T value)
        {
            actualValue = value;
            Raise();
            return this;
        }

        public Fallback<T> SetDefaultValue(T value)
        {
            DefaultValue = value;
            Raise();
            return this;
        }

        public Fallback<T> ResetValue()
        {
            return SetValue(default(T));
        }

        private bool CheckValue()
        {
            return !Equals(default(T), actualValue);
        }

        private void Raise()
        {
            this.RaisePropertyChanged(nameof(DefaultValue));
            this.RaisePropertyChanged(nameof(Value));
            this.RaisePropertyChanged(nameof(HasValue));
        }
    }
}