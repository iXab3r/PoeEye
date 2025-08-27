using System.Windows.Input;

namespace PoeShared.Scaffolding;

public interface ICommandWrapper : ICommand
{
    bool IsBusy { get; }
    
    string Error { get; }

    string Description { get; }
   
    IObservable<object> WhenExecuted { get; }
   
    Task ExecuteAsync(object parameter = default);

    bool CanExecute() => CanExecute(null);

    void Execute() => Execute(null);
}