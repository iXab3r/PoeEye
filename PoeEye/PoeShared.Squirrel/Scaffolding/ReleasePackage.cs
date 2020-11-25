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

        public string InputPackageFile { get; }
        
        public string ReleasePackageFile { get; private set; }

        public string SuggestedReleaseFileName
        {
            get
            {
                var zp = new ZipPackage(InputPackageFile);
                return $"{zp.Id}-{zp.Version}-full.nupkg";
            }
        }

        public override string ToString()
        {
            return new { InputPackageFile, ReleasePackageFile, Version }.ToString();
        }

        public string CreateReleasePackage(string outputFile, string packagesRootDir = null, Func<string, string> releaseNotesProcessor = null,
            Action<string> contentsPostProcessHook = null)
        {
            Guard.ArgumentIsTrue(!string.IsNullOrEmpty(outputFile), "!string.IsNullOrEmpty(outputFile)");
            releaseNotesProcessor ??= (x => new Markdown().Transform(x));

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

        private static void ExtractDependentPackages(IEnumerable<IPackage> dependencies, DirectoryInfo tempPath, FrameworkName framework)
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

        private static void RemoveDeveloperDocumentation(DirectoryInfo expandedRepoPath)
        {
            expandedRepoPath.GetAllFilesRecursively()
                .Where(x => x.Name.EndsWith(".dll", true, CultureInfo.InvariantCulture))
                .Select(x => new FileInfo(x.FullName.ToLowerInvariant().Replace(".dll", ".xml")))
                .Where(x => x.Exists)
                .ForEach(x => x.Delete());
        }

        private static void RenderReleaseNotesMarkdown(string specPath, Func<string, string> releaseNotesProcessor)
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

        private static void RemoveDependenciesFromPackageSpec(string specPath)
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

        private IEnumerable<IPackage> FindAllDependentPackages(
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
}