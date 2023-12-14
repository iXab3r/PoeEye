using System;
using PoeShared.Scaffolding;

namespace PoeShared.Blazor;

public interface IReactiveComponent : IDisposableReactiveObject, IRefreshableComponent, IAsyncDisposable
{
}