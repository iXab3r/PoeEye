using Moq;
using NUnit.Framework;
using PoeShared.Logging;

namespace PoeShared.Tests.Logging;

[TestFixture]
public class FluentLogBuilderFixture
{
    private Mock<ILogWriter> logWriter;
        
    [SetUp]
    public void SetUp()
    {
        logWriter = new Mock<ILogWriter>();
        logWriter.SetupGet(x => x.IsDebugEnabled).Returns(true);
        logWriter.SetupGet(x => x.IsInfoEnabled).Returns(true);
        logWriter.SetupGet(x => x.IsWarnEnabled).Returns(true);
        logWriter.SetupGet(x => x.IsErrorEnabled).Returns(true);
    }
        
    [Test]
    public void ShouldLog()
    {
        //Given
        var instance = (IFluentLog)new FluentLogBuilder(logWriter.Object);

        //When
        instance.Info($"info");
        instance.Warn($"warn");

        //Then
        logWriter.Verify(x => x.WriteLog(It.Is<LogData>(y => y.ToString() == "info" &&  y.LogLevel == FluentLogLevel.Info)));
        logWriter.Verify(x => x.WriteLog(It.Is<LogData>(y => y.ToString() == "warn" && y.LogLevel == FluentLogLevel.Warn)));
    }

    [Test]
    public void ShouldBuildChain()
    {
        //Given
        var instance = new FluentLogBuilder(logWriter.Object).WithPrefix("test1").WithPrefix("test2");

        //When
        instance.Info($"info");


        //Then
        logWriter.Verify(x => x.WriteLog(It.Is<LogData>(y => y.ToString() == "[test1] [test2] info" &&  y.LogLevel == FluentLogLevel.Info)));
    }

    [Test]
    public void ShouldBuildTable()
    {
        //Given
        var items = new[] {"a", "b", "c"};
        var instance = new FluentLogBuilder(logWriter.Object).WithPrefix("test1").WithTable(items, ", ");

        //When
        instance.Info($"info");

        //Then
        var expectedMessage = @$"[test1] info, Items: 3, #1 a, #2 b, #3 c";
        logWriter.Verify(x => x.WriteLog(It.Is<LogData>(y => y.ToString() == expectedMessage &&  y.LogLevel == FluentLogLevel.Info)));
    }
}