using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.Services;

public interface IFileDialog : IDisposableReactiveObject
{
    string Title { get; set; }
   
    string InitialDirectory { get; set; }
   
    string InitialFileName { get; set; }
   
    string Filter { get; set; }
}