using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Immutable;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.Services;

public sealed class ComDlg32FileDialog : DisposableReactiveObjectWithLogger, ISaveFileDialog, IOpenFileDialog
{
    private const int MAX_PATH = 256;

    FileInfo IOpenFileDialog.ShowDialog()
    {
        var ofn = new OpenFileName
        {
            file = new string(new char[MAX_PATH]),
            filter = PrepareFilter(Filter),
            filterIndex = 2,
            fileTitle = new string(new char[64]),
            dlgOwner = UnsafeNative.ResolveParentForDialogWindow()
        };
        ofn.maxFile = ofn.file.Length;
        ofn.maxFileTitle = ofn.fileTitle.Length;
        ofn.initialDir = InitialDirectory;
        ofn.title = Title;
        ofn.flags = OpenFileNameFlags.OFN_EXPLORER | OpenFileNameFlags.OFN_FILEMUSTEXIST | OpenFileNameFlags.OFN_PATHMUSTEXIST | OpenFileNameFlags.OFN_NOCHANGEDIR;
        if (!GetOpenFileName(ofn))
        {
            return null;
        }

        return new FileInfo(ofn.file);
    }

    ImmutableArray<FileInfo> IOpenFileDialog.ShowDialogMultiselect()
    {
        // Minimal implementation: fallback to single selection for legacy ComDlg32-based dialog
        var file = ((IOpenFileDialog)this).ShowDialog();
        return file != null ? ImmutableArray.Create(file) : ImmutableArray<FileInfo>.Empty;
    }

    public FileInfo ShowDialog(IntPtr hwndOwner)
    {
        throw new NotSupportedException("Using Owner is not supported");
    }

    public string Title { get; set; }

    public string InitialDirectory { get; set; }

    public string InitialFileName { get; set; }

    public string Filter { get; set; }

    public FileInfo LastFile { get; }

    FileInfo ISaveFileDialog.ShowDialog()
    {
        var ofn = new OpenFileName
        {
            filter = PrepareFilter(Filter),
            filterIndex = 2,
            file = new string(new char[MAX_PATH]),
            fileTitle = new string(new char[64]),
            dlgOwner = UnsafeNative.ResolveParentForDialogWindow()
        };

        ofn.maxFileTitle = ofn.fileTitle.Length;
        ofn.initialDir = InitialDirectory;
        ofn.title = Title;
        ofn.flags = OpenFileNameFlags.OFN_OVERWRITEPROMPT | OpenFileNameFlags.OFN_HIDEREADONLY;
        ofn.maxFile = ofn.file.Length;

        if (!GetSaveFileName(ofn))
        {
            return null;
        }

        return new FileInfo(ofn.file);
    }

