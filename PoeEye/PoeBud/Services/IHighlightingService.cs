using JetBrains.Annotations;
using PoeBud.Models;

namespace PoeBud.Services
{
    internal interface IHighlightingService
    {
        void Highlight([NotNull] IPoeTradeSolution solution);
    }
}