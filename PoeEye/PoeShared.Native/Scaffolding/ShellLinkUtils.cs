using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace PoeShared.Scaffolding;

public static class ShellLinkUtils
{
    /// <summary>
    /// Creates Shell link (.lnk)
    /// </summary>
    /// <param name="filePath">Must have .lnk extension</param>
    /// <param name="linkUrl"></param>
    /// <param name="description"></param>
    /// <exception cref="ArgumentException"></exception>
    public static void CreateShellLink(
        string filePath, 
        Uri linkUrl,
        string description = null)
    {
        var extension = Path.GetExtension(filePath);
        if (!string.Equals(extension, ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Shell links must have extension '.lnk', but was {extension}");
        }
        
        // ReSharper disable once SuspiciousTypeConversion.Global COM
        var link = (IShellLink)new ShellLink();

        if (!string.IsNullOrEmpty(description))
        {
            link.SetDescription(description);
        }
        link.SetPath(linkUrl.ToString());
        
        // ReSharper disable once SuspiciousTypeConversion.Global COM
        var lnkFile = (IPersistFile)link;
        lnkFile.Save(filePath, fRemember: false);
    }
    
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}