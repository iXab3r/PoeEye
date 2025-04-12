using PoeShared.Scaffolding;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace PoeShared.Dialogs.Services;

using System;
using System.IO;

public sealed class WpfFileDialog : DisposableReactiveObjectWithLogger, ISaveFileDialog, IOpenFileDialog
{
    public WpfFileDialog()
    {
    }
    
    public string Title { get; set; }
    
    public string InitialDirectory { get; set; }
    
    public string InitialFileName { get; set; }
    
    public string Filter { get; set; }

    public FileInfo ShowDialog(IntPtr hwndOwner)
    {
        throw new NotSupportedException("Using Owner is not supported");
    }

    public FileInfo LastFile { get; private set; }
    
    FileInfo IOpenFileDialog.ShowDialog()
    {
        Log.Info($"Showing Open file dialog, parameters: {new { Title, InitialDirectory, Filter, FileName = InitialFileName, LastSavedFile = LastFile }}");
        var dialog = new OpenFileDialog()
        {
            Title = Title, 
            FileName = InitialFileName,
            InitialDirectory = !string.IsNullOrEmpty(InitialDirectory) && Directory.Exists(InitialDirectory) 
                ? InitialDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures),
            Filter = Filter,
        };
        
        if (dialog.ShowDialog() != true)
        {
            Log.Info("User cancelled Open file dialog");
            return default;
        }

        var result = new FileInfo(dialog.FileName);
        LastFile = result;
        Log.Info($"User has selected file {result} (exists: {result.Exists})");
        return result;
    }

    FileInfo ISaveFileDialog.ShowDialog()
    {
        Log.Info($"Showing Save file dialog, parameters: {new { Title, InitialDirectory, Filter, FileName = InitialFileName, LastSavedFile = LastFile }}");
        var dialog = new SaveFileDialog
        {
            Title = Title, 
            FileName = InitialFileName,
            InitialDirectory = !string.IsNullOrEmpty(InitialDirectory) && Directory.Exists(InitialDirectory) 
                ? InitialDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures),
            Filter = Filter
        };
        
        if (dialog.ShowDialog() != true)
        {
            Log.Info("User cancelled Save file dialog");
            return default;
        }

        var result = new FileInfo(dialog.FileName);
        LastFile = result;
        Log.Info($"User has selected file {result} (exists: {result.Exists})");
        return result;
    }
}