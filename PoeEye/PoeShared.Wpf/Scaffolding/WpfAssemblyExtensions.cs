using System;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace PoeShared.Scaffolding;

public static class WpfAssemblyExtensions
{
    /// <summary>
    /// Load a resource WPF-BitmapImage (png, bmp, ...) from embedded resource defined as 'Resource' not as 'Embedded resource'.
    /// </summary>
    /// <param name="pathInApplication">Path without starting slash</param>
    /// <param name="assembly">Usually 'Assembly.GetExecutingAssembly()'. If not mentionned, I will use the calling assembly</param>
    /// <returns></returns>
    public static BitmapImage LoadBitmapFromResource(this Assembly assembly, string pathInApplication)
    {
        return new(new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + pathInApplication, UriKind.Absolute)); 
    }
}