using PoeBud.Models;
using ReactiveUI;

namespace PoeBud.ViewModels
{
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    internal interface ISolutionExecutorViewModel
    {
        bool IsBusy { get; set; }

        IReactiveList<string> PerformedOperations { [NotNull] get; }

        Task ExecuteSolution([NotNull] IPoeTradeSolution solutionToExecute);
    }
}