using System;
using Fody;
using Moq;
using NUnit.Framework;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using Shouldly;

// ReSharper disable PossibleNullReferenceException

namespace PoeShared.Fody.Tests;


/*
 * Unfortunately, the attempts to wrap logging method into IFs has failed - it worked for most of the cases
 * but there we some corner cases which I was not able to cover, basically, any operation changing the state of stack
 * made it very complex to determine exact invocation instructions. Reconstructing the stack is definitely possible, so maybe in v2
 *
 * Right now I thing the better way maybe to use Roslyn and direct source code manipulation before build
 */

[TestFixture]
[Ignore("Not implemented")]
public class WeaverTests : FixtureBase
{
    [TearDown]
    public void Dispose()
    {
        TestResult.Messages.ForEach(x => Log.Debug($"[{x.MessageImportance}] {x.Text}"));
    }
#pragma warning disable CS0618 // Type or member is obsolete
    private static readonly TestResult TestResult;
#pragma warning restore CS0618 // Type or member is obsolete
    private static readonly Type TestType;
    private Mock<IFluentLog> mockLogger;

    static WeaverTests()
    {
        var weavingTask = new FluentLogIsEnabled();
        TestResult = weavingTask.ExecuteTestRun("PoeShared.Fody.TestAssembly.dll", false);
        TestType = TestResult.Assembly.GetType("LoggerIsEnabledScenarios");
    }

    protected override void OneTimeSetUp()
    {
        base.OneTimeSetUp();
        Log.Debug($"Assembly: {TestResult.AssemblyPath}");
    }

    protected override void SetUp()
    {
        base.SetUp();
        mockLogger = new Mock<IFluentLog>();
    }

    [Test]
    public void Verify()
    {
        var weavingTask = new FluentLogIsEnabled();
        weavingTask.ExecuteTestRun("PoeShared.Fody.TestAssembly.dll", true);
    }
    
