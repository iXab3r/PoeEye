using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using PInvoke;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.Services;

internal sealed class ShBrowseForFolderDialog : DisposableReactiveObjectWithLogger, IFolderBrowserDialog
{
    private const int MAX_PATH = 256;

    public DirectoryInfo ShowDialog()
    {
        Log.Info($"Showing Open folder dialog, parameters: {new {Title, InitialDirectory, LastDirectory = SelectedPath}}");

        var parentWindow = User32.GetForegroundWindow();
        var bi = new BROWSEINFO
        {
            lpszTitle = Title,
            pszDisplayName = !string.IsNullOrEmpty(InitialDirectory) && Directory.Exists(InitialDirectory)
                ? InitialDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            hwndOwner = UnsafeNative.ResolveParentForDialogWindow(),
            ulFlags = BrowseInfoFlag.BIF_STATUSTEXT | BrowseInfoFlag.BIF_EDITBOX | BrowseInfoFlag.BIF_NEWDIALOGSTYLE
        };
        var pidl = SHBrowseForFolder(ref bi);
        if (pidl == IntPtr.Zero)
        {
            Log.Info("User cancelled Open folder dialog");
            return default;
        }

        var path = new StringBuilder(MAX_PATH);
        if (!SHGetPathFromIDList(pidl, path))
        {
            return string.IsNullOrEmpty(SelectedPath) ? null : new DirectoryInfo(SelectedPath);
        }

        SelectedPath = path.ToString();
        Log.Info($"User has selected folder {SelectedPath}");
        InitialDirectory = Path.GetDirectoryName(SelectedPath);

        return string.IsNullOrEmpty(SelectedPath) ? null : new DirectoryInfo(SelectedPath);
    }

    public string SelectedPath { get; set; }

    public string Title { get; set; }

    public string InitialDirectory { get; set; }

    [DllImport("shell32.dll")]
    public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern bool SHGetPathFromIDList(IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPath);
    
    public delegate int BrowseCallbackProc(IntPtr hwnd, BrowseForFolderMessages uMsg, IntPtr lParam, IntPtr lpData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct BROWSEINFO
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public BrowseInfoFlag ulFlags;
        public BrowseCallbackProc lpfn;
        public IntPtr lParam;
        public int iImage;
        [MarshalAs(UnmanagedType.LPTStr)] public string pszDisplayName;
        [MarshalAs(UnmanagedType.LPTStr)] public string lpszTitle;
    }
    
    [Flags]
    public enum BrowseInfoFlag : uint
    {
        BIF_RETURNONLYFSDIRS = 0x0001,  // The browse dialog box will display only directories.
        BIF_DONTGOBELOWDOMAIN = 0x0002, // Do not include network folders below the domain level in the dialog box's tree view control.
        BIF_STATUSTEXT = 0x0004,        // The browse dialog box will display a status area.
        BIF_RETURNFSANCESTORS = 0x0008, // Only return file system ancestors.
        BIF_EDITBOX = 0x0010,           // Include an edit control in the browse dialog box that allows the user to type the name of an item.
        BIF_VALIDATE = 0x0020,          // If the user types an invalid name into the edit box, the browse dialog box will call the application's BrowseCallbackProc with the BFFM_VALIDATEFAILED message.
        BIF_NEWDIALOGSTYLE = 0x0040,    // Use the new user interface. Setting this flag provides the user with a larger dialog box that can be resized.
        BIF_BROWSEINCLUDEURLS = 0x0080, // The browse dialog box can display URLs.
        BIF_USENEWUI = 0x0050,          // Use the new user interface, including an edit box. This flag is equivalent to BIF_EDITBOX | BIF_NEWDIALOGSTYLE.
        BIF_UAHINT = 0x0100,            // Do not include the New Folder button in the browse dialog box.
        BIF_NONEWFOLDERBUTTON = 0x0200, // Hide the New Folder button.
        BIF_NOTRANSLATETARGETS = 0x0400,// The browse dialog box will return the target of a Shell link object.
        BIF_BROWSEFORCOMPUTER = 0x1000, // The browse dialog box will display computers. If the user selects a computer, the OK button is grayed.
        BIF_BROWSEFORPRINTER = 0x2000,  // The browse dialog box will display printers.
        BIF_BROWSEINCLUDEFILES = 0x4000,// The browse dialog box will display files as well as folders.
        BIF_SHAREABLE = 0x8000          // The browse dialog box will display sharable resources on remote systems.
    }
    
    public enum BrowseForFolderMessages
    {
        BFFM_INITIALIZED = 1,
        BFFM_SELCHANGED = 2,
        BFFM_VALIDATEFAILEDA = 3,
        BFFM_VALIDATEFAILEDW = 4,
        BFFM_IUNKNOWN = 5
    }
}