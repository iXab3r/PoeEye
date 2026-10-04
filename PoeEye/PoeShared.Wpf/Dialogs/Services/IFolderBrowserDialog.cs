using System;
using System.IO;
using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.Services;

/// <summary>
/// Selects folders through native dialogs; explicit window handles are borrowed from their caller.
/// </summary>
public interface IFolderBrowserDialog : IDisposableReactiveObject
{
    DirectoryInfo ShowDialog();

    /// <summary>
    /// Selects a folder for the live originating WPF window on its dispatcher; unsupported implementations fail explicitly.
    /// The dialog borrows the HWND without releasing it or falling back to a foreground/global owner.
    /// </summary>
    DirectoryInfo ShowDialog(IntPtr hwndOwner) => throw new NotSupportedException(
        "Explicit folder-dialog owners are not supported by this implementation");
   
    string SelectedPath { get; set; }
    
    string Title { get; set; }
   
    string InitialDirectory { get; set; }
}