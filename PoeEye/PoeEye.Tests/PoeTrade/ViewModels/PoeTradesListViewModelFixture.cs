namespace PoeEye.Tests.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Subjects;

    using Helpers;

    using Moq;

    using NUnit.Framework;

    using PoeEye.PoeTrade;
    using PoeEye.PoeTrade.Models;
    using PoeEye.PoeTrade.ViewModels;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;
    using PoeShared.Prism;

    using Shouldly;

    using TypeConverter;

    [TestFixture]
    internal sealed class PoeTradesListViewModelFixture
    {
        private Mock<IClock> clock;
        private Mock<IEqualityComparer<IPoeItem>> poeItemsComparer;
        private Mock<IFactory<IPoeTradeViewModel, IPoeItem>> poeTradeViewModelFactory;
        private Mock<IFactory<IPoeLiveHistoryProvider, IPoeQuery>> poeLiveHistoryFactory;

        private Mock<IPoeLiveHistoryProvider> poeLiveHistory;
        private Mock<IHistoricalTradesViewModel> historicalTradesViewModel;
        private Mock<IPoeCaptchaRegistrator> captchaService;
        private ISubject<IPoeItem[]> poeLiveHistoryItems;
        private ISubject<Exception> poeLiveHistoryUpdateExceptions;

        private Mock<IConverter<IPoeQueryInfo, IPoeQuery>> poeQueryInfoToQueryConverter;

        [SetUp]
        public void SetUp()
        {
            clock = new Mock<IClock>();
            clock
                .SetupGet(x => x.Now)
                .Returns(new DateTime(2015, 1, 1));

            poeTradeViewModelFactory = new Mock<IFactory<IPoeTradeViewModel, IPoeItem>>();
            poeTradeViewModelFactory
                .Setup(x => x.Create(It.IsAny<IPoeItem>()))
                .Returns((IPoeItem item) => CreateTradeVm(item));

            poeItemsComparer = new Mock<IEqualityComparer<IPoeItem>>();
            poeItemsComparer
                .Setup(x => x.Equals(It.IsAny<IPoeItem>(), It.IsAny<IPoeItem>()))
                .Returns(false);

            poeQueryInfoToQueryConverter = new Mock<IConverter<IPoeQueryInfo, IPoeQuery>>();
            poeQueryInfoToQueryConverter
                .Setup(x => x.Convert(It.IsAny<IPoeQueryInfo>()))
                .Returns(Mock.Of<IPoeQuery>());

            poeLiveHistoryItems = new Subject<IPoeItem[]>();
            poeLiveHistoryUpdateExceptions = new Subject<Exception>();
            poeLiveHistory = new Mock<IPoeLiveHistoryProvider>();
            poeLiveHistory.SetupGet(x => x.ItemsPacks).Returns(poeLiveHistoryItems);
            poeLiveHistory.SetupGet(x => x.UpdateExceptions).Returns(poeLiveHistoryUpdateExceptions);

            poeLiveHistoryFactory = new Mock<IFactory<IPoeLiveHistoryProvider, IPoeQuery>>();
            poeLiveHistoryFactory
                .Setup(x => x.Create(It.IsAny<IPoeQuery>()))
                .Returns(poeLiveHistory.Object);

            historicalTradesViewModel = new Mock<IHistoricalTradesViewModel>();
            captchaService = new Mock<IPoeCaptchaRegistrator>();
            captchaService.Setup(x => x.CaptchaRequests).Returns(new Subject<string>());
        }

        private IPoeTradeViewModel CreateTradeVm(IPoeItem item)
        {
            var result = new Mock<IPoeTradeViewModel>();
            result.SetupGet(x => x.Trade).Returns(item);
            result.SetupGet(x => x.Anchors).Returns(new CompositeDisposable());
            result
                .SetupSet(x => x.TradeState)
                .Callback(value => result.SetPropertyAndNotify(x => x.TradeState, value));
            return result.Object;
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
            var actualItems = instance.Items.Select(x => x.Trade).ToArray();
            CollectionAssert.AreEqual(itemsPack, actualItems);
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
        public void ShouldCreateLiveHistoryProviderOnActiveQueryChange()
        {
            //Given
            var instance = CreateInstance();

            var queryInfo = Mock.Of<IPoeQueryInfo>();
            var convertedQuery = Mock.Of<IPoeQuery>();

            poeQueryInfoToQueryConverter
                .Setup(x => x.Convert(It.IsAny<IPoeQueryInfo>()))
                .Returns(convertedQuery);

            //When
            instance.ActiveQuery = queryInfo;

            //Then
            poeLiveHistoryFactory.Verify(x => x.Create(convertedQuery), Times.Once);
            poeQueryInfoToQueryConverter.Verify(x => x.Convert(queryInfo), Times.Once);
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
        public void ShouldMoveRemovedItemsToHistoricalTrades()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeLiveHistoryItems.OnNext(new[] {Mock.Of<IPoeItem>()});

            var trade = instance.Items.Single();

            poeLiveHistoryItems.OnNext(new IPoeItem[0]);

            //When
            trade.TradeState = PoeTradeState.Normal;

            //Then
            CollectionAssert.AreEqual(new IPoeTradeViewModel[0], instance.Items);
            historicalTradesViewModel.Verify(x => x.AddItems(trade.Trade), Times.Once);
        }

        [Test]
        [Theory]
        public void ShouldNotChangeTradeStateWhenSameItemArrivedAgain(PoeTradeState state)
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeItemsComparer
                .Setup(x => x.Equals(It.IsAny<IPoeItem>(), It.IsAny<IPoeItem>()))
                .Returns(true);

            poeLiveHistoryItems.OnNext(new[] {Mock.Of<IPoeItem>()});

            var trade = instance.Items.Single();
            Assert.AreEqual(PoeTradeState.New, trade.TradeState);
            trade.TradeState = state;

            //When
            poeLiveHistoryItems.OnNext(new[] {Mock.Of<IPoeItem>()});

            //Then
            trade.TradeState.ShouldBe(state);
        }

        [Test]
        public void ShouldNotRemoveItemFromHistoricalTradesIfArrivedAgain()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            var item = Mock.Of<IPoeItem>();
            poeLiveHistoryItems.OnNext(new[] {item});

            poeLiveHistoryItems.OnNext(new IPoeItem[0]);

            var trade = instance.Items.Single();
            trade.TradeState = PoeTradeState.Normal;

            poeItemsComparer
                .Setup(x => x.Equals(It.IsAny<IPoeItem>(), It.IsAny<IPoeItem>()))
                .Returns(true);

            historicalTradesViewModel.Reset();

            //When
            poeLiveHistoryItems.OnNext(new[] {item});

            //Then
            historicalTradesViewModel.Verify(x => x.AddItems(It.IsAny<IPoeItem[]>()), Times.Never);
            historicalTradesViewModel.Verify(x => x.Clear(), Times.Never);

            var actualItems = instance.Items.Select(x => x.Trade).ToArray();
            CollectionAssert.AreEqual(new[] {item}, actualItems);
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
        public void ShouldRemoveItemWhenTradeStateIsRemovedAndItemIsMarkedAsRead()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeLiveHistoryItems.OnNext(new[] {Mock.Of<IPoeItem>()});

            var trade = instance.Items.Single();
            Assert.AreEqual(PoeTradeState.New, trade.TradeState);

            poeLiveHistoryItems.OnNext(new IPoeItem[0]);
            Assert.AreEqual(PoeTradeState.Removed, trade.TradeState);
            Assert.AreEqual(1, instance.Items.Count);

            //When
            trade.TradeState = PoeTradeState.Normal;

            //Then
            trade.TradeState.ShouldBe(PoeTradeState.Normal);
            instance.Items.Count.ShouldBe(0);
        }

        private PoeTradesListViewModel CreateInstance()
        {
            return new PoeTradesListViewModel(
                poeLiveHistoryFactory.Object,
                poeTradeViewModelFactory.Object,
                captchaService.Object,
                historicalTradesViewModel.Object,
                poeItemsComparer.Object,
                poeQueryInfoToQueryConverter.Object,
                clock.Object,
                Scheduler.Immediate);
        }
    }
}