using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeBud.Models;
using ReactiveUI;

namespace PoeBud.ViewModels
{
    internal interface ISolutionExecutorViewModel
    {
        bool IsBusy { get; set; }

        IReactiveList<string> PerformedOperations { [NotNull] get; }

        Task ExecuteSolution([NotNull] IPoeTradeSolution solutionToExecute);
        
        void LogOperation([NotNull] string logMessage);
    }
}