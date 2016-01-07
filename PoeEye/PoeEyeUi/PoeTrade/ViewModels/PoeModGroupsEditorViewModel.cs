namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Input;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared.PoeTrade.Query;
    using PoeShared.Utilities;

    using ReactiveUI;

    internal sealed class PoeModGroupsEditorViewModel : DisposableReactiveObject, IPoeModGroupsEditorViewModel
    {
        private readonly IFactory<PoeModsEditorViewModel> groupsFactory;

        private readonly ReactiveCommand<object> addGrpCommand = ReactiveCommand.Create();
        private readonly ReactiveCommand<object> removeGrpCommand = ReactiveCommand.Create();

        public PoeModGroupsEditorViewModel([NotNull] IFactory<PoeModsEditorViewModel> groupsFactory)
        {
            Guard.ArgumentNotNull(() => groupsFactory);

            this.groupsFactory = groupsFactory;

            addGrpCommand
              .Subscribe(_ => AddGroup())
              .AddTo(Anchors);

            removeGrpCommand
                .OfType<PoeModsEditorViewModel>()
                .Subscribe(RemoveGroupCommandExecuted)
                .AddTo(Anchors);

            AddGroup();
        }

        public IReactiveList<IPoeModsEditorViewModel> Groups { get; } = new ReactiveList<IPoeModsEditorViewModel>() { ChangeTrackingEnabled = true };

        public ICommand AddGroupCommand => addGrpCommand;

        public ICommand RemoveGroupCommand => removeGrpCommand;

        private void RemoveGroupCommandExecuted(IPoeModsEditorViewModel groupToRemove)
        {
            Guard.ArgumentNotNull(() => groupToRemove);

            using (Groups.SuppressChangeNotifications())
            {
                Groups.Remove(groupToRemove);
            }
        }

        public IPoeModsEditorViewModel AddGroup()
        {
            var newGroup = groupsFactory.Create();

            Groups.Add(newGroup);

            return newGroup;
        }

        public IPoeQueryModsGroup[] ToGroups()
        {
            return Groups.Select(x => x.ToGroup()).ToArray();
        }
    }
}