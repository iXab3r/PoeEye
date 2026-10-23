using System.Linq;
using Moq;
using NUnit.Framework;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using PoeShared.Squirrel.Updater;
using Shouldly;

namespace PoeShared.Tests.Squirrel.Core;

public sealed class ApplicationUpdateInstallationServiceFixture
{
    /// <summary>
    /// WHAT: The updater's successor argument transport preserves original profile/path/JSON token
    /// boundaries, including a trailing directory separator and an empty argument, while omitting the startup flag.
    /// Regression: the old token-value filter removed the supported profile name autostart and script arguments
    /// after --. These values must survive independently of the actual autostart option before the delimiter.
    /// HOW: Capture normal application argument tokens and decode the protected launcher transport through
    /// the same decoder used by AppArguments at process startup; also retain the legacy plain GH payload.
    /// This covers the transport boundary; the real successor lifecycle remains covered by the product Apply cohort.
    /// </summary>
    [TestCase("release profile")]
    [TestCase("autostart")]
    [Category("Integration")]
    public void ShouldPreserveSuccessorTokensThroughApplicationStartupDecoder(string profile)
    {
        // Given
        var original = new[]
        {
            "--AUTOSTART", "--profile", profile, "--dataFolder", @"C:\App Data\",
            "--run", @"C:\My Scripts\update.csx", "--",
            "{\"message\":\"hello world\",\"path\":\"C:\\\\App Data\\\\\"}", "", "autostart", "--autostart"
        };
        var app = Mock.Of<IAppArguments>(x => x.CommandLineArguments == original &&
            x.AutostartFlag == "--autostart" && x.StartupArgs == string.Join(" ", original));

        // When
        var transport = ApplicationUpdateInstallationService.CaptureRestartArguments(app);
        var restarted = AppArguments.DecodeCommandLineArguments(new[] { transport });

        // Then
        restarted.ShouldBe(original.Skip(1).ToArray());

        // When: a previous launcher supplies its existing plain command-line GH representation.
        var legacy = AppArguments.DecodeCommandLineArguments(new[] { StringUtils.ToHexGzip("--profile release --no-activate") });

        // Then
        legacy.ShouldBe(new[] { "--profile", "release", "--no-activate" });
    }
}
