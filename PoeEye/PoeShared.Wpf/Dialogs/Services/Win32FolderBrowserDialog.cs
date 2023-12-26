using System;
using System.IO;
using System.Windows.Forms;
using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.Services;

internal sealed class Win32FolderBrowserDialog : DisposableReactiveObjectWithLogger, IFolderBrowserDialog
{
    public DirectoryInfo ShowDialog()
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
        
        if (dialog.ShowDialog() != DialogResult.OK )
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

    public string SelectedPath { get; set; }
    
    public string Title { get; set; }
    
    public string InitialDirectory { get; set; }
}