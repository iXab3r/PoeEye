using NUnit.Framework;

namespace PoeShared.Tests.Helpers
{
    public class NamedTestCaseData : TestCaseData
    {
        public NamedTestCaseData(params object[] args) : base(args)
        {
        }

        public NamedTestCaseData(object arg) : base(arg)
        {
        }

        public NamedTestCaseData(object arg1, object arg2) : base(arg1, arg2)
        {
        }

        public NamedTestCaseData(object arg1, object arg2, object arg3) : base(arg1, arg2, arg3)
        {
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(TestName) ? base.ToString() : $"{TestName}";
        }
    }
}