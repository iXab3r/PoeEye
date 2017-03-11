using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    using System;
    using System.Windows;

    using JetBrains.Annotations;

    internal interface IPoeWindow
    {
        Rect WindowBounds { get; }

        Rect StashBounds { get; }

        void MoveMouseToStashItem(int itemX, int itemY, StashTabType tabType);

        void SelectStashTabByName(int tabIndex, [NotNull] string[] knownTabs);

        void SelectStashTabByIdx(IStashTab tabToSelect, IStashTab[] tabs);

        void TransferItemFromStash(int itemX, int itemY, StashTabType tabType);

        IntPtr NativeWindowHandle { get; }
    }
}