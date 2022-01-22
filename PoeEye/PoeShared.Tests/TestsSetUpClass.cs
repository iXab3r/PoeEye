using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.Logging;
using NUnit.Framework;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Wpf.Scaffolding;
using PropertyBinder;
using PropertyBinder.Diagnostics;

namespace PoeShared.Tests;

[SetUpFixture]
public class TestsSetUpClass
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        SharedLog.Instance.LoadLogConfiguration(new FileInfo("log4net.tests.config"));
        Binder.SetTracer(new BinderLogger());
        Binder.DebugMode = true;

        SharedLog.Instance.Errors.Subscribe(
            ex =>
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw ex;
            });
    }

    private sealed class BinderLogger : IBindingTracer
    {
        private readonly IFluentLog log = typeof(TestsSetUpClass).PrepareLogger();
            
        public BinderLogger()
        {
        }

        public void OnScheduled(string bindingDescription)
        {
            log.WithPrefix(nameof(OnScheduled)).Debug(bindingDescription);
        }

        public void OnIgnored(string bindingDescription)
        {
            log.WithPrefix(nameof(OnIgnored)).Debug(bindingDescription);
        }

        public void OnStarted(string bindingDescription)
        {
            log.WithPrefix(nameof(OnStarted)).Debug(bindingDescription);
        }

        public void OnEnded(string bindingDescription)
        {
            log.WithPrefix(nameof(OnEnded)).Debug(bindingDescription);
        }

        public void OnException(Exception ex)
        {
            log.WithPrefix(nameof(OnException)).Error("Binding error", ex);
        }
    }
}