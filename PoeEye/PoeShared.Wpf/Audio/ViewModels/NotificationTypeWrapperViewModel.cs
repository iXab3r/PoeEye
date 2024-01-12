using System;
using System.Reactive.Linq;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Audio.ViewModels;

internal sealed class NotificationTypeWrapperViewModel : DisposableReactiveObject
{
    private readonly ObservableAsPropertyHelper<bool> isSelectedSupplier;

    public NotificationTypeWrapperViewModel(
        IAudioNotificationSelectorViewModel owner,
        string value,
        string name)
    {
        Owner = owner;
        Value = value;
        Name = name;

        isSelectedSupplier = owner
            .WhenAnyValue(x => x.SelectedValue)
            .Select(x => Value.Equals(x, StringComparison.OrdinalIgnoreCase))
            .ToProperty(this, x => x.IsSelected)
            .AddTo(Anchors);
    }

    public bool IsSelected => isSelectedSupplier.Value;

    public string Value { get; }

    public string Name { get; }
        
    public IAudioNotificationSelectorViewModel Owner { get; }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append("Notification");
        builder.AppendParameter(nameof(Name), Name);
        builder.AppendParameter(nameof(Value), Value);
    }
}