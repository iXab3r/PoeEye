using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using PoeShared.Squirrel.Native;

namespace PoeShared.Squirrel.Scaffolding;

internal static unsafe class UnsafeUtility
{
    public static List<Tuple<string, int>> EnumerateProcesses()
    {
        var bytesReturned = 0;
        var pids = new int[2048];

        fixed (int* p = pids)
        {
            if (!NativeMethods.EnumProcesses((IntPtr) p, sizeof(int) * pids.Length, out bytesReturned))
            {
                throw new Win32Exception("Failed to enumerate processes");
            }

            if (bytesReturned < 1)
            {
                throw new Exception("Failed to enumerate processes");
            }
        }

        return Enumerable.Range(0, bytesReturned / sizeof(int))
            .Where(i => pids[i] > 0)
            .Select(
                i =>
                {
                    try
                    {
                        var hProcess = NativeMethods.OpenProcess(ProcessAccess.QueryLimitedInformation, false, pids[i]);
                        if (hProcess == IntPtr.Zero)
                        {
                            throw new Win32Exception();
                        }

                        var sb = new StringBuilder(256);
                        var capacity = sb.Capacity;
                        if (!NativeMethods.QueryFullProcessImageName(hProcess, 0, sb, ref capacity))
                        {
                            throw new Win32Exception();
                        }

                        NativeMethods.CloseHandle(hProcess);
                        return Tuple.Create(sb.ToString(), pids[i]);
                    }
                    catch (Exception)
                    {
                        return Tuple.Create(default(string), pids[i]);
                    }
                })
            .ToList();
    }
}