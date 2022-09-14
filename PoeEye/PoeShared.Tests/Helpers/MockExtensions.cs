using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using AutoFixture;
using Moq;
using Moq.Language.Flow;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;

namespace PoeShared.Tests.Helpers;

public static class FixtureExtensions
{
    public static void RegisterFactory<T>(
        this Fixture container,
        Func<T> itemFactory)
    {
        var mock = new Mock<IFactory<T>>();
        mock.Setup(x => x.Create()).Returns(itemFactory);
        container.Register(() => mock.Object);
    }
    
    public static void RegisterFactory<T, TInput>(
        this Fixture container,
        Func<TInput, T> itemFactory)
    {
        var mock = new Mock<IFactory<T, TInput>>();
        mock.Setup(x => x.Create(It.IsAny<TInput>())).Returns(itemFactory);
        container.Register(() => mock.Object);
    }
    
    public static void RegisterFactory<T, TInput1, TInput2>(
        this Fixture container,
        Func<TInput1, TInput2, T> itemFactory)
    {
        var mock = new Mock<IFactory<T, TInput1, TInput2>>();
        mock.Setup(x => x.Create(It.IsAny<TInput1>(), It.IsAny<TInput2>())).Returns(itemFactory);
        container.Register(() => mock.Object);
    }
    
    public static Mock<IConfigProvider<TConfig>> RegisterConfigProvider<TConfig>(
        this Fixture container,
        TConfig config) 
        where TConfig : IPoeEyeConfig
    {
        var configProvider = new Mock<IConfigProvider<TConfig>>();
        configProvider
            .SetupGet(x => x.ActualConfig)
            .Returns(config);
        container.Register(() => configProvider.Object);
        return configProvider;
    }

    public static Mock<T> RegisterMock<T>(this Fixture container) where T : class
    {
        var mock = new Mock<T>();
        container.Register(() => mock.Object);
        return mock;
    }
}

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