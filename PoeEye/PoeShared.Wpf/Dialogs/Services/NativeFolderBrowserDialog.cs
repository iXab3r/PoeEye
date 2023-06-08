using System;
using System.IO;
using System.Windows.Forms;
using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.Services;

internal sealed class NativeFolderBrowserDialog : DisposableReactiveObjectWithLogger, IFolderBrowserDialog
{
    public DirectoryInfo ShowDialog()
    {
        Log.Info(() => $"Showing Open folder dialog, parameters: {new { Title, InitialDirectory, LastDirectory }}");
        var dialog = new FolderBrowserDialog()
        {
            Description = Title,
            InitialDirectory = !string.IsNullOrEmpty(InitialDirectory) && Directory.Exists(InitialDirectory) 
                ? InitialDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            Log.Info("User cancelled Open file dialog");
            return default;
        }

        LastDirectory = new DirectoryInfo(dialog.SelectedPath);
        Log.Info(() => $"User has selected folder {LastDirectory} (exists: {LastDirectory.Exists})");
        return LastDirectory;
    }

    public DirectoryInfo LastDirectory { get; private set; }
    
    public string Title { get; set; }
    
    public string InitialDirectory { get; set; }
}