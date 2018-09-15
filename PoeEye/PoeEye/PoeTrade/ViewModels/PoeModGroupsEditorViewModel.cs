using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeShared.PoeTrade;
using PoeShared.PoeTrade.Query;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Commands;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeModGroupsEditorViewModel : DisposableReactiveObject, IPoeModGroupsEditorViewModel
    {
        private readonly IFactory<PoeModsEditorViewModel, IPoeStaticDataSource> groupsFactory;
        private readonly IPoeStaticDataSource staticDataSource;

        public PoeModGroupsEditorViewModel(
            [NotNull] IPoeStaticDataSource staticDataSource,
            [NotNull] IFactory<PoeModsEditorViewModel, IPoeStaticDataSource> groupsFactory)
        {
            Guard.ArgumentNotNull(groupsFactory, nameof(groupsFactory));

            this.staticDataSource = staticDataSource;
            this.groupsFactory = groupsFactory;

            AddGroupCommand = new DelegateCommand(() => AddGroup());

            RemoveGroupCommand = new DelegateCommand<IPoeModsEditorViewModel>(RemoveGroupCommandExecuted);

            AddGroup();
        }

        public ICommand AddGroupCommand { get; }

        public ICommand RemoveGroupCommand { get; }

        public IReactiveList<IPoeModsEditorViewModel> Groups { get; } = new ReactiveList<IPoeModsEditorViewModel> {ChangeTrackingEnabled = true};

        public IPoeModsEditorViewModel AddGroup()
        {
            var newGroup = groupsFactory.Create(staticDataSource);

            Groups.Add(newGroup);

            return newGroup;
        }

        public IPoeQueryModsGroup[] ToGroups()
        {
            return Groups.Select(x => x.ToGroup()).ToArray();
        }

        private void RemoveGroupCommandExecuted(IPoeModsEditorViewModel groupToRemove)
        {
            Guard.ArgumentNotNull(groupToRemove, nameof(groupToRemove));

            using (Groups.SuppressChangeNotifications())
            {
                Groups.Remove(groupToRemove);
            }
        }
    }
}