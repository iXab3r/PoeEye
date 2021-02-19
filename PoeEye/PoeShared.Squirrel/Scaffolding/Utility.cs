using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using NuGet;
using Splat;
using Squirrel;
using HttpUtility = System.Web.HttpUtility;

namespace PoeShared.Squirrel.Scaffolding
{
    internal static class Utility
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Utility));

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

        private static readonly string[] PeExtensions = {".exe", ".dll", ".node"};

        /// <summary>
        ///     The namespace for fully-qualified domain names (from RFC 4122, Appendix C).
        /// </summary>
        public static readonly Guid DnsNamespace = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        /// <summary>
        ///     The namespace for URLs (from RFC 4122, Appendix C).
        /// </summary>
        public static readonly Guid UrlNamespace = new Guid("6ba7b811-9dad-11d1-80b4-00c04fd430c8");

        /// <summary>
        ///     The namespace for ISO OIDs (from RFC 4122, Appendix C).
        /// </summary>
        public static readonly Guid IsoOidNamespace = new Guid("6ba7b812-9dad-11d1-80b4-00c04fd430c8");

        public static string RemoveByteOrderMarkerIfPresent(string content)
        {
            return string.IsNullOrEmpty(content)
                ? string.Empty
                : RemoveByteOrderMarkerIfPresent(Encoding.UTF8.GetBytes(content));
        }

        public static string RemoveByteOrderMarkerIfPresent(byte[] content)
        {
            byte[] output = { };

            if (content == null)
            {
                goto done;
            }

            Func<byte[], byte[], bool> matches = (bom, src) =>
            {
                if (src.Length < bom.Length)
                {
                    return false;
                }

                return !bom.Where((chr, index) => src[index] != chr).Any();
            };

            var utf32Be = new byte[] {0x00, 0x00, 0xFE, 0xFF};
            var utf32Le = new byte[] {0xFF, 0xFE, 0x00, 0x00};
            var utf16Be = new byte[] {0xFE, 0xFF};
            var utf16Le = new byte[] {0xFF, 0xFE};
            var utf8 = new byte[] {0xEF, 0xBB, 0xBF};

            if (matches(utf32Be, content))
            {
                output = new byte[content.Length - utf32Be.Length];
            }
            else if (matches(utf32Le, content))
            {
                output = new byte[content.Length - utf32Le.Length];
            }
            else if (matches(utf16Be, content))
            {
                output = new byte[content.Length - utf16Be.Length];
            }
            else if (matches(utf16Le, content))
            {
                output = new byte[content.Length - utf16Le.Length];
            }
            else if (matches(utf8, content))
            {
                output = new byte[content.Length - utf8.Length];
            }
            else
            {
                output = content;
            }

            done:
            if (output.Length > 0)
            {
                Buffer.BlockCopy(content, content.Length - output.Length, output, 0, output.Length);
            }

            return Encoding.UTF8.GetString(output);
        }

        public static IEnumerable<FileInfo> GetAllFilesRecursively(this DirectoryInfo rootPath)
        {
            Guard.ArgumentIsTrue(rootPath != null, "rootPath != null");

            return rootPath.EnumerateFiles("*", SearchOption.AllDirectories);
        }

        public static IEnumerable<string> GetAllFilePathsRecursively(string rootPath)
        {
            Guard.ArgumentIsTrue(rootPath != null, "rootPath != null");

            return Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories);
        }

        public static string CalculateFileSha1(string filePath)
        {
            Guard.ArgumentIsTrue(filePath != null, "filePath != null");

            using (var stream = File.OpenRead(filePath))
            {
                return CalculateStreamSha1(stream);
            }
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

        public static async Task CopyToAsync(string from, string to)
        {
            Guard.ArgumentIsTrue(!string.IsNullOrEmpty(from) && File.Exists(from), "!string.IsNullOrEmpty(from) && File.Exists(from)");
            Guard.ArgumentIsTrue(!string.IsNullOrEmpty(to), "!string.IsNullOrEmpty(to)");

            if (!File.Exists(from))
            {
                Log.WarnFormat("The file {0} does not exist", from);

                // TODO: should we fail this operation?
                return;
            }

            // XXX: SafeCopy
            await Task.Run(() => File.Copy(from, to, true));
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

        public static async Task<Tuple<int, string>> InvokeProcessAsync(ProcessStartInfo psi, CancellationToken ct)
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

        internal static string TempNameForIndex(int index, string prefix)
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

            Log.DebugFormat("Starting to delete folder: {0}", directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                Log.WarnFormat("DeleteDirectory: does not exist - {0}", directoryPath);
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
                Log.WarnFormat(message, ex);
            }

            var dirs = new string[0];
            try
            {
                dirs = Directory.GetDirectories(directoryPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                var message = $"The directories inside {directoryPath} could not be read";
                Log.WarnFormat(message, ex);
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

            Log.DebugFormat("Now deleting folder: {0}", directoryPath);
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

        public static string FindHelperExecutable(string toFind, IEnumerable<string> additionalDirs = null)
        {
            additionalDirs = additionalDirs ?? Enumerable.Empty<string>();
            var dirs = new[] {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}
                .Concat(additionalDirs ?? Enumerable.Empty<string>());

            var exe = @".\" + toFind;
            return dirs
                       .Select(x => Path.Combine(x, toFind))
                       .FirstOrDefault(x => File.Exists(x)) ??
                   exe;
        }

        private static string Find7Zip()
        {
            if (ModeDetector.InUnitTestRunner())
            {
                var vendorDir = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location.Replace("file:///", "")) ?? throw new InvalidOperationException(),
                    "..",
                    "..",
                    "..",
                    "vendor",
                    "7zip"
                );
                return FindHelperExecutable("7z.exe", new[] {vendorDir});
            }

            return FindHelperExecutable("7z.exe");
        }

        public static async Task ExtractZipToDirectory(string zipFilePath, string outFolder)
        {
            var sevenZip = Find7Zip();
            var result = default(Tuple<int, string>);

            try
            {
                var cmd = sevenZip;
                var args = $"x \"{zipFilePath}\" -tzip -mmt on -aoa -y -o\"{outFolder}\" *";
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    cmd = "wine";
                    args = sevenZip + " " + args;
                }

                result = await InvokeProcessAsync(cmd, args, CancellationToken.None);
                if (result.Item1 != 0)
                {
                    throw new Exception(result.Item2);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to extract file {zipFilePath} to {outFolder}", ex);
                throw;
            }
        }

        public static async Task CreateZipFromDirectory(string zipFilePath, string inFolder)
        {
            var sevenZip = Find7Zip();
            var result = default(Tuple<int, string>);

            try
            {
                var cmd = sevenZip;
                var args = $"a \"{zipFilePath}\" -tzip -aoa -y -mmt on *";
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    cmd = "wine";
                    args = sevenZip + " " + args;
                }

                result = await InvokeProcessAsync(cmd, args, CancellationToken.None, inFolder);
                if (result.Item1 != 0)
                {
                    throw new Exception(result.Item2);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to extract file {zipFilePath} to {inFolder}", ex);
                throw;
            }
        }

        public static string AppDirForRelease(string rootAppDirectory, IReleaseEntry entry)
        {
            return Path.Combine(rootAppDirectory, "app-" + entry.Version);
        }

        public static string AppDirForVersion(string rootAppDirectory, SemanticVersion version)
        {
            return Path.Combine(rootAppDirectory, "app-" + version);
        }

        public static string PackageDirectoryForAppDir(string rootAppDirectory)
        {
            return Path.Combine(rootAppDirectory, "packages");
        }

        public static string LocalReleaseFileForAppDir(string rootAppDirectory)
        {
            return Path.Combine(PackageDirectoryForAppDir(rootAppDirectory), "RELEASES");
        }

        public static IEnumerable<ReleaseEntry> LoadLocalReleases(string localReleaseFile)
        {
            var file = File.OpenRead(localReleaseFile);

            // NB: sr disposes file
            using (var sr = new StreamReader(file, Encoding.UTF8))
            {
                return ReleaseEntry.ParseReleaseFile(sr.ReadToEnd());
            }
        }

        public static T FindCurrentVersion<T>(IEnumerable<T> localReleases) where T : IReleaseEntry
        {
            return !localReleases.Any() ? default : localReleases.OrderByDescending(x => x.Version).FirstOrDefault(x => !x.IsDelta);
        }

        private static TAcc Scan<T, TAcc>(this IEnumerable<T> This, TAcc initialValue, Func<TAcc, T, TAcc> accFunc)
        {
            var acc = initialValue;

            foreach (var x in This)
            {
                acc = accFunc(acc, x);
            }

            return acc;
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

        // http://stackoverflow.com/questions/3111669/how-can-i-determine-the-subsystem-used-by-a-given-net-assembly
        public static bool ExecutableUsesWin32Subsystem(string peImage)
        {
            using (var s = new FileStream(peImage, FileMode.Open, FileAccess.Read))
            {
                var rawPeSignatureOffset = new byte[4];
                s.Seek(0x3c, SeekOrigin.Begin);
                s.Read(rawPeSignatureOffset, 0, 4);

                int peSignatureOffset = rawPeSignatureOffset[0];
                peSignatureOffset |= rawPeSignatureOffset[1] << 8;
                peSignatureOffset |= rawPeSignatureOffset[2] << 16;
                peSignatureOffset |= rawPeSignatureOffset[3] << 24;

                var coffHeader = new byte[24];
                s.Seek(peSignatureOffset, SeekOrigin.Begin);
                s.Read(coffHeader, 0, 24);

                byte[] signature = {(byte) 'P', (byte) 'E', (byte) '\0', (byte) '\0'};
                for (var index = 0; index < 4; index++)
                {
                    if (coffHeader[index] != signature[index])
                    {
                        throw new Exception("File is not a PE image");
                    }
                }

                var subsystemBytes = new byte[2];
                s.Seek(68, SeekOrigin.Current);
                s.Read(subsystemBytes, 0, 2);

                var subSystem = subsystemBytes[0] | (subsystemBytes[1] << 8);
                return subSystem == 2; /*IMAGE_SUBSYSTEM_WINDOWS_GUI*/
            }
        }

        public static bool FileIsLikelyPeImage(string name)
        {
            var ext = Path.GetExtension(name);
            return PeExtensions.Any(x => ext.Equals(x, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsFileTopLevelInPackage(string fullName, string pkgPath)
        {
            var fn = fullName.ToLowerInvariant();
            var pkg = pkgPath.ToLowerInvariant();
            var relativePath = fn.Replace(pkg, "");

            // NB: We want to match things like `/lib/net45/foo.exe` but not `/lib/net45/bar/foo.exe`
            return relativePath.Split(Path.DirectorySeparatorChar).Length == 4;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, MoveFileFlags dwFlags);

        public static Guid CreateGuidFromHash(string text)
        {
            return CreateGuidFromHash(text, IsoOidNamespace);
        }

        public static Guid CreateGuidFromHash(byte[] data)
        {
            return CreateGuidFromHash(data, IsoOidNamespace);
        }

        public static Guid CreateGuidFromHash(string text, Guid namespaceId)
        {
            return CreateGuidFromHash(Encoding.UTF8.GetBytes(text), namespaceId);
        }

        public static Guid CreateGuidFromHash(byte[] nameBytes, Guid namespaceId)
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

        [Flags]
        private enum MoveFileFlags
        {
            MOVEFILE_REPLACE_EXISTING = 0x00000001,
            MOVEFILE_COPY_ALLOWED = 0x00000002,
            MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004,
            MOVEFILE_WRITE_THROUGH = 0x00000008,
            MOVEFILE_CREATE_HARDLINK = 0x00000010,
            MOVEFILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
        }
    }
}