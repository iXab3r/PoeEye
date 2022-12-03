using System.IO;

namespace PoeShared.Dialogs.Services;

public interface IOpenFileDialog : IFileDialog
{
    FileInfo ShowDialog();
    
    FileInfo LastFile { get; }
}