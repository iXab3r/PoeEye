using System.ComponentModel;
using Moq;
using NUnit.Framework;
using PoeEye.Tests.Helpers;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.Tests.PoeShared.Scaffolding
{
    [TestFixture]
    public class ReactiveObjectExtensionsFixture
    {
        [Test]
        public void ShouldBindProperty()
        {
            //Given
            var target = new Mock<ITestReactiveTarget>();
            var source = new Mock<ITestReactiveSource>();

            var anchor = ReactiveObjectExtensions.BindPropertyTo(target.Object, x => x.TargetValue, source.Object, x => x.SourceValue);

            //When
            source.RaisePropertyChanged(x => x.SourceValue);

            //Then
            target.Verify(x => x.RaisePropertyChanged(It.Is<PropertyChangedEventArgs>(y => y.PropertyName == nameof(ITestReactiveTarget.TargetValue))), Times.Once);
        }

        public interface ITestReactiveSource : IReactiveObject
        {
            int SourceValue { get; set; }
        }

        public interface ITestReactiveTarget : IReactiveObject
        {
            string TargetValue { get; set; }
        }
    }
}