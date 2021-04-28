using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using Moq;
using Moq.Language.Flow;
using PoeShared.Scaffolding;

namespace PoeShared.Tests.Helpers
{
    public static class MockExtensions
    {
        public static ISubject<TProperty> ReturnsPublisher<T, TProperty>(this ISetupGetter<T, IObservable<TProperty>> getter) 
            where T : class
        {
            var result = new Subject<TProperty>();
            getter.Returns(result);    
            return result;
        }
        
        public static void SetupGetAndNotify<T, TProperty>(this Mock<T> mock, Expression<Func<T, TProperty>> expression, TProperty value)
            where T : class, INotifyPropertyChanged
        {
            var propertyName = expression.GetMemberName();
            mock.SetupGet(expression).Returns(value);
            mock.Raise(x => x.PropertyChanged -= null, new PropertyChangedEventArgs(propertyName));
        }
    }
}