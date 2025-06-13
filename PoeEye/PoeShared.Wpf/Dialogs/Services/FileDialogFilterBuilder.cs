using System;
using System.Collections.Generic;
using System.Linq;

namespace PoeShared.Dialogs.Services;
public class FileDialogFilterBuilder
{
    private readonly List<(string name, string pattern)> filters = new();

    /// <summary>
    /// Add a filter with a raw pattern (e.g., "*.cs;*.csx")
    /// </summary>
    public FileDialogFilterBuilder Add(string name, string pattern)
    {
        filters.Add((name, pattern));
        return this;
    }

    /// <summary>
    /// Add a filter by specifying multiple file extensions (e.g., "cs", "csx")
    /// </summary>
    public FileDialogFilterBuilder Add(string name, params string[] extensions)
    {
        if (extensions == null || extensions.Length == 0)
            throw new ArgumentException("At least one extension must be provided.", nameof(extensions));

        var pattern = string.Join(";", extensions.Select(ext => ext.StartsWith("*.") ? ext : $"*.{ext.TrimStart('.')}"));
        filters.Add((name, pattern));
        return this;
    }

    public string Build() => ToString();

    public override string ToString()
    {
        return string.Join("|", filters.Select(f => $"{f.name}|{f.pattern}"));
    }
}
