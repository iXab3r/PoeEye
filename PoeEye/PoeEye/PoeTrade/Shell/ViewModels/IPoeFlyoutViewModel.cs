﻿using JetBrains.Annotations;
using MahApps.Metro.Controls;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.PoeTrade.Shell.ViewModels
{
    public interface IPoeFlyoutViewModel : IDisposableReactiveObject
    {
        bool IsOpen { get; set; }

        Position Position { get; }

        string Header { [CanBeNull] get; }
    }

    internal sealed class TestPoeFlyoutViewModel : DisposableReactiveObject, IPoeFlyoutViewModel
    {
        private bool isOpen;

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        public string Header => "Test";

        public Position Position => Position.Bottom;
    }
}