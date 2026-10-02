using System;
using System.Collections.Immutable;
using System.IO;

namespace PoeShared.Dialogs.Services;

/// <summary>Opens files through a host-provided native dialog; callers own the UI thread and window context.</summary>
public interface IOpenFileDialog : IFileDialog
{
    FileInfo ShowDialog();

    /// <summary>
    /// Opens a file dialog owned by the originating window. Invoke on that window's UI dispatcher.
    /// The WPF backend requires a live WPF window HWND; cancellation returns no file.
    /// </summary>
    FileInfo ShowDialog(IntPtr hwndOwner) => throw new NotSupportedException("This backend does not support an explicit owner");
    
    ImmutableArray<FileInfo> ShowDialogMultiselect();

    FileInfo LastFile { get; }
}