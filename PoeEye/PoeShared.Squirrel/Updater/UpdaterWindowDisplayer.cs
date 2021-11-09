﻿using System.Windows;
using PoeShared.Native;
using PoeShared.Prism;

namespace PoeShared.Squirrel.Updater
{
    internal sealed class UpdaterWindowDisplayer : IUpdaterWindowDisplayer
    {
        private readonly IFactory<IUpdaterWindowViewModel, IWindowViewController, UpdaterWindowArgs> updaterWindowFactory;
        private readonly IFactory<UpdaterWindow> windowFactory;
        
        public UpdaterWindowDisplayer(
            IFactory<IUpdaterWindowViewModel, IWindowViewController, UpdaterWindowArgs> updaterWindowFactory,
            IFactory<UpdaterWindow> windowFactory)
        {
            this.updaterWindowFactory = updaterWindowFactory;
            this.windowFactory = windowFactory;
        }

        public bool? ShowDialog(UpdaterWindowArgs args)
        {
            var window = windowFactory.Create();
            var viewController = new WindowViewController(window);
            using var updaterWindowViewModel = updaterWindowFactory.Create(viewController, args);
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.DataContext = updaterWindowViewModel;
                return window.ShowDialog();
            }
        }
    }
}