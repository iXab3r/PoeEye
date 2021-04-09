using System;
using System.Reactive.Subjects;
using Moq.Language.Flow;

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
    }
}