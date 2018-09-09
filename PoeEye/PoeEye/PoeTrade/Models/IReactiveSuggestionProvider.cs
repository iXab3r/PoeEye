﻿using System.Collections.Generic;
using JetBrains.Annotations;
using ReactiveUI;
using WpfAutoCompleteControls.Editors;

namespace PoeEye.PoeTrade.Models {
    internal interface IReactiveSuggestionProvider : IReactiveObject, ISuggestionProvider
    {
        IEnumerable<string> Items { [CanBeNull] get; [CanBeNull] set; }
    }
}