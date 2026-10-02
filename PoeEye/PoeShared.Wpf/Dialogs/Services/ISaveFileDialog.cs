using System;
using System.IO;

namespace PoeShared.Dialogs.Services;

public interface ISaveFileDialog : IFileDialog
{
   FileInfo ShowDialog();
   
   /// <summary>Shows Save owned by a live host HWND on its UI dispatcher; cancellation returns no file.</summary>
   FileInfo ShowDialog(IntPtr hwndOwner);
   
   FileInfo LastFile { get; }
}