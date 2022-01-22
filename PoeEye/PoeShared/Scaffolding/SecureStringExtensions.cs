using System;
using System.Runtime.InteropServices;
using System.Security;

namespace PoeShared.Scaffolding;

/// <summary>
///     Provides unsafe temporary operations on secured strings.
/// </summary>
[SuppressUnmanagedCodeSecurity]
public static class SecureStringExtensions
{
    /// <summary>
    ///     Converts a secured string to an unsecured string.
    /// </summary>
    public static string ToUnsecuredString(this SecureString secureString)
    {
        // copy&paste from the internal System.Net.UnsafeNclNativeMethods
        var bstrPtr = IntPtr.Zero;
        if (secureString != null)
        {
            if (secureString.Length != 0)
            {
                try
                {
                    bstrPtr = Marshal.SecureStringToBSTR(secureString);
                    return Marshal.PtrToStringBSTR(bstrPtr);
                }
                finally
                {
                    if (bstrPtr != IntPtr.Zero)
                    {
                        Marshal.ZeroFreeBSTR(bstrPtr);
                    }
                }
            }
        }

        return string.Empty;
    }

    /// <summary>
    ///     Copies the existing instance of a secure string into the destination, clearing the destination beforehand.
    /// </summary>
    public static void CopyInto(this SecureString source, SecureString destination)
    {
        destination.Clear();
        foreach (var chr in source.ToUnsecuredString())
        {
            destination.AppendChar(chr);
        }
    }

    /// <summary>
    ///     Converts an unsecured string to a secured string.
    /// </summary>
    public static SecureString ToSecuredString(this string plainString)
    {
        var result = new SecureString();
        if (string.IsNullOrEmpty(plainString))
        {
            return result;
        }

        foreach (var c in plainString)
        {
            result.AppendChar(c);
        }

        return result;
    }
}