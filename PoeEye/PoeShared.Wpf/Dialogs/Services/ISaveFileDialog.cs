using System.IO;

namespace PoeShared.Dialogs.Services;

public interface ISaveFileDialog : IFileDialog
{
   FileInfo ShowDialog();
   
   FileInfo LastFile { get; }
}