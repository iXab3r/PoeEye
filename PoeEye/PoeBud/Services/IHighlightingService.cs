using System;
using JetBrains.Annotations;
using PoeBud.Models;

namespace PoeBud.Services
{
    internal interface IHighlightingService
    {
        IDisposable Highlight([CanBeNull] IPoeTradeSolution solution);
        
        IDisposable Highlight([CanBeNull] IPoeTradeSolution solution, TimeSpan duration);
    }
}