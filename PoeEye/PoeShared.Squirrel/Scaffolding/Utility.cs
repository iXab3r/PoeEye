using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Squirrel;
using HttpUtility = System.Web.HttpUtility;

namespace PoeShared.Squirrel.Scaffolding;

internal static class Utility
{
    private static readonly IFluentLog Log = typeof(Utility).PrepareLogger();

    private static readonly Lazy<string> DirectoryChars = new Lazy<string>(
        () =>
        {
            return "abcdefghijklmnopqrstuvwxyz" +
                   Enumerable.Range(0x03B0, 0x03FF - 0x03B0) // Greek and Coptic
                       .Concat(Enumerable.Range(0x0400, 0x04FF - 0x0400)) // Cyrillic
                       .Aggregate(
                           new StringBuilder(),
                           (acc, x) =>
                           {
                               acc.Append(char.ConvertFromUtf32(x));
                               return acc;
                           });
        });

    /// <summary>
    ///     The namespace for ISO OIDs (from RFC 4122, Appendix C).
    /// </summary>
    public static readonly Guid IsoOidNamespace = new Guid("6ba7b812-9dad-11d1-80b4-00c04fd430c8");

    public static IEnumerable<FileInfo> GetAllFilesRecursively(this DirectoryInfo rootPath)
    {
        Guard.ArgumentIsTrue(rootPath != null, "rootPath != null");

        return rootPath.EnumerateFiles("*", SearchOption.AllDirectories);
    }

    public static string CalculateStreamSha1(Stream file)
    {
        Guard.ArgumentIsTrue(file != null && file.CanRead, "file != null && file.CanRead");

        using (var sha1 = SHA1.Create())
        {
            return BitConverter.ToString(sha1.ComputeHash(file)).Replace("-", string.Empty);
        }
    }

    public static WebClient CreateWebClient()
    {
        // WHY DOESNT IT JUST DO THISSSSSSSS
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

        var ret = new WebClient();
        var wp = WebRequest.DefaultWebProxy;
        if (wp != null)
        {
            wp.Credentials = CredentialCache.DefaultCredentials;
            ret.Proxy = wp;
        }

        return ret;
    }

    public static void Retry(this Action block, int retries = 2)
    {
        Guard.ArgumentIsTrue(retries > 0, "retries > 0");

        Func<object> thunk = () =>
        {
            block();
            return null;
        };

        thunk.Retry(retries);
    }
        
    public static async Task<long> GetRemoteFileSize(Uri uriPath)
    {
        var webRequest = WebRequest.Create(uriPath);
        webRequest.Method = "HEAD";

        using var webResponse = await webRequest.GetResponseAsync();
            
        var contentLength = webResponse.Headers.Get("Content-Length");
        if (long.TryParse(contentLength, out var fileSize))
        {
            return fileSize;
        }

        throw new ApplicationException($"Failed to get file size from {uriPath}, content headers: {webRequest.Headers}");
    }

    public static T Retry<T>(this Func<T> block, int retries = 2)
    {
        Guard.ArgumentIsTrue(retries > 0, "retries > 0");

        while (true)
        {
            try
            {
                var ret = block();
                return ret;
            }
            catch (Exception)
            {
                if (retries == 0)
                {
                    throw;
                }

                retries--;
                Thread.Sleep(250);
            }
        }
    }

    public static Task<Tuple<int, string>> InvokeProcessAsync(string fileName, string arguments, CancellationToken ct, string workingDirectory = "")
    {
        var psi = new ProcessStartInfo(fileName, arguments);
        if (Environment.OSVersion.Platform != PlatformID.Win32NT && fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            psi = new ProcessStartInfo("wine", fileName + " " + arguments);
        }

        psi.UseShellExecute = false;
        psi.WindowStyle = ProcessWindowStyle.Hidden;
        psi.ErrorDialog = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.WorkingDirectory = workingDirectory;

        return InvokeProcessAsync(psi, ct);
    }

