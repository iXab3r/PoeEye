using NUnit.Framework;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PropertyBinder;
using ReactiveUI;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace PoeShared.Tests.PropertyBinder
{
    [TestFixture]
    public class BinderPerformanceModelFixture : BenchmarkBase
    {
        [Test]
        public void RunModelBenchmarks()
        {
            BenchmarkRunner.Run<BinderPerformanceModelFixture>(Config);
        }
        
        [Test]
        [TestCaseSource(nameof(GetModels))]
        public void ShouldCreate(BaseViewModel viewModel)
        {
            //Given
            //When

            //Then
            viewModel.Data.ShouldBe(3);
            viewModel.DataPlusOne.ShouldBe(4);
        }

        [Test]
        [TestCaseSource(nameof(GetModels))]
        public void ShouldTrack(BaseViewModel viewModel)
        {
            //Given
            //When
            viewModel.Data = 4;

            //Then
            viewModel.Data.ShouldBe(4);
            viewModel.DataPlusOne.ShouldBe(5);
        }
        
        [Test]
        [TestCaseSource(nameof(GetModels))]
        public void ShouldTrackNulls(BaseViewModel viewModel)
        {
            //Given
            //When
            viewModel.Data = null;

            //Then
            viewModel.Data.ShouldBe(null);
            viewModel.DataPlusOne.ShouldBe(99);
        }

        [Test]
        [TestCaseSource(nameof(GetModels))]
        public void ShouldSum(BaseViewModel viewModel)
        {
            //Given
            //When
            viewModel.Data = 1;
            viewModel.Data1 = 2;
            viewModel.Data2 = 3;
            viewModel.Data3 = 4;
            
            //Then
            viewModel.Sum.ShouldBe(10);
        }
        
        [ParamsSource(nameof(GetModels))]
        public BinderPerformanceModelFixture.BaseViewModel ViewModel { get; set; }
        
        [Benchmark(Description = "Create new")]
        public void ReactiveModelCreate()
        {
            var viewModel = new BinderPerformanceModelFixture.ReactiveViewModel();
        }
        
        [Benchmark(Description = "Create new")]
        public void BinderModelCreate()
        {
            var viewModel = new BinderPerformanceModelFixture.BinderViewModel();
        }
        
        [Benchmark(Description = "Assignment")]
        [Arguments(100)]
        public void BinderModelAssignment(int count)
        {
            for (int i = 0; i < count; i++)
            {
                ViewModel.Data = i;
            }
        }
        
        [Benchmark(Description = "Sum")]
        [Arguments(100)]
        public void BinderModelSumCalculation(int count)
        {
            for (int i = 0; i < count; i++)
            {
                ViewModel.Data = i;
                ViewModel.Data1 = i + 1;
                ViewModel.Data2 = i + 2;
                ViewModel.Data3 = i + 3;
            }
        }

        public static IEnumerable<BaseViewModel> GetModels()
        {
            yield return new BinderViewModel();
            yield return new ReactiveViewModel();
            yield return new PropertyHelperViewModel();
        }
        
        public abstract class BaseViewModel : DisposableReactiveObject
        {
            private int? data = 3;

            public int? Data
            {
                get => data;
                set => RaiseAndSetIfChanged(ref data, value);
            }

            public abstract int? DataPlusOne { get; protected set; }

            public abstract int? Sum { get; protected set; }

            public int? Data1 { get; set; }

            public int? Data2 { get; set; }

            public int? Data3 { get; set; }
        }
        
        public sealed class BinderViewModel : BaseViewModel
        {
            private static readonly Binder<BinderViewModel> Binder = new();

            static BinderViewModel()
            {
                Binder.Bind(x => x.Data == null ? 99 : x.Data + 1).To(x => x.DataPlusOne);
                Binder.Bind(x => x.Data + x.Data1 + x.Data2 + x.Data3).To(x => x.Sum);
            }

            public override int? DataPlusOne { get; protected set; }

            public override int? Sum { get; protected set; }
            
            public BinderViewModel()
            {
                Binder.Attach(this).AddTo(Anchors);
            }

            public override string ToString()
            {
                return "Binder";
            }
        }
        
        public sealed class PropertyHelperViewModel : BaseViewModel
        {
            private readonly ObservableAsPropertyHelper<int?> sum;
            private readonly ObservableAsPropertyHelper<int?> dataPlusOne;

            public PropertyHelperViewModel()
            {
                this.WhenAnyValue(x => x.Data)
                    .Select(x => x == null ? 99 : x + 1)
                    .ToProperty(out dataPlusOne, this, x => x.DataPlusOne)
                    .AddTo(Anchors);

                this.WhenAnyProperty(x => x.Data, x => x.Data1, x => x.Data2, x => x.Data3)
                    .Select(_ => Data + Data1 + Data2 + Data3)
                    .ToProperty(out sum, this, x => x.Sum)
                    .AddTo(Anchors);
            }

            public override int? DataPlusOne
            {
                get => dataPlusOne.Value;
                protected set => throw new NotImplementedException();
            }

            public override int? Sum
            {
                get => sum.Value;
                protected set => throw new NotImplementedException();
            }

            public override string ToString()
            {
                return "PropertyHelper";
            }
        }
        
        public sealed class ReactiveViewModel : BaseViewModel
        {
            
            public ReactiveViewModel()
            {
                this.WhenAnyValue(x => x.Data)
                    .Subscribe(x => DataPlusOne = x == null ? 99 : x + 1)
                    .AddTo(Anchors);

                this.WhenAnyProperty(x => x.Data, x => x.Data1, x => x.Data2, x => x.Data3)
                    .Subscribe(_ => Sum = Data + Data1 + Data2 + Data3)
                    .AddTo(Anchors);
            }
            
            public override int? DataPlusOne { get; protected set; }

            public override int? Sum { get; protected set; }
            
            public override string ToString()
            {
                return "Reactive";
            }
        }
    }
}