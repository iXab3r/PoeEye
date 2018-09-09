using System;
using System.Windows;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    internal interface IPoeWindow
    {
        Rect WindowBounds { get; }

        Rect StashBounds { get; }

        IntPtr NativeWindowHandle { get; }

        void MoveMouseToStashItem(int itemX, int itemY, StashTabType tabType);

        void SelectStashTabByIdx(IStashTab tabToSelect, IStashTab[] tabs);

        void TransferItemFromStash(int itemX, int itemY, StashTabType tabType);
    }
}