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
using ReactiveUI;
using ReactiveUI.Legacy;

namespace PoeEye.PoeTrade.ViewModels
{
    internal sealed class PoeModGroupsEditorViewModel : DisposableReactiveObject, IPoeModGroupsEditorViewModel
    {
        private readonly ReactiveCommand<object> addGrpCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
        private readonly ReactiveCommand<object> removeGrpCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
        private readonly IPoeStaticDataSource staticDataSource;
        private readonly IFactory<PoeModsEditorViewModel, IPoeStaticDataSource> groupsFactory;

        public PoeModGroupsEditorViewModel(
            [NotNull] IPoeStaticDataSource staticDataSource,
            [NotNull] IFactory<PoeModsEditorViewModel, IPoeStaticDataSource> groupsFactory)
        {
            Guard.ArgumentNotNull(groupsFactory, nameof(groupsFactory));

            this.staticDataSource = staticDataSource;
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

        public ICommand AddGroupCommand => addGrpCommand;

        public ICommand RemoveGroupCommand => removeGrpCommand;

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
