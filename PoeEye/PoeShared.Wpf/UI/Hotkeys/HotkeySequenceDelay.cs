using System;
using System.Reactive.Linq;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.UI.Hotkeys
{
    public sealed class HotkeySequenceDelay : HotkeySequenceItem
    {
        private TimeSpan delay;
        private readonly ObservableAsPropertyHelper<bool> isVisible;
        private bool isKeypress;

        public HotkeySequenceDelay(HotkeySequenceEditor owner)
        {
            Observable.CombineLatest(
                    owner.Observe(HotkeySequenceEditor.HideKeypressDelaysProperty)
                        .StartWithDefault()
                        .ToUnit(),
                    this.WhenAnyValue(x => x.IsKeypress).ToUnit()
                )
                .Select(x => !isKeypress || !owner.HideKeypressDelays)
                .ToProperty(out isVisible, this, x => x.IsVisible)
                .AddTo(Anchors);
        }

        public TimeSpan Delay
        {
            get => delay;
            set => RaiseAndSetIfChanged(ref delay, value);
        }

        public bool IsKeypress
        {
            get => isKeypress;
            set => RaiseAndSetIfChanged(ref isKeypress, value);
        }

        public override bool IsVisible => isVisible.Value;
    }
}