using System;
using System.Reactive;
using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using PoeShared.Common;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.ViewModels
{
    public interface INegotiationViewModel : IDisposableReactiveObject
    {
        string CharacterName { get; }

        PoePrice Price { get; }

        bool IsExpanded { get; set; }

        void SetCloseController([NotNull] INegotiationCloseController closeController);
    }
}