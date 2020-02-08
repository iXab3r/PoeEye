using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using log4net;
using MarkdownSharp;
using NuGet;
using SharpCompress.Archives.Zip;
using SharpCompress.Readers;
using Splat;
using Squirrel;

namespace PoeShared.Squirrel.Scaffolding
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> Return<T>(T value)
        {
            yield return value;
        }

        /// <summary>
        ///     Enumerates the sequence and invokes the given action for each value in the sequence.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="onNext">Action to invoke for each element.</param>
        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (onNext == null)
            {
                throw new ArgumentNullException(nameof(onNext));
            }

            foreach (var item in source)
            {
                onNext(item);
            }
        }

        /// <summary>
        ///     Returns the elements with the maximum key value by using the default comparer to compare key values.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="keySelector">Key selector used to extract the key for each element in the sequence.</param>
        /// <returns>List with the elements that share the same maximum key value.</returns>
        public static IList<TSource> MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            return MaxBy(source, keySelector, Comparer<TKey>.Default);
        }

        /// <summary>
        ///     Returns the elements with the minimum key value by using the specified comparer to compare key values.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="keySelector">Key selector used to extract the key for each element in the sequence.</param>
        /// <param name="comparer">Comparer used to determine the maximum key value.</param>
        /// <returns>List with the elements that share the same maximum key value.</returns>
        public static IList<TSource> MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return ExtremaBy(source, keySelector, (key, minValue) => comparer.Compare(key, minValue));
        }

        private static IList<TSource> ExtremaBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, int> compare)
        {
            var result = new List<TSource>();

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw new InvalidOperationException("Source sequence doesn't contain any elements.");
                }

                var current = e.Current;
                var resKey = keySelector(current);
                result.Add(current);

                while (e.MoveNext())
                {
                    var cur = e.Current;
                    var key = keySelector(cur);

                    var cmp = compare(key, resKey);
                    if (cmp == 0)
                    {
                        result.Add(cur);
                    }
                    else if (cmp > 0)
                    {
                        result = new List<TSource> {cur};
                        resKey = key;
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Lazily invokes an action for each value in the sequence.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="onNext">Action to invoke for each element.</param>
        /// <returns>Sequence exhibiting the specified side-effects upon enumeration.</returns>
        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (onNext == null)
            {
                throw new ArgumentNullException(nameof(onNext));
            }

            return DoHelper(source, onNext, _ => { }, () => { });
        }

        /// <summary>
        ///     Lazily invokes an action for each value in the sequence, and executes an action for successful termination.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="onNext">Action to invoke for each element.</param>
        /// <param name="onCompleted">Action to invoke on successful termination of the sequence.</param>
        /// <returns>Sequence exhibiting the specified side-effects upon enumeration.</returns>
        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action onCompleted)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (onNext == null)
            {
                throw new ArgumentNullException(nameof(onNext));
            }

            if (onCompleted == null)
            {
                throw new ArgumentNullException(nameof(onCompleted));
            }

            return DoHelper(source, onNext, _ => { }, onCompleted);
        }

        /// <summary>
        ///     Lazily invokes an action for each value in the sequence, and executes an action upon exceptional termination.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="onNext">Action to invoke for each element.</param>
        /// <param name="onError">Action to invoke on exceptional termination of the sequence.</param>
        /// <returns>Sequence exhibiting the specified side-effects upon enumeration.</returns>
        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action<Exception> onError)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (onNext == null)
            {
                throw new ArgumentNullException(nameof(onNext));
            }

            if (onError == null)
            {
                throw new ArgumentNullException(nameof(onError));
            }

            return DoHelper(source, onNext, onError, () => { });
        }

        /// <summary>
        ///     Lazily invokes an action for each value in the sequence, and executes an action upon successful or exceptional
        ///     termination.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="onNext">Action to invoke for each element.</param>
        /// <param name="onError">Action to invoke on exceptional termination of the sequence.</param>
        /// <param name="onCompleted">Action to invoke on successful termination of the sequence.</param>
        /// <returns>Sequence exhibiting the specified side-effects upon enumeration.</returns>
        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (onNext == null)
            {
                throw new ArgumentNullException(nameof(onNext));
            }

            if (onError == null)
            {
                throw new ArgumentNullException(nameof(onError));
            }

            if (onCompleted == null)
            {
                throw new ArgumentNullException(nameof(onCompleted));
            }

            return DoHelper(source, onNext, onError, onCompleted);
        }

        private static IEnumerable<TSource> DoHelper<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action<Exception> onError,
            Action onCompleted)
        {
            using (var e = source.GetEnumerator())
            {
                while (true)
                {
                    var current = default(TSource);
                    try
                    {
                        if (!e.MoveNext())
                        {
                            break;
                        }

                        current = e.Current;
                    }
                    catch (Exception ex)
                    {
                        onError(ex);
                        throw;
                    }

                    onNext(current);
                    yield return current;
                }

                onCompleted();
            }
        }

        /// <summary>
        ///     Returns the source sequence prefixed with the specified value.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="values">Values to prefix the sequence with.</param>
        /// <returns>Sequence starting with the specified prefix value, followed by the source sequence.</returns>
        public static IEnumerable<TSource> StartWith<TSource>(this IEnumerable<TSource> source, params TSource[] values)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.StartWith_(values);
        }

        private static IEnumerable<TSource> StartWith_<TSource>(this IEnumerable<TSource> source, params TSource[] values)
        {
            foreach (var x in values)
            {
                yield return x;
            }

            foreach (var item in source)
            {
                yield return item;
            }
        }

        /// <summary>
        ///     Returns elements with a distinct key value by using the default equality comparer to compare key values.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="keySelector">Key selector.</param>
        /// <returns>Sequence that contains the elements from the source sequence with distinct key values.</returns>
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            return source.Distinct_(keySelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        ///     Returns elements with a distinct key value by using the specified equality comparer to compare key values.
        /// </summary>
        /// <typeparam name="TSource">Source sequence element type.</typeparam>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <param name="source">Source sequence.</param>
        /// <param name="keySelector">Key selector.</param>
        /// <param name="comparer">Comparer used to compare key values.</param>
        /// <returns>Sequence that contains the elements from the source sequence with distinct key values.</returns>
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return source.Distinct_(keySelector, comparer);
        }

        private static IEnumerable<TSource> Distinct_<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            var set = new HashSet<TKey>(comparer);

            foreach (var item in source)
            {
                var key = keySelector(item);

                if (set.Add(key))
                {
                    yield return item;
                }
            }
        }
    }

    internal static class FrameworkTargetVersion
    {
        public static FrameworkName Net40 = new FrameworkName(".NETFramework,Version=v4.0");
        public static FrameworkName Net45 = new FrameworkName(".NETFramework,Version=v4.5");
    }

    public interface IReleasePackage
    {
        string InputPackageFile { get; }
        string ReleasePackageFile { get; }
        string SuggestedReleaseFileName { get; }

        string CreateReleasePackage(string outputFile, string packagesRootDir = null, Func<string, string> releaseNotesProcessor = null,
            Action<string> contentsPostProcessHook = null);
    }

    public static class VersionComparer
    {
        public static bool Matches(IVersionSpec versionSpec, SemanticVersion version)
        {
            if (versionSpec == null)
            {
                return true; // I CAN'T DEAL WITH THIS
            }

            bool minVersion;
            if (versionSpec.MinVersion == null)
            {
                minVersion = true; // no preconditon? LET'S DO IT
            }
            else if (versionSpec.IsMinInclusive)
            {
                minVersion = version >= versionSpec.MinVersion;
            }
            else
            {
                minVersion = version > versionSpec.MinVersion;
            }

            bool maxVersion;
            if (versionSpec.MaxVersion == null)
            {
                maxVersion = true; // no preconditon? LET'S DO IT
            }
            else if (versionSpec.IsMaxInclusive)
            {
                maxVersion = version <= versionSpec.MaxVersion;
            }
            else
            {
                maxVersion = version < versionSpec.MaxVersion;
            }

            return maxVersion && minVersion;
        }
    }

    public class ReleasePackage : IEnableLogger, IReleasePackage
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ReleasePackage));

        public ReleasePackage(string inputPackageFile, bool isReleasePackage = false)
        {
            InputPackageFile = inputPackageFile;

            if (isReleasePackage)
            {
                ReleasePackageFile = inputPackageFile;
            }
        }

        public SemanticVersion Version => InputPackageFile.ToSemanticVersion();

        public string InputPackageFile { get; protected set; }
        public string ReleasePackageFile { get; protected set; }

        public string SuggestedReleaseFileName
        {
            get
            {
                var zp = new ZipPackage(InputPackageFile);
                return $"{zp.Id}-{zp.Version}-full.nupkg";
            }
        }

        public string CreateReleasePackage(string outputFile, string packagesRootDir = null, Func<string, string> releaseNotesProcessor = null,
            Action<string> contentsPostProcessHook = null)
        {
            Guard.ArgumentIsTrue(!string.IsNullOrEmpty(outputFile), "!string.IsNullOrEmpty(outputFile)");
            releaseNotesProcessor = releaseNotesProcessor ?? (x => new Markdown().Transform(x));

            if (ReleasePackageFile != null)
            {
                return ReleasePackageFile;
            }

            var package = new ZipPackage(InputPackageFile);

            var dontcare = default(SemanticVersion);

            // NB: Our test fixtures use packages that aren't SemVer compliant, 
            // we don't really care that they aren't valid
            if (!ModeDetector.InUnitTestRunner() && !SemanticVersion.TryParseStrict(package.Version.ToString(), out dontcare))
            {
                throw new Exception(
                    $"Your package version is currently {package.Version}, which is *not* SemVer-compatible, change this to be a SemVer version number");
            }

            // we can tell from here what platform(s) the package targets
            // but given this is a simple package we only
            // ever expect one entry here (crash hard otherwise)
            var frameworks = package.GetSupportedFrameworks();
            if (frameworks.Count() > 1)
            {
                var platforms = frameworks
                    .Aggregate(new StringBuilder(), (sb, f) => sb.Append(f + "; "));

                throw new InvalidOperationException(
                    $"The input package file {InputPackageFile} targets multiple platforms - {platforms} - and cannot be transformed into a release package.");
            }

            if (!frameworks.Any())
            {
                throw new InvalidOperationException(
                    $"The input package file {InputPackageFile} targets no platform and cannot be transformed into a release package.");
            }

            var targetFramework = frameworks.Single();

            // Recursively walk the dependency tree and extract all of the
            // dependent packages into the a temporary directory
            Log.InfoFormat("Creating release package: {0} => {1}", InputPackageFile, outputFile);
            var dependencies = FindAllDependentPackages(
                    package,
                    new LocalPackageRepository(packagesRootDir),
                    frameworkName: targetFramework)
                .ToArray();

            string tempPath = null;

            using (Utility.WithTempDirectory(out tempPath))
            {
                var tempDir = new DirectoryInfo(tempPath);

                ExtractZipWithEscaping(InputPackageFile, tempPath).Wait();

                Log.InfoFormat("Extracting dependent packages: [{0}]", string.Join(",", dependencies.Select(x => x.Id)));
                ExtractDependentPackages(dependencies, tempDir, targetFramework);

                var specPath = tempDir.GetFiles("*.nuspec").First().FullName;

                Log.InfoFormat("Removing unnecessary data");
                RemoveDependenciesFromPackageSpec(specPath);
                RemoveDeveloperDocumentation(tempDir);

                if (releaseNotesProcessor != null)
                {
                    RenderReleaseNotesMarkdown(specPath, releaseNotesProcessor);
                }

                AddDeltaFilesToContentTypes(tempDir.FullName);

                contentsPostProcessHook?.Invoke(tempPath);

                Utility.CreateZipFromDirectory(outputFile, tempPath).Wait();

                ReleasePackageFile = outputFile;
                return ReleasePackageFile;
            }
        }

        private static Task ExtractZipWithEscaping(string zipFilePath, string outFolder)
        {
            return Task.Run(
                () =>
                {
                    using (var za = ZipArchive.Open(zipFilePath))
                    using (var reader = za.ExtractAllEntries())
                    {
                        while (reader.MoveToNextEntry())
                        {
                            var parts = reader.Entry.Key.Split('\\', '/').Select(x => Uri.UnescapeDataString(x));
                            var decoded = string.Join(Path.DirectorySeparatorChar.ToString(), parts);

                            var fullTargetFile = Path.Combine(outFolder, decoded);
                            var fullTargetDir = Path.GetDirectoryName(fullTargetFile);
                            Directory.CreateDirectory(fullTargetDir);

                            Utility.Retry(
                                () =>
                                {
                                    if (reader.Entry.IsDirectory)
                                    {
                                        Directory.CreateDirectory(Path.Combine(outFolder, decoded));
                                    }
                                    else
                                    {
                                        reader.WriteEntryToFile(Path.Combine(outFolder, decoded));
                                    }
                                },
                                5);
                        }
                    }
                });
        }

        public static Task ExtractZipForInstall(string zipFilePath, string outFolder, string rootPackageFolder)
        {
            var re = new Regex(@"lib[\\\/][^\\\/]*[\\\/]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            return Task.Run(
                () =>
                {
                    using (var za = ZipArchive.Open(zipFilePath))
                    using (var reader = za.ExtractAllEntries())
                    {
                        while (reader.MoveToNextEntry())
                        {
                            var parts = reader.Entry.Key.Split('\\', '/');
                            var decoded = string.Join(Path.DirectorySeparatorChar.ToString(), parts);

                            if (!re.IsMatch(decoded))
                            {
                                continue;
                            }

                            decoded = re.Replace(decoded, "", 1);

                            var fullTargetFile = Path.Combine(outFolder, decoded);
                            var fullTargetDir = Path.GetDirectoryName(fullTargetFile);
                            Directory.CreateDirectory(fullTargetDir);

                            var failureIsOkay = false;
                            if (!reader.Entry.IsDirectory && decoded.Contains("_ExecutionStub.exe"))
                            {
                                // NB: On upgrade, many of these stubs will be in-use, nbd tho.
                                failureIsOkay = true;

                                fullTargetFile = Path.Combine(
                                    rootPackageFolder,
                                    Path.GetFileName(decoded).Replace("_ExecutionStub.exe", ".exe"));

                                LogHost.Default.Info("Rigging execution stub for {0} to {1}", decoded, fullTargetFile);
                            }

                            try
                            {
                                Utility.Retry(
                                    () =>
                                    {
                                        if (reader.Entry.IsDirectory)
                                        {
                                            Directory.CreateDirectory(fullTargetFile);
                                        }
                                        else
                                        {
                                            reader.WriteEntryToFile(fullTargetFile);
                                        }
                                    },
                                    5);
                            }
                            catch (Exception e)
                            {
                                if (!failureIsOkay)
                                {
                                    throw;
                                }

                                Log.Warn("Can't write execution stub, probably in use", e);
                            }
                        }
                    }
                });
        }

        private void ExtractDependentPackages(IEnumerable<IPackage> dependencies, DirectoryInfo tempPath, FrameworkName framework)
        {
            dependencies.ForEach(
                pkg =>
                {
                    Log.InfoFormat("Scanning {0}", pkg.Id);

                    pkg.GetLibFiles()
                        .ForEach(
                            file =>
                            {
                                var outPath = new FileInfo(Path.Combine(tempPath.FullName, file.Path));

                                if (!VersionUtility.IsCompatible(framework, new[] {file.TargetFramework}))
                                {
                                    Log.InfoFormat("Ignoring {0} as the target framework is not compatible", outPath);
                                    return;
                                }

                                Directory.CreateDirectory(outPath.Directory.FullName);

                                using (var of = File.Create(outPath.FullName))
                                {
                                    Log.InfoFormat("Writing {0} to {1}", file.Path, outPath);
                                    file.GetStream().CopyTo(of);
                                }
                            });
                });
        }

        private void RemoveDeveloperDocumentation(DirectoryInfo expandedRepoPath)
        {
            expandedRepoPath.GetAllFilesRecursively()
                .Where(x => x.Name.EndsWith(".dll", true, CultureInfo.InvariantCulture))
                .Select(x => new FileInfo(x.FullName.ToLowerInvariant().Replace(".dll", ".xml")))
                .Where(x => x.Exists)
                .ForEach(x => x.Delete());
        }

        private void RenderReleaseNotesMarkdown(string specPath, Func<string, string> releaseNotesProcessor)
        {
            var doc = new XmlDocument();
            doc.Load(specPath);

            // XXX: This code looks full tart
            var metadata = doc.DocumentElement.ChildNodes
                .OfType<XmlElement>()
                .First(x => x.Name.ToLowerInvariant() == "metadata");

            var releaseNotes = metadata.ChildNodes
                .OfType<XmlElement>()
                .FirstOrDefault(x => x.Name.ToLowerInvariant() == "releasenotes");

            if (releaseNotes == null)
            {
                Log.InfoFormat("No release notes found in {0}", specPath);
                return;
            }

            releaseNotes.InnerText = string.Format(
                "<![CDATA[\n" + "{0}\n" + "]]>",
                releaseNotesProcessor(releaseNotes.InnerText));

            doc.Save(specPath);
        }

        private void RemoveDependenciesFromPackageSpec(string specPath)
        {
            var xdoc = new XmlDocument();
            xdoc.Load(specPath);

            var metadata = xdoc.DocumentElement.FirstChild;
            var dependenciesNode = metadata.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.Name.ToLowerInvariant() == "dependencies");
            if (dependenciesNode != null)
            {
                metadata.RemoveChild(dependenciesNode);
            }

            xdoc.Save(specPath);
        }

        internal IEnumerable<IPackage> FindAllDependentPackages(
            IPackage package = null,
            IPackageRepository packageRepository = null,
            HashSet<string> packageCache = null,
            FrameworkName frameworkName = null)
        {
            package = package ?? new ZipPackage(InputPackageFile);
            packageCache = packageCache ?? new HashSet<string>();

            var deps = package.DependencySets
                .Where(x => x.TargetFramework == null || x.TargetFramework == frameworkName)
                .SelectMany(x => x.Dependencies);

            return deps.SelectMany(
                    dependency =>
                    {
                        var ret = MatchPackage(packageRepository, dependency.Id, dependency.VersionSpec);

                        if (ret == null)
                        {
                            var message = string.Format("Couldn't find file for package in {1}: {0}", dependency.Id, packageRepository.Source);
                            Log.ErrorFormat(message);
                            throw new Exception(message);
                        }

                        if (packageCache.Contains(ret.GetFullName()))
                        {
                            return Enumerable.Empty<IPackage>();
                        }

                        packageCache.Add(ret.GetFullName());

                        return FindAllDependentPackages(ret, packageRepository, packageCache, frameworkName).StartWith(ret).Distinct(y => y.GetFullName());
                    })
                .ToArray();
        }

        private IPackage MatchPackage(IPackageRepository packageRepository, string id, IVersionSpec version)
        {
            return packageRepository.FindPackagesById(id).FirstOrDefault(x => VersionComparer.Matches(version, x.Version));
        }


        internal static void AddDeltaFilesToContentTypes(string rootDirectory)
        {
            var doc = new XmlDocument();
            var path = Path.Combine(rootDirectory, "[Content_Types].xml");
            doc.Load(path);

            ContentType.Merge(doc);
            ContentType.Clean(doc);

            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                doc.Save(sw);
            }
        }
    }

    public class ChecksumFailedException : Exception
    {
        public string Filename { get; set; }
    }

    internal static class ContentType
    {
        public static void Clean(XmlDocument doc)
        {
            var typesElement = doc.FirstChild.NextSibling;
            if (typesElement.Name.ToLowerInvariant() != "types")
            {
                throw new Exception("Invalid ContentTypes file, expected root node should be 'Types'");
            }

            var children = typesElement.ChildNodes.OfType<XmlElement>();

            foreach (var child in children)
            {
                if (child.GetAttribute("Extension") == "")
                {
                    typesElement.RemoveChild(child);
                }
            }
        }

        public static void Merge(XmlDocument doc)
        {
            var elements = new[]
            {
                Tuple.Create("Default", "diff", "application/octet"),
                Tuple.Create("Default", "bsdiff", "application/octet"),
                Tuple.Create("Default", "exe", "application/octet"),
                Tuple.Create("Default", "dll", "application/octet"),
                Tuple.Create("Default", "shasum", "text/plain")
            };

            var typesElement = doc.FirstChild.NextSibling;
            if (typesElement.Name.ToLowerInvariant() != "types")
            {
                throw new Exception("Invalid ContentTypes file, expected root node should be 'Types'");
            }

            var existingTypes = typesElement.ChildNodes.OfType<XmlElement>()
                .Select(
                    k => Tuple.Create(
                        k.Name,
                        k.GetAttribute("Extension").ToLowerInvariant(),
                        k.GetAttribute("ContentType").ToLowerInvariant()));

            var toAdd = elements
                .Where(x => existingTypes.All(t => t.Item2 != x.Item2.ToLowerInvariant()))
                .Select(
                    element =>
                    {
                        var ret = doc.CreateElement(element.Item1, typesElement.NamespaceURI);

                        var ext = doc.CreateAttribute("Extension");
                        ext.Value = element.Item2;
                        var ct = doc.CreateAttribute("ContentType");
                        ct.Value = element.Item3;

                        ret.Attributes.Append(ext);
                        ret.Attributes.Append(ct);

                        return ret;
                    });

            foreach (var v in toAdd)
            {
                typesElement.AppendChild(v);
            }
        }
    }
}