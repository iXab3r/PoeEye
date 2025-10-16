using System.Collections.Immutable;
using System.IO;

namespace PoeShared.Dialogs.Services;

public interface IOpenFileDialog : IFileDialog
{
    FileInfo ShowDialog();
    
    ImmutableArray<FileInfo> ShowDialogMultiselect();

    FileInfo LastFile { get; }
}