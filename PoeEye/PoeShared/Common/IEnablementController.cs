using System.ComponentModel;
using System.Reactive;
using ReactiveUI;

namespace PoeShared.Common;

public interface IEnablementController : IDisposableReactiveObject
{
    AnnotatedBoolean IsEnabled { get; }

    IDisposable Disable(string reason = default);
}