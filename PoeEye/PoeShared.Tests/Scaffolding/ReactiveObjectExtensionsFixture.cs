using System.ComponentModel;
using Moq;
using NUnit.Framework;
using PoeShared.Scaffolding;

namespace PoeShared.Tests.Scaffolding
{
    [TestFixture]
    public class ReactiveObjectExtensionsFixture
    {
        public interface ITestReactiveSource : IDisposableReactiveObject
        {
            int SourceValue { get; set; }
        }

        public interface ITestReactiveTarget : IDisposableReactiveObject
        {
            string TargetValue { get; set; }
        }

        [Test]
        public void ShouldBindProperty()
        {
            //Given
            var target = new Mock<ITestReactiveTarget>();
            var source = new Mock<ITestReactiveSource>();

            var anchor = target.Object.RaiseWhenSourceValue(x => x.TargetValue, source.Object, x => x.SourceValue);

            //When
            source.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(ITestReactiveSource.SourceValue)));

            //Then
            target.Verify(x => x.RaisePropertyChanged(nameof(ITestReactiveTarget.TargetValue)),
                Times.Once);
        }
    }
}