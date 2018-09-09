using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace PoeBud.Models
{
    internal interface ISolutionExecutorModel
    {
        IObservable<string> Messages { [NotNull] get; }

        [NotNull]
        Task ExecuteSolution([NotNull] IPoeTradeSolution solutionToExecute);

        [NotNull]
        Task ExecuteSolution([NotNull] IPoeTradeSolution solutionToExecute, CancellationToken token);
    }
}