    private static async Task<Tuple<int, string>> InvokeProcessAsync(ProcessStartInfo psi, CancellationToken ct)
    {
        var pi = Process.Start(psi);
        await Task.Run(
            () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    if (pi.WaitForExit(2000))
                    {
                        return;
                    }
                }

                if (ct.IsCancellationRequested)
                {
                    pi.Kill();
                    ct.ThrowIfCancellationRequested();
                }
            });

        var textResult = await pi.StandardOutput.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(textResult) || pi.ExitCode != 0)
        {
            textResult = (textResult ?? "") + "\n" + await pi.StandardError.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(textResult))
            {
                textResult = string.Empty;
            }
        }

        return Tuple.Create(pi.ExitCode, textResult.Trim());
    }

    public static Task ForEachAsync<T>(this IEnumerable<T> source, Action<T> body, int degreeOfParallelism = 4)
    {
        return ForEachAsync(source, x => Task.Run(() => body(x)), degreeOfParallelism);
    }

    public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body, int degreeOfParallelism = 4)
    {
        return Task.WhenAll(
            from partition in Partitioner.Create(source).GetPartitions(degreeOfParallelism)
            select Task.Run(
                async () =>
                {
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            await body(partition.Current);
                        }
                    }
                }));
    }

    private static string TempNameForIndex(int index, string prefix)
    {
        if (index < DirectoryChars.Value.Length)
        {
            return prefix + DirectoryChars.Value[index];
        }

        return prefix + DirectoryChars.Value[index % DirectoryChars.Value.Length] + TempNameForIndex(index / DirectoryChars.Value.Length, "");
    }

    private static DirectoryInfo GetTempDirectory(string localAppDirectory)
    {
        var tempDir = Path.Combine(localAppDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SquirrelTemp");

        var di = new DirectoryInfo(tempDir);
        if (!di.Exists)
        {
            di.Create();
        }

        return di;
    }

    public static IDisposable WithTempDirectory(out string path, string localAppDirectory = null)
    {
        var di = GetTempDirectory(localAppDirectory);
        var tempDir = default(DirectoryInfo);

        var names = Enumerable.Range(0, 1 << 20).Select(x => TempNameForIndex(x, "temp"));

        foreach (var name in names)
        {
            var target = Path.Combine(di.FullName, name);

            if (!File.Exists(target) && !Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
                tempDir = new DirectoryInfo(target);
                break;
            }
        }

        path = tempDir.FullName;

        return Disposable.Create(() => Task.Run(async () => await DeleteDirectory(tempDir.FullName)).Wait());
    }

    public static IDisposable WithTempFile(out string path, string localAppDirectory = null)
    {
        var di = GetTempDirectory(localAppDirectory);
        var names = Enumerable.Range(0, 1 << 20).Select(x => TempNameForIndex(x, "temp"));

        path = "";
        foreach (var name in names)
        {
            path = Path.Combine(di.FullName, name);

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                break;
            }
        }

        var thePath = path;
        return Disposable.Create(() => File.Delete(thePath));
    }

    public static async Task DeleteDirectory(string directoryPath)
    {
        Guard.ArgumentIsTrue(!string.IsNullOrEmpty(directoryPath), "!string.IsNullOrEmpty(directoryPath)");

        Log.Debug(() => $"Starting to delete folder: {directoryPath}");

        if (!Directory.Exists(directoryPath))
        {
            Log.Warn($"DeleteDirectory: does not exist - {directoryPath}");
            return;
        }

        // From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502
        var files = new string[0];
        try
        {
            files = Directory.GetFiles(directoryPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            var message = $"The files inside {directoryPath} could not be read";
            Log.Warn(message, ex);
        }

        var dirs = new string[0];
        try
        {
            dirs = Directory.GetDirectories(directoryPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            var message = $"The directories inside {directoryPath} could not be read";
            Log.Warn(message, ex);
        }

        var fileOperations = files.ForEachAsync(
            file =>
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            });

        var directoryOperations =
            dirs.ForEachAsync(async dir => await DeleteDirectory(dir));

        await Task.WhenAll(fileOperations, directoryOperations);

        Log.Debug(() => $"Now deleting folder: {directoryPath}");
        File.SetAttributes(directoryPath, FileAttributes.Normal);

        try
        {
            Directory.Delete(directoryPath, false);
        }
        catch (Exception ex)
        {
            var message = $"DeleteDirectory: could not delete - {directoryPath}";
            Log.Error(message, ex);
        }
    }

    public static string AppDirForRelease(string rootAppDirectory, IReleaseEntry entry)
    {
        return Path.Combine(rootAppDirectory, "app-" + entry.Version);
    }

    public static string PackageDirectoryForAppDir(string rootAppDirectory)
    {
        return Path.Combine(rootAppDirectory, "packages");
    }

    public static IReadOnlyList<FileInfo> EnumeratePackagesForApp(string rootAppDirectory)
    {
        var packagesDirectory = PackageDirectoryForAppDir(rootAppDirectory);
        var packages = Directory.GetFiles(packagesDirectory, "*.nupkg", SearchOption.TopDirectoryOnly).Select(x => new FileInfo(x));
        return packages.ToList();
    }

    public static string LocalReleaseFileForAppDir(string rootAppDirectory)
    {
        return Path.Combine(PackageDirectoryForAppDir(rootAppDirectory), "RELEASES");
    }

    public static void WriteLocalReleases(string localReleaseFile, IEnumerable<IReleaseEntry> entries)
    {
        var content = string.Join("\n", entries.OrderBy(x => x.Version).ThenByDescending(x => x.IsDelta).Select(x => x.EntryAsString));
        using var file = File.Open(localReleaseFile, FileMode.Create, FileAccess.ReadWrite);
        using var writer = new StreamWriter(file, Encoding.UTF8);
        writer.Write(content);
    }
        
    public static IReadOnlyList<IReleaseEntry> LoadLocalReleases(string localReleaseFile)
    {
        using var file = File.OpenRead(localReleaseFile);
        using var sr = new StreamReader(file, Encoding.UTF8);
        return ReleaseEntry.ParseReleaseFile(sr.ReadToEnd()).ToList();
    }

    public static T FindCurrentVersion<T>(IEnumerable<T> localReleases) where T : IReleaseEntry
    {
        return !localReleases.Any() ? default : localReleases.OrderByDescending(x => x.Version).FirstOrDefault(x => !x.IsDelta);
    }

    public static bool IsHttpUrl(string urlOrPath)
    {
        if (!Uri.TryCreate(urlOrPath, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    public static Uri AppendPathToUri(Uri uri, string path)
    {
        var builder = new UriBuilder(uri);
        if (!builder.Path.EndsWith("/"))
        {
            builder.Path += "/";
        }

        builder.Path += path;
        return builder.Uri;
    }

    public static Uri EnsureTrailingSlash(Uri uri)
    {
        return AppendPathToUri(uri, "");
    }

    public static Uri AddQueryParamsToUri(Uri uri, IEnumerable<KeyValuePair<string, string>> newQuery)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);

        foreach (var entry in newQuery)
        {
            query[entry.Key] = entry.Value;
        }

        var builder = new UriBuilder(uri);
        builder.Query = query.ToString();

        return builder.Uri;
    }

    public static void DeleteFileHarder(string path, bool ignoreIfFails = false)
    {
        try
        {
            Retry(() => File.Delete(path));
        }
        catch (Exception ex)
        {
            if (ignoreIfFails)
            {
                return;
            }

            Log.Error("Really couldn't delete file: " + path, ex);
            throw;
        }
    }

    public static async Task DeleteDirectoryOrJustGiveUp(string dir)
    {
        try
        {
            await DeleteDirectory(dir);
        }
        catch
        {
            var message = $"Uninstall failed to delete dir '{dir}'";
        }
    }

    public static Guid CreateGuidFromHash(string text)
    {
        return CreateGuidFromHash(text, IsoOidNamespace);
    }

    public static Guid CreateGuidFromHash(byte[] data)
    {
        return CreateGuidFromHash(data, IsoOidNamespace);
    }

    private static Guid CreateGuidFromHash(string text, Guid namespaceId)
    {
        return CreateGuidFromHash(Encoding.UTF8.GetBytes(text), namespaceId);
    }

    private static Guid CreateGuidFromHash(byte[] nameBytes, Guid namespaceId)
    {
        // convert the namespace UUID to network order (step 3)
        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);

        // comput the hash of the name space ID concatenated with the 
        // name (step 4)
        byte[] hash;
        using (var algorithm = SHA1.Create())
        {
            algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
            algorithm.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
            hash = algorithm.Hash;
        }

        // most bytes from the hash are copied straight to the bytes of 
        // the new GUID (steps 5-7, 9, 11-12)
        var newGuid = new byte[16];
        Array.Copy(hash, 0, newGuid, 0, 16);

        // set the four most significant bits (bits 12 through 15) of 
        // the time_hi_and_version field to the appropriate 4-bit 
        // version number from Section 4.1.3 (step 8)
        newGuid[6] = (byte) ((newGuid[6] & 0x0F) | (5 << 4));

        // set the two most significant bits (bits 6 and 7) of the 
        // clock_seq_hi_and_reserved to zero and one, respectively 
        // (step 10)
        newGuid[8] = (byte) ((newGuid[8] & 0x3F) | 0x80);

        // convert the resulting UUID to local byte order (step 13)
        SwapByteOrder(newGuid);
        return new Guid(newGuid);
    }

    // Converts a GUID (expressed as a byte array) to/from network order (MSB-first).
    private static void SwapByteOrder(byte[] guid)
    {
        SwapBytes(guid, 0, 3);
        SwapBytes(guid, 1, 2);
        SwapBytes(guid, 4, 5);
        SwapBytes(guid, 6, 7);
    }

    private static void SwapBytes(byte[] guid, int left, int right)
    {
        var temp = guid[left];
        guid[left] = guid[right];
        guid[right] = temp;
    }
}