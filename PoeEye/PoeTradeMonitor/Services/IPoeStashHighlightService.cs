using System;
using System.Windows;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.ViewModels;
using PoeShared.StashApi.DataTypes;

namespace PoeEye.TradeMonitor.Services
{
    internal interface IPoeStashHighlightService : IDisposable
    {
        IGridCellViewController AddHighlight(ItemPosition pos, StashTabType stashType);
    }
}