    [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    public static extern bool GetOpenFileName([In] [Out] OpenFileName ofn);

    [DllImport("Comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetSaveFileName([In] [Out] OpenFileName ofn);

    private static string PrepareFilter(string wpfFilter)
    {
        var filterList = new List<string>();
        filterList.Add("All Files");
        filterList.Add("*.*");
        if (!string.IsNullOrEmpty(wpfFilter))
        {
            filterList.AddRange(wpfFilter.SplitTrim('|'));
        }

        var filters = PrepareFilter(filterList);
        return filters;
    }
    
    private static string PrepareFilter(IEnumerable<string> filters)
    {
        return string.Join("\0", filters) + "\0";
    }

    /// <summary>
    /// The structure contains information that the GetOpenFileName and GetSaveFileName functions use to initialize an Open or Save As dialog box.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class OpenFileName
    {
        public int structSize = Marshal.SizeOf<OpenFileName>();
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public string filter;
        public string customFilter = null;
        public int maxCustFilter;
        public int filterIndex;
        public string file;
        public int maxFile;
        public string fileTitle;
        public int maxFileTitle;
        public string initialDir;
        public string title;
        public OpenFileNameFlags flags;
        public short fileOffset;
        public short fileExtension;
        public string defExt;
        public int custData;
        public IntPtr hook;
        public string templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public OpenFileNameFlagsEx flagsEx = 0;
    }

    [Flags]
    public enum OpenFileNameFlags : int
    {
        /// <summary>
        /// Enables the Read Only check box in the dialog box. 
        /// </summary>
        OFN_READONLY = 0x00000001,

        /// <summary>
        /// The Save As dialog box prompts the user for confirmation before overwriting a file.
        /// </summary>
        OFN_OVERWRITEPROMPT = 0x00000002,

        /// <summary>
        /// The dialog box hides the Read Only check box.
        /// </summary>
        OFN_HIDEREADONLY = 0x00000004,

        /// <summary>
        /// Restores the current directory to the original value if the user changed the directory while searching for files.
        /// </summary>
        OFN_NOCHANGEDIR = 0x00000008,

        /// <summary>
        /// Shows the Help button in the dialog box.
        /// </summary>
        OFN_SHOWHELP = 0x00000010,

        /// <summary>
        /// Enables the hook procedure specified in the OPENFILENAME structure.
        /// </summary>
        OFN_ENABLEHOOK = 0x00000020,

        /// <summary>
        /// Enables the template specified in the OPENFILENAME structure.
        /// </summary>
        OFN_ENABLETEMPLATE = 0x00000040,

        /// <summary>
        /// Indicates that the hInstance member specifies a data block that contains a preloaded dialog box template. 
        /// The system ignores the lpTemplateName if this flag is specified.
        /// </summary>
        OFN_ENABLETEMPLATEHANDLE = 0x00000080,

        /// <summary>
        /// The common dialog boxes will return the path and file name specified in the lpstrFile member.
        /// </summary>
        OFN_NOVALIDATE = 0x00000100,

        /// <summary>
        /// Allows multiple file selections.
        /// </summary>
        OFN_ALLOWMULTISELECT = 0x00000200,

        /// <summary>
        /// The File Name entry field specifies a file name extension that differs from the extension specified by the Default Extension.
        /// </summary>
        OFN_EXTENSIONDIFFERENT = 0x00000400,

        /// <summary>
        /// The user can only type valid paths and file names into the File Name entry field.
        /// </summary>
        OFN_PATHMUSTEXIST = 0x00000800,

        /// <summary>
        /// The user can type only names of existing files in the File Name entry field.
        /// </summary>
        OFN_FILEMUSTEXIST = 0x00001000,

        /// <summary>
        /// If the user specifies a file that does not exist, this flag causes the dialog box to prompt the user for permission to create the file. 
        /// If the user chooses to create the file, the dialog box closes.
        /// </summary>
        OFN_CREATEPROMPT = 0x00002000,

        /// <summary>
        /// Specifies that if a call to the OpenFile function fails because of a network sharing violation, the error is ignored and the dialog box returns the selected file name.
        /// </summary>
        OFN_SHAREAWARE = 0x00004000,

        /// <summary>
        /// Specifies that only non-readonly files can be selected. The chosen file name is not checked to see if it is an existing read-only file.
        /// </summary>
        OFN_NOREADONLYRETURN = 0x00008000,

        /// <summary>
        /// Specifies that the file is not created before the dialog box is closed. This flag is useful for saving a file or document that has not yet been created.
        /// </summary>
        OFN_NOTESTFILECREATE = 0x00010000,

        /// <summary>
        /// Hides and disables the Network button.
        /// </summary>
        OFN_NONETWORKBUTTON = 0x00020000,

        /// <summary>
        /// Indicates that the returned file name must not include the path. In other words, the path and file name must be concatenated.
        /// </summary>
        OFN_NOLONGNAMES = 0x00040000,

        /// <summary>
        /// Specifies that the dialog box should use the Explorer-style dialog box.
        /// </summary>
        OFN_EXPLORER = 0x00080000,

        /// <summary>
        /// Directs the dialog box to return the path and file name of the selected shortcut (.LNK) file.
        /// </summary>
        OFN_NODEREFERENCELINKS = 0x00100000,

        /// <summary>
        /// Specifies that long file names should be returned.
        /// </summary>
        OFN_LONGNAMES = 0x00200000,

        /// <summary>
        /// Enables the inclusion of items that are not in the file system.
        /// </summary>
        OFN_ENABLEINCLUDENOTIFY = 0x00400000,

        /// <summary>
        /// Enables resizing of the File Name listbox and dialog box in an Open or Save As dialog box.
        /// </summary>
        OFN_ENABLESIZING = 0x00800000,

        /// <summary>
        /// Prevents adding to the list of recently used files on the File menu.
        /// </summary>
        OFN_DONTADDTORECENT = 0x02000000,

        /// <summary>
        /// Forces the dialog box to show all files, regardless of their system or hidden attribute settings.
        /// </summary>
        OFN_FORCESHOWHIDDEN = 0x10000000,

        /// <summary>
        /// Hide the places bar in open/save dialog box.
        /// </summary>
        OFN_EX_NOPLACESBAR = 0x00000001
    }

    /// <summary>
    /// Constants for OpenFileNameEx flags.
    /// </summary>
    [Flags]
    public enum OpenFileNameFlagsEx : int
    {
        /// <summary>
        /// Hide the places bar in open/save dialog box.
        /// </summary>
        OFN_EX_NOPLACESBAR = 0x00000001,
    }
}