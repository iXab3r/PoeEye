using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Moq;
using NUnit.Framework;
using PoeEye.Config;
using PoeEye.PoeTrade.Models;
using PoeEye.PoeTrade.ViewModels;
using PoeEye.Tests.Helpers;
using PoeShared;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.PoeTrade;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Shouldly;

namespace PoeEye.Tests.PoeEye.PoeTrade.ViewModels
{
    [TestFixture]
    internal sealed class PoeTradesListViewModelFixture
    {
        [SetUp]
        public void SetUp()
        {
            clock = new Mock<IClock>();
            clock
                .SetupGet(x => x.Now)
                .Returns(new DateTime(2015, 1, 1));

            quickFilterFactory = new Mock<IFactory<IPoeTradeQuickFilter>>();
            quickFilterFactory.Setup(x => x.Create()).Returns(() => new Mock<IPoeTradeQuickFilter>().Object);

            ReadOnlyObservableCollection<IPoeTradeViewModel> advancedListAddedList = null;
            advancedList = new Mock<IPoeAdvancedTradesListViewModel>();
            advancedList
                .Setup(x => x.Add(It.IsAny<ReadOnlyObservableCollection<IPoeTradeViewModel>>()))
                .Callback((ReadOnlyObservableCollection<IPoeTradeViewModel> x) => advancedListAddedList = x);
            advancedList
                .SetupGet(x => x.Items)
                .Returns(() => advancedListAddedList);

            listFactory = new Mock<IFactory<IPoeAdvancedTradesListViewModel>>();
            listFactory.Setup(x => x.Create()).Returns(advancedList.Object);

            poeApiWrapper = new Mock<IPoeApiWrapper>();

            poeTradeViewModelFactory = new Mock<IFactory<IPoeTradeViewModel, IPoeItem>>();
            poeTradeViewModelFactory
                .Setup(x => x.Create(It.IsAny<IPoeItem>()))
                .Returns((IPoeItem item) => CreateTradeVm(item));

            poeItemsComparer = new Mock<IEqualityComparer<IPoeItem>>();
            poeItemsComparer
                .Setup(x => x.Equals(It.IsAny<IPoeItem>(), It.IsAny<IPoeItem>()))
                .Returns(false);

            poeLiveHistoryItems = new Subject<IPoeItem[]>();
            poeLiveHistoryUpdateExceptions = new Subject<Exception>();
            poeLiveHistory = new Mock<IPoeLiveHistoryProvider>();
            poeLiveHistory.SetupGet(x => x.ItemsPacks).Returns(poeLiveHistoryItems);
            poeLiveHistory.SetupGet(x => x.UpdateExceptions).Returns(poeLiveHistoryUpdateExceptions);

            poeLiveHistoryFactory = new Mock<IFactory<IPoeLiveHistoryProvider, IPoeApiWrapper, IPoeQueryInfo>>();
            poeLiveHistoryFactory
                .Setup(x => x.Create(It.IsAny<IPoeApiWrapper>(), It.IsAny<IPoeQueryInfo>()))
                .Returns(poeLiveHistory.Object);

            captchaService = new Mock<IPoeCaptchaRegistrator>();
            captchaService.Setup(x => x.CaptchaRequests).Returns(new Subject<string>());
            
            configSink = new Subject<PoeEyeMainConfig>();
            configProvider = new Mock<IConfigProvider<PoeEyeMainConfig>>();
            configProvider.SetupGet(x => x.WhenChanged).Returns(configSink);
        }

        private Mock<IClock> clock;
        private Mock<IPoeApiWrapper> poeApiWrapper;
        private Mock<IEqualityComparer<IPoeItem>> poeItemsComparer;
        private Mock<IFactory<IPoeTradeViewModel, IPoeItem>> poeTradeViewModelFactory;
        private Mock<IFactory<IPoeLiveHistoryProvider, IPoeApiWrapper, IPoeQueryInfo>> poeLiveHistoryFactory;
        private Mock<IFactory<IPoeAdvancedTradesListViewModel>> listFactory;
        private Mock<IFactory<IPoeTradeQuickFilter>> quickFilterFactory;
        private Mock<IPoeAdvancedTradesListViewModel> advancedList;
        private Mock<IConfigProvider<PoeEyeMainConfig>> configProvider;

        private Mock<IPoeLiveHistoryProvider> poeLiveHistory;
        private Mock<IPoeCaptchaRegistrator> captchaService;
        private ISubject<IPoeItem[]> poeLiveHistoryItems;
        private ISubject<Exception> poeLiveHistoryUpdateExceptions;
        private ISubject<PoeEyeMainConfig> configSink;

        private IPoeTradeViewModel CreateTradeVm(IPoeItem item)
        {
            var result = new Mock<IPoeTradeViewModel>();
            result.SetupGet(x => x.Trade).Returns(item);
            result.SetupGet(x => x.Anchors).Returns(new CompositeDisposable());
            result
                .SetupSet(x => x.TradeState = It.IsAny<PoeTradeState>())
                .Callback((PoeTradeState value) => result.SetPropertyAndNotify(x => x.TradeState, value));
            return result.Object;
        }

