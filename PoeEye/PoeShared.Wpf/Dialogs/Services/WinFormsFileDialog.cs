using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Immutable;
using System.Linq;
using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.Services;

public sealed class WinFormsFileDialog : DisposableReactiveObjectWithLogger, ISaveFileDialog, IOpenFileDialog
{
    public string Title { get; set; }

    public string InitialDirectory { get; set; }

    public string InitialFileName { get; set; }

    public string Filter { get; set; }

    public FileInfo LastFile { get; private set; }

    FileInfo IOpenFileDialog.ShowDialog() => ShowOpenDialog();

    ImmutableArray<FileInfo> IOpenFileDialog.ShowDialogMultiselect() => ShowOpenDialogMultiselect();

    FileInfo ISaveFileDialog.ShowDialog() => ShowSaveDialog();

    public FileInfo ShowDialog(IntPtr hwndOwner) => ShowOpenDialog(hwndOwner);

    private FileInfo ShowOpenDialog(IntPtr hwndOwner = default)
    {
        return EnsureSta(() =>
        {
            Log.Info($"Showing OpenFileDialog (WinForms), parameters: {new { Title, InitialDirectory, Filter, InitialFileName, LastFile }}");

            using var dialog = new OpenFileDialog
            {
                Title = Title,
                FileName = InitialFileName,
                InitialDirectory = IsValidDirectory(InitialDirectory) ? InitialDirectory : GetDefaultFolder(),
                Filter = Filter
            };

            var owner = hwndOwner != IntPtr.Zero ? new Win32Window(hwndOwner) : null;

            if (dialog.ShowDialog(owner) != DialogResult.OK)
            {
                Log.Info("User cancelled OpenFileDialog");
                return null;
            }

            var result = new FileInfo(dialog.FileName);
            LastFile = result;
            Log.Info($"User selected file: {result} (exists: {result.Exists})");
            return result;
        });
    }

    private ImmutableArray<FileInfo> ShowOpenDialogMultiselect(IntPtr hwndOwner = default)
    {
        return EnsureSta(() =>
        {
            Log.Info($"Showing OpenFileDialog (WinForms, multiselect), parameters: {new { Title, InitialDirectory, Filter, InitialFileName, LastFile }}");

            using var dialog = new OpenFileDialog
            {
                Title = Title,
                FileName = InitialFileName,
                InitialDirectory = IsValidDirectory(InitialDirectory) ? InitialDirectory : GetDefaultFolder(),
                Filter = Filter,
                Multiselect = true
            };

            var owner = hwndOwner != IntPtr.Zero ? new Win32Window(hwndOwner) : null;

            if (dialog.ShowDialog(owner) != DialogResult.OK)
            {
                Log.Info("User cancelled OpenFileDialog (multiselect)");
                return ImmutableArray<FileInfo>.Empty;
            }

            var files = dialog.FileNames?.Select(x => new FileInfo(x)).ToImmutableArray() ?? ImmutableArray<FileInfo>.Empty;
            if (files.Length > 0)
            {
                LastFile = files[^1];
            }
            Log.Info($"User selected {files.Length} file(s)");
            return files;
        });
    }

    private FileInfo ShowSaveDialog(IntPtr? hwndOwner = null)
    {
        return EnsureSta(() =>
        {
            Log.Info($"Showing SaveFileDialog (WinForms), parameters: {new { Title, InitialDirectory, Filter, InitialFileName, LastFile }}");

            using var dialog = new SaveFileDialog
            {
                Title = Title,
                FileName = InitialFileName,
                InitialDirectory = IsValidDirectory(InitialDirectory) ? InitialDirectory : GetDefaultFolder(),
                Filter = Filter
            };

            var owner = hwndOwner.HasValue ? new Win32Window(hwndOwner.Value) : null;

            if (dialog.ShowDialog(owner) != DialogResult.OK)
            {
                Log.Info("User cancelled SaveFileDialog");
                return null;
            }

            var result = new FileInfo(dialog.FileName);
            LastFile = result;
            Log.Info($"User selected file: {result} (exists: {result.Exists})");
            return result;
        });
    }

    /// <summary>
    /// Ensures the given function is executed on a STA thread, creating a temporary one if necessary.
    /// </summary>
    private static T EnsureSta<T>(Func<T> func)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            return func();
        }

        T result = default;
        Exception error = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = func();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        })
        {
            IsBackground = true
        };
        thread.Name = nameof(WinFormsFileDialog);
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error != null)
        {
            throw new InvalidOperationException("Exception while executing on STA thread", error);
        }

        return result;
    }

    private static bool IsValidDirectory(string dir) => !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir);

    private static string GetDefaultFolder() =>
        Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures);

    /// <summary>
    /// Wraps a raw HWND as an IWin32Window for use with WinForms dialogs.
    /// </summary>
    private sealed class Win32Window : IWin32Window
    {
        public Win32Window(IntPtr handle) => Handle = handle;

        public IntPtr Handle { get; }
    }
}
