using System;
using System.IO;

namespace PoeShared.Dialogs.Services;

public interface ISaveFileDialog : IFileDialog
{
   FileInfo ShowDialog();
   
   FileInfo ShowDialog(IntPtr hwndOwner);
   
   FileInfo LastFile { get; }
}