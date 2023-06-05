using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using PoeShared.Squirrel.Native;
using PoeShared.Squirrel.Scaffolding;

namespace PoeShared.Squirrel.Core;

internal static class SquirrelAwareExecutableDetector
{
    public static List<string> GetAllSquirrelAwareApps(string directory, int minimumVersion = 1)
    {
        var executables = new DirectoryInfo(directory).EnumerateFiles("*.exe").ToArray();
        var result = (from x in executables
            select x.FullName into x
            where (GetPeSquirrelAwareVersion(x) ?? -1) >= minimumVersion
            select x).ToList();
        if (result.Any())
        {
            return result;
        }
        return executables.Length == 1 ? executables.Select(x => x.FullName).ToList() : new List<string>();
    }

    public static int? GetPeSquirrelAwareVersion(string executable)
    {
        if (!File.Exists(executable))
        {
            return null;
        }

        var fullname = Path.GetFullPath(executable);

        return Utility.Retry(
            () =>
                GetAssemblySquirrelAwareVersion(fullname) ?? GetVersionBlockSquirrelAwareValue(fullname));
    }

    private static int? GetAssemblySquirrelAwareVersion(string executable)
    {
        try
        {
            var assembly = AssemblyDefinition.ReadAssembly(executable);
            if (!assembly.HasCustomAttributes)
            {
                return null;
            }

            var attrs = assembly.CustomAttributes;
            var attribute = attrs.FirstOrDefault(
                x =>
                {
                    if (x.AttributeType.FullName != typeof(AssemblyMetadataAttribute).FullName)
                    {
                        return false;
                    }

                    if (x.ConstructorArguments.Count != 2)
                    {
                        return false;
                    }

                    return x.ConstructorArguments[0].Value.ToString() == "SquirrelAwareVersion";
                });

            if (attribute == null)
            {
                return null;
            }

            if (!int.TryParse(attribute.ConstructorArguments[1].Value.ToString(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var result))
            {
                return null;
            }

            return result;
        }
        catch (FileLoadException)
        {
            return null;
        }
        catch (BadImageFormatException)
        {
            var dllFile = new FileInfo(Path.ChangeExtension(executable, "dll"));
            if (dllFile.Exists)
            {
                return GetPeSquirrelAwareVersion(dllFile.FullName);
            }
            return null;
        }
    }

    private static int? GetVersionBlockSquirrelAwareValue(string executable)
    {
        var size = NativeMethods.GetFileVersionInfoSize(executable, IntPtr.Zero);

        // Nice try, buffer overflow
        if (size <= 0 || size > 4096)
        {
            return null;
        }

        var buf = new byte[size];
        if (!NativeMethods.GetFileVersionInfo(executable, 0, size, buf))
        {
            return null;
        }

        if (!NativeMethods.VerQueryValue(buf, "\\StringFileInfo\\040904B0\\SquirrelAwareVersion", out var result, out var resultSize))
        {
            return null;
        }

        // NB: I have **no** idea why, but Atom.exe won't return the version
        // number "1" despite it being in the resource file and being 100% 
        // identical to the version block that actually works. I've got stuff
        // to ship, so we're just going to return '1' if we find the name in 
        // the block at all. I hate myself for this.
        return 1;

#if __NOT__DEFINED_EVAR__
            int ret;
            string resultData = Marshal.PtrToStringAnsi(result, resultSize-1 /* Subtract one for null terminator */);
            if (!Int32.TryParse(resultData, NumberStyles.Integer, CultureInfo.CurrentCulture, out ret)) return null;

            return ret;
#endif
    }
}