        private PoeTradesListViewModel CreateInstance()
        {
            return new PoeTradesListViewModel(
                poeApiWrapper.Object,
                poeLiveHistoryFactory.Object,
                poeTradeViewModelFactory.Object,
                listFactory.Object,
                captchaService.Object,
                poeItemsComparer.Object,
                quickFilterFactory.Object,
                configProvider.Object,
                clock.Object,
                Scheduler.Immediate);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ShouldAddItemsToList(int itemsCount)
        {
            //Given
            var instance = CreateInstance();

            var itemsPack = Enumerable.Range(0, itemsCount).Select(_ => Mock.Of<IPoeItem>()).ToArray();

            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            //When
            poeLiveHistoryItems.OnNext(itemsPack);

            //Then
            itemsPack.ForEach(x => poeTradeViewModelFactory.Verify(y => y.Create(x), Times.Once));
        }

        [Test]
        [TestCase(PoeTradeState.New, PoeTradeState.New)]
        [TestCase(PoeTradeState.Normal, PoeTradeState.Normal)]
        [TestCase(PoeTradeState.Unknown, PoeTradeState.Unknown)]
        [TestCase(PoeTradeState.Removed, PoeTradeState.New)]
        public void ShouldChangeTradeStateToNewWhenSameItemArrivedAgainAndHadRemovedStateBefore(PoeTradeState initialState, PoeTradeState expectedState)
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeItemsComparer
                .Setup(x => x.Equals(It.IsAny<IPoeItem>(), It.IsAny<IPoeItem>()))
                .Returns(true);

            var item = Mock.Of<IPoeItem>();
            poeLiveHistoryItems.OnNext(new[] {item});

            var trade = instance.Items.Single();
            Assert.AreEqual(PoeTradeState.New, trade.TradeState);

            trade.TradeState = initialState;

            //When
            poeLiveHistoryItems.OnNext(new[] {item});

            //Then
            trade.TradeState.ShouldBe(expectedState);
        }

        [Test]
        public void ShouldChangeTradeStateToNormalWhenMarkedAsRead()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeLiveHistoryItems.OnNext(new[] {Mock.Of<IPoeItem>()});

            var trade = instance.Items.Single();
            Assert.AreEqual(PoeTradeState.New, trade.TradeState);

            //When
            trade.TradeState = PoeTradeState.Normal;

            //Then
            trade.TradeState.ShouldBe(PoeTradeState.Normal);
        }

        [Test]
        public void ShouldChangeTradeStateToRemoved()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeLiveHistoryItems.OnNext(new[] {Mock.Of<IPoeItem>()});

            var trade = instance.Items.Single();
            Assert.AreEqual(PoeTradeState.New, trade.TradeState);

            //When
            poeLiveHistoryItems.OnNext(new[] {Mock.Of<IPoeItem>()});

            //Then
            trade.TradeState.ShouldBe(PoeTradeState.Removed);
        }

        [Test]
        public void ShouldDisposeLiveHistoryProviderOnQueryChange()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeLiveHistory.ResetCalls();

            //When
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            //Then
            poeLiveHistory.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void ShouldPropagateLiveHistoryExceptions()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            var now = new DateTime(2015, 1, 1);
            clock.SetupGet(x => x.Now).Returns(now);
            var exception = new Exception("msg");

            //When
            poeLiveHistoryUpdateExceptions.OnNext(exception);

            //Then
            instance.Errors.ShouldBe($"[{now}] msg");
        }

        [Test]
        [Ignore("Not working due to problems with SetPropertyAndNotify")]
        public void ShouldRemoveItemWhenTradeStateIsRemovedAndItemIsMarkedAsRead()
        {
            //Given
            var instance = CreateInstance();

            poeTradeViewModelFactory.Setup(x => x.Create(It.IsAny<IPoeItem>())).Returns((IPoeItem item) =>
            {
                var tradeMock = new Mock<IPoeTradeViewModel>();
                tradeMock.SetupAllProperties();
                tradeMock.SetupGet(x => x.Trade).Returns(item);
                tradeMock.SetupSet(x => x.TradeState).Callback(value => tradeMock.SetPropertyAndNotify(x => x.TradeState, value));
                return tradeMock.Object;
            });

            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeLiveHistoryItems.OnNext(new[] {Mock.Of<IPoeItem>()});

            var trade = instance.Items.Single();
            Assert.AreEqual(PoeTradeState.New, trade.TradeState);

            poeLiveHistoryItems.OnNext(new IPoeItem[0]);
            Assert.AreEqual(PoeTradeState.Removed, trade.TradeState);
            Assert.AreEqual(1, instance.Items.Count);

            //When
            trade.TradeState = PoeTradeState.Normal; // same as when user clicks on MarkAsRead button

            //Then
            instance.Items.Count.ShouldBe(0);
        }
    }
}