﻿using System.Collections.Generic;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Prism.Modularity;
using ReactiveUI;

namespace PoeShared.Modularity
{
    public interface IPoeEyeModulesRegistrator 
    {
        [NotNull] 
        IPoeEyeModulesRegistrator RegisterSettingsEditor<TConfig, TSettingsViewModel>()
            where TConfig : class, IPoeEyeConfig, new()
            where TSettingsViewModel : ISettingsViewModel<TConfig>;
    }

    public interface IPoeEyeModulesEnumerator
    {
        IReactiveList<ISettingsViewModel> Settings { [NotNull] get; }
    }
}