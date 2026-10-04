using System;
using System.IO;
using System.Windows.Forms;
using HwndSource = System.Windows.Interop.HwndSource;
using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.Services;

internal sealed class Win32FolderBrowserDialog : DisposableReactiveObjectWithLogger, IFolderBrowserDialog
{
    public DirectoryInfo ShowDialog() => ShowDialogCore(null);

    public DirectoryInfo ShowDialog(IntPtr hwndOwner)
    {
        var owner = HwndSource.FromHwnd(hwndOwner)?.RootVisual as System.Windows.Window
            ?? throw new ArgumentException("A live WPF owner window is required", nameof(hwndOwner));
        owner.Dispatcher.VerifyAccess();
        return ShowDialogCore(new Win32Window(hwndOwner));
    }

    private DirectoryInfo ShowDialogCore(IWin32Window owner)
    {
        Log.Info($"Showing Open folder dialog, parameters: {new { Title, InitialDirectory, LastDirectory = SelectedPath }}");
        var dialog = new FolderBrowserDialog()
        {
            Description = Title,
            InitialDirectory = !string.IsNullOrEmpty(InitialDirectory) && Directory.Exists(InitialDirectory) 
                ? InitialDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            SelectedPath = SelectedPath ?? string.Empty
        };
        
        if ((owner == null ? dialog.ShowDialog() : dialog.ShowDialog(owner)) != DialogResult.OK)
        {
            Log.Info("User cancelled Open file dialog");
            return default;
        }

        SelectedPath = dialog.SelectedPath;
        if (SelectedPath != null)
        {
            Log.Info($"User has selected folder {SelectedPath}");
            InitialDirectory = Path.GetDirectoryName(SelectedPath);
        }

        return string.IsNullOrEmpty(SelectedPath) ? null : new DirectoryInfo(SelectedPath);
    }

    // This adapter only borrows the HWND; it does not attach a window procedure or own its lifetime.
    private sealed class Win32Window : IWin32Window
    {
        public Win32Window(IntPtr handle) => Handle = handle;
        public IntPtr Handle { get; }
    }

    public string SelectedPath { get; set; }
    
    public string Title { get; set; }
    
    public string InitialDirectory { get; set; }
}