    [Test]
    public void Original_LogDebug_IsEnabledTrue()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = new LoggerIsEnabledScenarios(mockLogger.Object);
        instance.LogDebug();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Never);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_IsEnabledTrue()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_IsEnabledFalse()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(false);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void LogDebugViaGetLogger_IsEnabledTrue()
    {
        //Not supported yet, thus IsDebugEnabled does not affect results in any way
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugViaGetLogger();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Never);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebugViaGetLogger_IsEnabledFalse()
    {
        //Not supported yet, thus IsDebugEnabled does not affect results in any way
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(false);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugViaGetLogger();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Never);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }
    
    [Test]
    public void LogDebugViaStaticLogger_IsEnabledTrue()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        Action action = () => instance.LogDebugViaStaticLogger();
        action.ShouldThrow<NullReferenceException>();
    }

    [Test]
    public void LogDebugViaStaticLogger_IsEnabledFalse()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(false);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        Action action = () => instance.LogDebugViaStaticLogger();
        action.ShouldThrow<NullReferenceException>();
    }
    
    [Theory]
    public void LogDebugFormatted(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugFormatted();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), isEnabled ? Times.Once : Times.Never);
    } 
    
    [Theory]
    public void LogDebugFormattedSelf(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugFormattedSelf();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Exactly(isEnabled ? 2 : 0));
        mockLogger.VerifyGet(x => x.IsInfoEnabled, Times.Exactly(isEnabled ? 1 : 0));
        mockLogger.VerifyGet(x => x.IsWarnEnabled, Times.Exactly(isEnabled ? 1 : 0));
        mockLogger.VerifyGet(x => x.IsErrorEnabled, Times.Exactly(isEnabled ? 1 : 0));
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Exactly(isEnabled ? 1 : 0));
    } 
   
    [Theory]
    public void LogDebugWithException(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugWithException();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>(), It.IsAny<Exception>()), isEnabled ? Times.Once : Times.Never);
    } 
    
    [Theory]
    public void LogDebugFormattedWithIntCondition(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugFormattedWithIntCondition();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), isEnabled ? Times.Once : Times.Never);
    } 
    
    [Theory]
    public void LogDebugFormattedWithLogLevelCondition(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugFormattedWithLogLevelCondition();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), isEnabled ? Times.Once : Times.Never);
    }
    
    [Theory]
    public void LogDebugFormattedOneArg(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugFormattedOneArg();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), isEnabled ? Times.Once : Times.Never);
    } 
    
    
    [Theory]
    public void LogDebugFormattedTwoArgs(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugFormattedTwoArgs();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), isEnabled ? Times.Once : Times.Never);
    }  
    
    [Theory]
    public void LogDebugFormattedOneArgWithException(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugFormattedOneArgWithException();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>(), It.IsAny<Exception>()), isEnabled ? Times.Once : Times.Never);
    }  
    
    [Theory]
    public void LogDebugViaVariable(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugViaVariable();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), isEnabled ? Times.Once : Times.Never);
    } 
    
    [Theory]
    public void LogDebugWithIf(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugWithIf();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), isEnabled ? Times.Once : Times.Never);
    } 
    
    [Theory]
    public void LogDebugWithStoreToField(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugWithStoreToField();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), isEnabled ? Times.Once : Times.Never);
    }  
    
    [Theory]
    public void LogDebugWithStoreToFieldAndRestore(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugWithStoreToFieldAndRestore();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), isEnabled ? Times.Once : Times.Never);
    } 
    
    [Theory]
    public void LogDebugWithExceptionViaVariable(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugWithExceptionViaVariable();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>(), It.IsAny<Exception>()), isEnabled ? Times.Once : Times.Never);
    }  
    
    [Theory]
    public void LogDebugFormattedWithExceptionViaVariable(bool isEnabled)
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(isEnabled);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugFormattedWithExceptionViaVariable();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>(), It.IsAny<Exception>()), isEnabled ? Times.Once : Times.Never);
    } 

    [Test]
    public void LogInformation_IsEnabledTrue()
    {
        mockLogger.SetupGet(x => x.IsInfoEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogInformation();
        mockLogger.VerifyGet(x => x.IsInfoEnabled, Times.Once);
        mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogInformation_IsEnabledFalse()
    {
        mockLogger.SetupGet(x => x.IsInfoEnabled).Returns(false);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogInformation();
        mockLogger.VerifyGet(x => x.IsInfoEnabled, Times.Once);
        mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void LogWarning_IsEnabledTrue()
    {
        mockLogger.SetupGet(x => x.IsWarnEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogWarning();
        mockLogger.VerifyGet(x => x.IsWarnEnabled, Times.Once);
        mockLogger.Verify(x => x.Warn(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogWarning_IsEnabledFalse()
    {
        mockLogger.SetupGet(x => x.IsWarnEnabled).Returns(false);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogWarning();
        mockLogger.VerifyGet(x => x.IsWarnEnabled, Times.Once);
        mockLogger.Verify(x => x.Warn(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void LogError_IsEnabledTrue()
    {
        mockLogger.SetupGet(x => x.IsErrorEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogError();
        mockLogger.VerifyGet(x => x.IsErrorEnabled, Times.Once);
        mockLogger.Verify(x => x.Error(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogError_IsEnabledFalse()
    {
        mockLogger.SetupGet(x => x.IsErrorEnabled).Returns(false);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogError();
        mockLogger.VerifyGet(x => x.IsErrorEnabled, Times.Once);
        mockLogger.Verify(x => x.Error(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void LogDebugWithEnabled()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugWithEnabled();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebugWithEnabledProperty()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugWithEnabledProperty();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebugWithEnabled_With_Code_Before()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugWithEnabled_With_Code_Before();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebugWithEnabled_With_Code_After()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugWithEnabled_With_Code_After();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebugWithEnabled_With_Code_Before_And_After()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebugWithEnabled_With_Code_Before_And_After();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_With_Code_Before()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_With_Code_Before();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_With_Code_After()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_With_Code_After();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_With_Code_Before_And_After()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_With_Code_Before_And_After();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_Multiple()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_Multiple();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Exactly(2));
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Exactly(2));
    }

    [Test]
    public void LogDebug_Multiple_With_First_IsEnabled()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_Multiple_With_First_IsEnabled();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Exactly(2));
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Exactly(2));
    }

    [Test]
    public void LogDebug_Multiple_With_Second_IsEnabled()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_Multiple_With_Second_IsEnabled();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Exactly(2));
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Exactly(2));
    }

    [Test]
    public void LogDebug_Multiple_With_Code()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_Multiple_With_Code();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Exactly(2));
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Exactly(2));
    }

    [Test]
    public void LogDebug_In_Switch()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Switch();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Switch_With_Before_Code()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Switch_With_Before_Code();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Switch_With_After_Code()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Switch_With_After_Code();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Switch_With_Before_And_After_Code()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Switch_With_Before_And_After_Code();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Exception()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Exception();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Exception_With_Before_Code()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Exception_With_Before_Code();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Exception_With_After_Code()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Exception_With_After_Code();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Exception_With_Before_And_After_Code()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Exception_With_Before_And_After_Code();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Exception_With_Before_Code_In_Try()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Exception_With_Before_Code_In_Try();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Exception_With_After_Code_In_Try()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Exception_With_After_Code_In_Try();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Exception_With_Before_And_After_Code_In_Try()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Exception_With_Before_And_After_Code_In_Try();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public void LogDebug_In_Exception_Catch()
    {
        mockLogger.SetupGet(x => x.IsDebugEnabled).Returns(true);
        var instance = (dynamic) Activator.CreateInstance(TestType, mockLogger.Object);
        instance.LogDebug_In_Exception_Catch();
        mockLogger.VerifyGet(x => x.IsDebugEnabled, Times.Once);
        mockLogger.Verify(x => x.Debug(It.IsAny<string>()), Times.Once);
    }


    [Test]
    public void Interface_Decompiled()
    {
        VerifyDecompiled("ILoggerIsEnabledScenarios");
    }

    [Test]
    public void Abstract_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenariosAbstract");
    }

    [Test]
    public void AbstractImplementation_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenariosAbstractImplementation");
    }

    [Test]
    public void Enum_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledEnum");
    }

    [Test]
    public void LogInformation_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogInformation");
    }

    [Test]
    public void LogWarning_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogWarning");
    }

    [Test]
    public void LogError_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogError");
    }

    [Test]
    public void LogCritical_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogCritical");
    }
    
    [Test]
    public void LogDebugWithException_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugWithException");
    }
    
    [Test]
    public void LogDebugFormatted_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugFormatted");
    } 
    
    [Test]
    public void LogDebugFormattedWithIntCondition_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugFormattedWithIntCondition");
    }  
    
    [Test]
    public void LogDebugFormattedWithLogLevelCondition_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugFormattedWithLogLevelCondition");
    } 
    
    [Test]
    public void LogDebugFormattedSelf_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugFormattedSelf");
    }
    
    [Test]
    public void LogDebugFormattedOneArg_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugFormattedOneArg");
    }
    
    [Test]
    public void LogDebugFormattedTwoArgs_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugFormattedTwoArgs");
    }
    
    [Test]
    public void LogDebugFormattedOneArgWithException_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugFormattedOneArgWithException");
    }

    [Test]
    public void LogDebugWithEnabled_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugWithEnabled");
    }

    [Test]
    public void LogDebugWithEnabled_With_Code_Before_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugWithEnabled_With_Code_Before");
    }

    [Test]
    public void LogDebugWithEnabled_With_Code_After_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugWithEnabled_With_Code_After");
    }

    [Test]
    public void LogDebugWithEnabled_With_Code_Before_And_After_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebugWithEnabled_With_Code_Before_And_After");
    }

    [Test]
    public void LogDebug_With_Code_Before_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_With_Code_Before");
    }

    [Test]
    public void LogDebug_With_Code_After_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_With_Code_After");
    }

    [Test]
    public void LogDebug_With_Code_Before_And_After_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_With_Code_Before_And_After");
    }

    [Test]
    public void LogDebug_Multiple_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_Multiple");
    }

    [Test]
    public void LogDebug_Multiple_With_First_IsEnabled_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_Multiple_With_First_IsEnabled");
    }

    [Test]
    public void LogDebug_Multiple_With_Second_IsEnabled_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_Multiple_With_Second_IsEnabled");
    }

    [Test]
    public void LogDebug_Multiple_With_Code_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_Multiple_With_Code");
    }

    [Test]
    public void LogDebug_In_Switch_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Switch");
    }

    [Test]
    public void LogDebug_In_Switch_With_Before_Code_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Switch_With_Before_Code");
    }

    [Test]
    public void LogDebug_In_Switch_With_After_Code_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Switch_With_After_Code");
    }

    [Test]
    public void LogDebug_In_Switch_With_Before_And_After_Code_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Switch_With_Before_And_After_Code");
    }

    [Test]
    public void LogDebug_In_Exception_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Exception");
    }

    [Test]
    public void LogDebug_In_Exception_With_Before_Code_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Exception_With_Before_Code");
    }

    [Test]
    public void LogDebug_In_Exception_With_After_Code_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Exception_With_After_Code");
    }

    [Test]
    public void LogDebug_In_Exception_With_Before_And_After_Code_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Exception_With_Before_And_After_Code");
    }

    [Test]
    public void LogDebug_In_Exception_With_Before_Code_In_Try_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Exception_With_Before_Code_In_Try");
    }

    [Test]
    public void LogDebug_In_Exception_With_After_Code_In_Try_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Exception_With_After_Code_In_Try");
    }

    [Test]
    public void LogDebug_In_Exception_With_Before_And_After_Code_In_Try_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Exception_With_Before_And_After_Code_In_Try");
    }

    [Test]
    public void LogDebug_In_Exception_Catch_Decompiled()
    {
        VerifyDecompiled("LoggerIsEnabledScenarios::LogDebug_In_Exception_Catch");
    }

    private void VerifyDecompiled(string identifier)
    {
        var il = Ildasm.Decompile(TestResult.AssemblyPath, identifier);
        il.ShouldNotBeEmpty();
        Log.Debug($"Decompiled '{identifier}':\n{il}");
    }
}