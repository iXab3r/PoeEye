﻿using PoeShared.Squirrel.Updater;
using Unity;
using Unity.Extension;

namespace PoeShared.Squirrel.Prism
{
    public sealed class UpdaterRegistrations : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Container
                .RegisterSingleton<IUpdateSourceProvider, UpdateSourceProviderFromConfig>()
                .RegisterType<IApplicationUpdaterViewModel, ApplicationUpdaterViewModel>()
                .RegisterType<IApplicationUpdaterModel, ApplicationUpdaterModel>();
        }
    }
}