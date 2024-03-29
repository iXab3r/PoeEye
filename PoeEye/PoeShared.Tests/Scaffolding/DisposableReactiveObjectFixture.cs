using NUnit.Framework;
using AutoFixture;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using PoeShared.Bindings;
using PoeShared.Scaffolding;
using PoeShared.Tests.Helpers;
using PropertyBinder;
using ReactiveUI;
using Shouldly;

namespace PoeShared.Tests.Scaffolding;

[TestFixture]
public class DisposableReactiveObjectFixture
{
    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    private Fixture fixture;

    [Test]
    public void ShouldCreate()
    {
        // Given
        // When 
        Action action = () => CreateInstance();

        // Then
        action.ShouldNotThrow();
    }

    [Test]
    public void ShouldSubscribeToPropertyChange()
    {
        //Given
        var instance = CreateInstance();

        var values = new List<int>();
        using var anchor = instance.WhenAnyValue(x => x.IntProperty).Subscribe(x => values.Add(x));

        //When
        instance.IntProperty = 1;

        //Then
        values.ShouldBe(new[] { 0, 1 });
    }

    [Test]
    public void ShouldSubscribeToInnerClassPropertyChange()
    {
        //Given
        var instance = CreateInstance();
        instance.InnerValue = CreateInstance();

        var values = new List<int>();
        using var anchor = instance.WhenAnyValue(x => x.InnerValue.IntProperty).Subscribe(x => values.Add(x));

        //When
        instance.InnerValue.IntProperty = 1;

        //Then
        values.ShouldBe(new[] { 0, 1 });
    }

    [Test]
    public void ShouldSubscribeToInnerClassIfSetAfterSubscriptionOnPropertyChange()
    {
        //Given
        var instance = CreateInstance();

        var values = new List<int>();
        using var anchor = instance.WhenAnyValue(x => x.InnerValue.IntProperty).Subscribe(x => values.Add(x));

        //When
        var innerValue = CreateInstance();
        innerValue.IntProperty = 1;
        instance.InnerValue = innerValue;
        innerValue.IntProperty = 2;

        //Then
        values.ShouldBe(new[] { 1, 2 });
    }

    [Test]
    public void ShouldSubscribeToInnerClassIfNullOnPropertyChange()
    {
        // https://github.com/reactiveui/ReactiveUI/issues/976 Bindings with property chains not updating target when chain is broken
        // By design, WhenAny does not propagate null if part of chain is null, is simply ignores it until new instance becomes available
            
        //Given
        var instance = CreateInstance();
        instance.InnerValue = CreateInstance();

        var values = new List<int?>();
        using var anchor = instance.WhenAnyValue(x => x.InnerValue.IntProperty).Subscribe(x => values.Add(x));
        instance.InnerValue.IntProperty = 1;

        //When
        instance.InnerValue = default;

        //Then
        values.ShouldBe(new int?[] { 0, 1 });
    }

    [Test]
    public void ShouldCreateCustomBinder()
    {
        //Given
        var object1 = new TestStub();

        var sourceExprText = @"x => x.IntProperty";
        var sourceParameter = Expression.Parameter(typeof(TestStub), "x");
        var sourceBinderExpr = System.Linq.Dynamic.Core.DynamicExpressionParser.ParseLambda<TestStub, int>(ParsingConfig.Default, false, sourceExprText, sourceParameter);
            
        var targetExprText = @"x => x.OtherIntProperty";
        var targetParameter = Expression.Parameter(typeof(TestStub), "x");
        var targetBinderExpr = System.Linq.Dynamic.Core.DynamicExpressionParser.ParseLambda<TestStub, int>(ParsingConfig.Default, false, targetExprText, targetParameter);
            
        var binder = new Binder<TestStub>();
        binder.Bind(sourceBinderExpr).To(targetBinderExpr);
            
        //When
        using var binderAnchor = binder.Attach(object1);
        object1.IntProperty = 1;

        //Then
        object1.OtherIntProperty.ShouldBe(1);
    }
    
    [Test]
    public void ShouldCreateCustomBinderUsingExpressionParser()
    {
        //Given
        var object1 = new TestStub();

        var sourceExprText = @"x => x.IntProperty";
        var sourceBinderExpr = CsharpExpressionParser.Instance.ParseFunction<TestStub, int>(sourceExprText);
            
        var targetExprText = @"x => x.OtherIntProperty";
        var targetBinderExpr = CsharpExpressionParser.Instance.ParseFunction<TestStub, int>(targetExprText);
            
        var binder = new Binder<TestStub>();
        binder.Bind(sourceBinderExpr).To(targetBinderExpr);
            
        //When
        using var binderAnchor = binder.Attach(object1);
        object1.IntProperty = 1;

        //Then
        object1.OtherIntProperty.ShouldBe(1);
    }

    [Test]
    public void ShouldBindOnDifferentObjects()
    {
        //Given
        var object1 = new TestStub();
        var object2 = new OtherStub();

        var sourceExprText = @"x => x.IntProperty";
        var sourceParameter = Expression.Parameter(typeof(TestStub), "x");
        var sourceBinderExpr = System.Linq.Dynamic.Core.DynamicExpressionParser.ParseLambda<TestStub, int>(ParsingConfig.Default, false, sourceExprText, sourceParameter);
            
        var binder = new Binder<TestStub>();
        binder.Bind(sourceBinderExpr).To((x,v) => object2.DoubleProperty = v);
            
        //When
        using var binderAnchor = binder.Attach(object1);
        object1.IntProperty = 1;

        //Then
        object2.DoubleProperty.ShouldBe(1);
    }

    [Test]
    public void ShouldListenWhenDisposed()
    {
        //Given
        var instance = CreateInstance();
        var listener = instance.ListenWhenDisposed().Listen();
        listener.ShouldBeEmpty();
        
        //When
        instance.Dispose();

        //Then
        listener.CollectionShouldBe(Unit.Default);
    }
    
    [Test]
    public void ShouldListenWhenDisposedForAlreadyDisposedObject()
    {
        //Given
        var instance = CreateInstance();
        instance.Dispose();
        
        //When
        var listener = instance.ListenWhenDisposed().Listen();

        //Then
        listener.CollectionShouldBe(Unit.Default);
    }

    [Test]
    [Repeat(10000)]
    public void ShouldListenWhenDisposedMultithread()
    {
        //Given
        var instance = CreateInstance();
        instance.Dispose();

        var listener = new ConcurrentQueue<Unit>();
        var startEvent = new ManualResetEvent(false);
        var listenerTask = Task.Factory.StartNew(() =>
        {
            startEvent.WaitOne();
            instance.ListenWhenDisposed().Subscribe(x => listener.Enqueue(x));
        });
        
        var disposerTask = Task.Factory.StartNew(() =>
        {
            startEvent.WaitOne();
            instance.Dispose();
        });

        //When
        startEvent.Set();
        Task.WaitAll(listenerTask, disposerTask);

        //Then
        listener.CollectionShouldBe(Unit.Default);
    }

    private TestStub CreateInstance()
    {
        return fixture.Build<TestStub>().OmitAutoProperties().Create();
    }

    private sealed class OtherStub : DisposableReactiveObject
    {

        public int IntProperty { get; set; }
            
        public double DoubleProperty { get; set; }
    }

    private sealed class TestStub : DisposableReactiveObject
    {

        public int IntProperty { get; set; }
            
        public int OtherIntProperty { get; set; }

        public string StringProperty { get; set; }

        public TestStub InnerValue { get; set; }
    }
}