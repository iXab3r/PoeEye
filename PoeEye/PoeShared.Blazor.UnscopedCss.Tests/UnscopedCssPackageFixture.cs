using System.Diagnostics;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;

namespace PoeShared.Blazor.UnscopedCss.Tests;

[TestFixture]
[NonParallelizable]
public sealed class UnscopedCssPackageFixture
{
    private static readonly TimeSpan BuildTimeout = TimeSpan.FromMinutes(5);

    private string repoRoot = null!;
    private string packageProjectPath = null!;
    private string packageOutputDir = null!;
    private string packageVersion = null!;
    private string dotnetExe = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("These integration tests exercise the Windows build toolchain.");
        }

        repoRoot = FindRepoRoot();
        packageProjectPath = Path.Combine(repoRoot, "Sources", "PoeShared.Blazor.UnscopedCss", "PoeShared.Blazor.UnscopedCss.csproj");
        packageOutputDir = Path.Combine(repoRoot, "Sources", "PoeShared.Blazor.UnscopedCss", "bin", "Debug");
        packageVersion = ReadPackageVersion(packageProjectPath);
        dotnetExe = FindDotNetExe();

        RunProcess(dotnetExe, $"restore \"{packageProjectPath}\"", repoRoot);
        RunProcess(dotnetExe, $"pack \"{packageProjectPath}\" --no-restore -c Debug", repoRoot);
    }

    [Test]
    public void PackedPackage_ShouldContainDesktopAndCoreTaskAssemblies()
    {
        var packagePath = Path.Combine(packageOutputDir, $"PoeShared.Blazor.UnscopedCss.{packageVersion}.nupkg");
        File.Exists(packagePath).ShouldBeTrue($"Expected packed package at {packagePath}");

        using var archive = System.IO.Compression.ZipFile.OpenRead(packagePath);
        var entries = archive.Entries.Select(x => x.FullName).ToArray();

        entries.ShouldContain("buildTransitive/PoeShared.Blazor.UnscopedCss.props");
        entries.ShouldContain("buildTransitive/PoeShared.Blazor.UnscopedCss.targets");
        entries.ShouldContain("tasks/net472/PoeShared.Blazor.UnscopedCss.dll");
        entries.ShouldContain("tasks/net8.0/PoeShared.Blazor.UnscopedCss.dll");
    }

    [Test]
    public void DotNetBuild_ShouldUnscopeOnlyFlaggedFiles()
    {
        using var sandbox = new TemporaryDirectory("PoeShared.Blazor.UnscopedCss.Tests");
        var projectDir = CreateConsumerProject(sandbox.Path, "DotNetConsumer");
        RestoreConsumerProject(projectDir, "DotNetConsumer.csproj");

        var buildResult = RunProcess(
            dotnetExe,
            $"build \"{Path.Combine(projectDir, "DotNetConsumer.csproj")}\" --no-restore -v:minimal /p:PoeBlazorUnscopedCssVerbose=true",
            projectDir,
            extraEnvironment: new Dictionary<string, string?>
            {
                ["NUGET_PACKAGES"] = Path.Combine(projectDir, ".packages")
            });

        buildResult.Output.ShouldContain("Processed 1 flagged scoped CSS file(s), rewrote 1.");
        AssertSelectiveUnscoping(projectDir, "DotNetConsumer");
    }

    [Test]
    public void DesktopMsBuild_ShouldLoadDesktopTaskAndUnscopeOnlyFlaggedFiles()
    {
        var msbuildExe = TryFindDesktopMsBuildExe();
        if (msbuildExe == null)
        {
            Assert.Ignore("Visual Studio MSBuild.exe was not found on this machine.");
        }

        using var sandbox = new TemporaryDirectory("PoeShared.Blazor.UnscopedCss.Tests");
        var projectDir = CreateConsumerProject(sandbox.Path, "DesktopConsumer");
        RestoreConsumerProject(projectDir, "DesktopConsumer.csproj");

        var buildResult = RunProcess(
            msbuildExe,
            $"\"{Path.Combine(projectDir, "DesktopConsumer.csproj")}\" /t:Build /p:PoeBlazorUnscopedCssVerbose=true /v:minimal",
            projectDir,
            extraEnvironment: new Dictionary<string, string?>
            {
                ["NUGET_PACKAGES"] = Path.Combine(projectDir, ".packages")
            });

        buildResult.Output.ShouldContain("Processed 1 flagged scoped CSS file(s), rewrote 1.");
        AssertSelectiveUnscoping(projectDir, "DesktopConsumer");
    }

    private void AssertSelectiveUnscoping(string projectDir, string projectName)
    {
        var objDir = Path.Combine(projectDir, "obj", "Debug", "net8.0", "scopedcss");
        var flaggedCssPath = Path.Combine(objDir, "Component1.razor.rz.scp.css");
        var scopedCssPath = Path.Combine(objDir, "Component2.razor.rz.scp.css");
        var bundlePath = Path.Combine(objDir, "bundle", $"{projectName}.styles.css");

        File.Exists(flaggedCssPath).ShouldBeTrue($"Expected generated flagged CSS at {flaggedCssPath}");
        File.Exists(scopedCssPath).ShouldBeTrue($"Expected generated scoped CSS at {scopedCssPath}");
        File.Exists(bundlePath).ShouldBeTrue($"Expected final bundle at {bundlePath}");

        var flaggedCss = File.ReadAllText(flaggedCssPath, Encoding.UTF8);
        var scopedCss = File.ReadAllText(scopedCssPath, Encoding.UTF8);
        var bundleCss = File.ReadAllText(bundlePath, Encoding.UTF8);

        flaggedCss.ShouldContain(".event-log-row:nth-child(even)");
        flaggedCss.ShouldContain(".event-log-message-text");
        flaggedCss.ShouldNotContain("[poe-smoke-scope]");
        Regex.IsMatch(flaggedCss, @"\[b-[A-Za-z0-9]+\]").ShouldBeFalse("Flagged CSS should not keep Blazor scope selectors.");

        Regex.IsMatch(scopedCss, @"\.still-scoped\[b-[A-Za-z0-9]+\]").ShouldBeTrue("Unflagged CSS should remain scoped.");

        bundleCss.ShouldContain("Component1.razor.rz.scp.css");
        bundleCss.ShouldContain(".event-log-row:nth-child(even)");
        bundleCss.ShouldContain(".event-log-message-text");
        bundleCss.ShouldContain("Component2.razor.rz.scp.css");
        Regex.IsMatch(bundleCss, @"\.still-scoped\[b-[A-Za-z0-9]+\]").ShouldBeTrue("Bundle should keep normal scoping for unflagged files.");
        bundleCss.ShouldNotContain("[poe-smoke-scope]");
    }

    private string CreateConsumerProject(string sandboxPath, string projectName)
    {
        var projectDir = Path.Combine(sandboxPath, projectName);
        Directory.CreateDirectory(projectDir);

        // The temp consumer uses a dedicated packages folder so restore always picks up the freshly packed local package.
        File.WriteAllText(
            Path.Combine(projectDir, $"{projectName}.csproj"),
            $$"""
            <Project Sdk="Microsoft.NET.Sdk.Razor">

              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <RestoreAdditionalProjectSources>{{EscapeXml(packageOutputDir)}}</RestoreAdditionalProjectSources>
                <RestorePackagesPath>{{EscapeXml(Path.Combine(projectDir, ".packages"))}}</RestorePackagesPath>
              </PropertyGroup>

              <ItemGroup>
                <SupportedPlatform Include="browser" />
              </ItemGroup>

              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.0.24" />
                <PackageReference Include="PoeShared.Blazor.UnscopedCss" Version="{{EscapeXml(packageVersion)}}" PrivateAssets="all" />
              </ItemGroup>

              <ItemGroup>
                <None Update="Component1.razor.css"
                      CssScope="poe-smoke-scope"
                      PoeUnscopeAfterRewrite="true" />
              </ItemGroup>

            </Project>
            """,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        File.WriteAllText(
            Path.Combine(projectDir, "Component1.razor"),
            """
            <div class="event-log-row">
                <p class="event-log-message-text">Flagged component</p>
            </div>
            """,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        File.WriteAllText(
            Path.Combine(projectDir, "Component1.razor.css"),
            """
            ::deep .event-log-row:nth-child(even) {
                background-color: rgba(0, 0, 0, 0.20);
            }

            ::deep .event-log-message-text {
                white-space: pre-wrap;
            }
            """,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        File.WriteAllText(
            Path.Combine(projectDir, "Component2.razor"),
            """
            <div class="still-scoped">
                Unflagged component
            </div>
            """,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        File.WriteAllText(
            Path.Combine(projectDir, "Component2.razor.css"),
            """
            .still-scoped {
                color: green;
            }
            """,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return projectDir;
    }

    private void RestoreConsumerProject(string projectDir, string projectFileName)
    {
        RunProcess(
            dotnetExe,
            $"restore \"{Path.Combine(projectDir, projectFileName)}\"",
            projectDir,
            extraEnvironment: new Dictionary<string, string?>
            {
                ["NUGET_PACKAGES"] = Path.Combine(projectDir, ".packages")
            });
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sources", "EyeAuras.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the EyeAuras repository root from the test output directory.");
    }

    private static string ReadPackageVersion(string csprojPath)
    {
        var document = XDocument.Load(csprojPath);
        var version = document.Root?
            .Elements("PropertyGroup")
            .Elements("Version")
            .Select(x => x.Value)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException($"Could not determine package version from {csprojPath}");
        }

        return version;
    }

    private static string FindDotNetExe()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet", "dotnet.exe")
        };

        return candidates.FirstOrDefault(File.Exists)
               ?? throw new FileNotFoundException("Could not locate dotnet.exe in the standard Program Files locations.");
    }

    private static string? TryFindDesktopMsBuildExe()
    {
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio", "2022");
        if (!Directory.Exists(baseDir))
        {
            return null;
        }

        var editions = new[] { "Enterprise", "Professional", "Community", "BuildTools" };
        foreach (var edition in editions)
        {
            var candidate = Path.Combine(baseDir, edition, "MSBuild", "Current", "Bin", "amd64", "MSBuild.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static ProcessResult RunProcess(
        string fileName,
        string arguments,
        string workingDirectory,
        IDictionary<string, string?>? extraEnvironment = null)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (extraEnvironment != null)
        {
            foreach (var kvp in extraEnvironment)
            {
                process.StartInfo.Environment[kvp.Key] = kvp.Value;
            }
        }

        var outputBuilder = new StringBuilder();
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                outputBuilder.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                outputBuilder.AppendLine(args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit((int)BuildTimeout.TotalMilliseconds))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort timeout cleanup.
            }

            throw new TimeoutException($"Process '{fileName} {arguments}' timed out after {BuildTimeout}.");
        }

        var output = outputBuilder.ToString();
        process.ExitCode.ShouldBe(
            0,
            $"Command failed: {fileName} {arguments}{Environment.NewLine}{output}");

        return new ProcessResult(process.ExitCode, output);
    }

    private static string EscapeXml(string value) => SecurityElement.Escape(value) ?? value;

    private readonly record struct ProcessResult(int ExitCode, string Output);

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory(string prefix)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (!Directory.Exists(Path))
            {
                return;
            }

            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // Keep the sandbox around for debugging if Windows still has file handles open.
            }
        }
    }
}
