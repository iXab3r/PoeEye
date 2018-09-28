using System;
using JetBrains.Annotations;
using PoeEye.StashGrid.Models;
using PoeShared.Common;
using PoeShared.StashApi.DataTypes;

namespace PoeEye.StashGrid.Services
{
    public interface IPoeStashHighlightService : IDisposable
    {
        [NotNull] 
        IGridCellViewController AddHighlight(ItemPosition pos, StashTabType stashType);
    }
}