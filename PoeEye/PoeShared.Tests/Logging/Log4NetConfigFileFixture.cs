using System;
using System.IO;
using AutoFixture;
using log4net.Core;
using PoeShared.Logging;

namespace PoeShared.Tests.Logging;

[TestFixture]
internal class Log4NetConfigFileFixtureTests : FixtureBase
{
    [Test]
    public void ShouldCreate()
    {
        //Given

        //When
        Action action = () => CreateInstance();

        //Then
        action.ShouldNotThrow();
    }

    [Test]
    [TestCase( "Logging\\Samples\\debug.config", FluentLogLevel.Debug)]
    [TestCase( "Logging\\Samples\\warn.config", FluentLogLevel.Warn)]
    public void ShouldLoad(string filePath, FluentLogLevel expected)
    {
        //Given
        var config = new Log4NetConfigFile();
        config.Load(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath)));

        //When

        //Then
        config.Level.ShouldBe(expected.ToLog4NetLevel());
    }

    [Test]
    public void ShouldSave()
    {
        //Given
        var config = new Log4NetConfigFile();
        config.Load(new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logging\\Samples\\warn.config")));
        config.Level = Level.Critical;

        //When
        var newConfigPath = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logging\\Results\\debug.config"));
        config.SaveTo(newConfigPath);

        var newConfig = new Log4NetConfigFile();
        newConfig.Load(newConfigPath);

        //Then
        newConfig.Level.ShouldBe(Level.Critical);
    }

    private Log4NetConfigFile CreateInstance()
    {
        return Container.Create<Log4NetConfigFile>();
    }
}