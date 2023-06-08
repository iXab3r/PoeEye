using System.IO;
using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.Services;

public interface IFolderBrowserDialog : IDisposableReactiveObject
{
    DirectoryInfo ShowDialog();
   
    DirectoryInfo LastDirectory { get; }
    
    string Title { get; set; }
   
    string InitialDirectory { get; set; }
}