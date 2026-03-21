using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace PoeShared.Blazor.UnscopedCss.Tasks;

public sealed class UnscopeGeneratedScopedCssTask : Microsoft.Build.Utilities.Task
{
    private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };
    private static readonly Regex DefaultBlazorScopeRegex = new Regex(@"\[b-[A-Za-z0-9]+\]", RegexOptions.CultureInvariant);

    [Required]
    public ITaskItem[] Files { get; set; } = Array.Empty<ITaskItem>();

    [Required]
    public ITaskItem[] FlaggedOriginalFiles { get; set; } = Array.Empty<ITaskItem>();

    public bool Verbose { get; set; }

    public string? PhaseName { get; set; }

    public override bool Execute()
    {
        var stopwatch = Stopwatch.StartNew();
        var flaggedFiles = BuildFlaggedFileLookup();
        if (flaggedFiles.Count == 0)
        {
            return true;
        }

        var processedFiles = 0;
        var rewrittenFiles = 0;

        foreach (var file in Files)
        {
            var originalFile = GetItemFullPath(file.GetMetadata("OriginalItemSpec"));
            if (originalFile is null || !flaggedFiles.TryGetValue(originalFile, out var flaggedItem))
            {
                continue;
            }

            processedFiles++;
            if (TryRewriteFile(file, flaggedItem))
            {
                rewrittenFiles++;
            }
        }

        stopwatch.Stop();

        if (Verbose && processedFiles > 0)
        {
            Log.LogMessage(
                MessageImportance.High,
                FormatMessage("Processed {0} flagged CSS output file(s), rewrote {1} in {2} ms."),
                processedFiles,
                rewrittenFiles,
                stopwatch.ElapsedMilliseconds);
        }

        return !Log.HasLoggedErrors;
    }

    private Dictionary<string, ITaskItem> BuildFlaggedFileLookup()
    {
        var result = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in FlaggedOriginalFiles)
        {
            if (!ShouldUnscope(GetUnscopeCssValue(item)))
            {
                continue;
            }

            var fullPath = GetItemFullPath(item.GetMetadata("OriginalItemSpec") is { Length: > 0 } original ? original : item.ItemSpec);
            if (fullPath is null)
            {
                continue;
            }

            result[fullPath] = item;
        }

        return result;
    }

    private bool TryRewriteFile(ITaskItem item, ITaskItem flaggedItem)
    {
        var generatedFile = item.GetMetadata("GeneratedOutputFile");
        var originalFile = item.GetMetadata("OriginalItemSpec");

        if (string.IsNullOrWhiteSpace(generatedFile))
        {
            Log.LogError("[PoeShared.Blazor.UnscopedCss] Missing GeneratedOutputFile metadata for '{0}'.", item.ItemSpec);
            return false;
        }

        if (!File.Exists(generatedFile))
        {
            Log.LogWarning("[PoeShared.Blazor.UnscopedCss] Generated scoped CSS file '{0}' for '{1}' was not found.", generatedFile, originalFile);
            return false;
        }

        var content = ReadFilePreservingBom(generatedFile, out var hadBom);
        var scopeSelectors = DetermineScopeSelectors(item, flaggedItem, content);
        if (scopeSelectors.Count == 0)
        {
            if (Verbose)
            {
                Log.LogMessage(
                    MessageImportance.High,
                    FormatMessage("Skipping '{0}' because no Blazor scope selector could be identified."),
                    originalFile);
            }

            return false;
        }

        var updatedContent = content;
        foreach (var selector in scopeSelectors)
        {
            updatedContent = updatedContent.Replace(selector, string.Empty);
        }

        if (ReferenceEquals(updatedContent, content) || updatedContent == content)
        {
            if (Verbose)
            {
                Log.LogMessage(
                    MessageImportance.High,
                    FormatMessage("'{0}' did not contain removable scope selectors after inspection."),
                    originalFile);
            }

            return false;
        }

        WriteFilePreservingBom(generatedFile, updatedContent, hadBom);

        if (Verbose)
        {
            Log.LogMessage(
                MessageImportance.High,
                FormatMessage("Unscoped '{0}' via generated file '{1}'. Removed selectors: {2}"),
                originalFile,
                generatedFile,
                string.Join(", ", scopeSelectors));
        }

        return true;
    }

    private HashSet<string> DetermineScopeSelectors(ITaskItem item, ITaskItem flaggedItem, string content)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var configuredCssScope = flaggedItem.GetMetadata("ConfiguredCssScope");
        if (string.IsNullOrWhiteSpace(configuredCssScope))
        {
            configuredCssScope = item.GetMetadata("ConfiguredCssScope");
        }

        if (!string.IsNullOrWhiteSpace(configuredCssScope))
        {
            result.Add($"[{configuredCssScope}]");
        }

        foreach (Match match in DefaultBlazorScopeRegex.Matches(content))
        {
            if (match.Success)
            {
                result.Add(match.Value);
            }
        }

        return result;
    }

    private string GetUnscopeCssValue(ITaskItem item)
    {
        var value = item.GetMetadata("UnscopeCss");
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return item.GetMetadata("LegacyUnscopeCss");
    }

    private string FormatMessage(string messageFormat)
    {
        if (string.IsNullOrWhiteSpace(PhaseName))
        {
            return "[PoeShared.Blazor.UnscopedCss] " + messageFormat;
        }

        return $"[PoeShared.Blazor.UnscopedCss:{PhaseName}] " + messageFormat;
    }

    private static bool ShouldUnscope(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        return string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadFilePreservingBom(string path, out bool hadBom)
    {
        var bytes = File.ReadAllBytes(path);
        hadBom = bytes.Length >= Utf8Bom.Length &&
                 bytes[0] == Utf8Bom[0] &&
                 bytes[1] == Utf8Bom[1] &&
                 bytes[2] == Utf8Bom[2];

        if (hadBom)
        {
            return Encoding.UTF8.GetString(bytes, Utf8Bom.Length, bytes.Length - Utf8Bom.Length);
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static void WriteFilePreservingBom(string path, string content, bool hadBom)
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: hadBom);
        File.WriteAllText(path, content, encoding);
    }

    private static string? GetItemFullPath(string itemSpec)
    {
        if (string.IsNullOrWhiteSpace(itemSpec))
        {
            return null;
        }

        return Path.GetFullPath(itemSpec);
    }
}
