namespace PoeEye.Tests.PoeTrade.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Reactive.Subjects;

    using Factory;

    using Helpers;

    using NUnit.Framework;

    using Moq;

    using PoeEyeUi.PoeTrade;
    using PoeEyeUi.PoeTrade.ViewModels;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using ReactiveUI;

    using Shouldly;

    using TypeConverter;

    [TestFixture]
    internal sealed class PoeTradesListViewModelFixture
    {
        private Mock<IClock> clock;
        private Mock<IEqualityComparer<IPoeItem>> poeItemsComparer;
        private Mock<IFactory<IPoeTradeViewModel, IPoeItem>> poeTradeViewModelFactory;
        private Mock<IFactory<IPoeLiveHistoryProvider, IPoeQuery>> poeLiveHistoryFactory;
        private Mock<IFactory<IHistoricalTradesViewModel, IReactiveList<IPoeItem>, IReactiveList<IPoeTradeViewModel>>> poeHistoricalTradesViewModelFactory;

        private Mock<IPoeLiveHistoryProvider> poeLiveHistory;
        private ISubject<IPoeItem[]> poeLiveHistoryItems; 
        private ISubject<Exception> poeLiveHistoryUpdateExceptions;

        private Mock<IConverter<IPoeQueryInfo, IPoeQuery>> poeQueryInfoToQueryConverter;

        [SetUp]
        public void SetUp()
        {
            clock = new Mock<IClock>();
            clock
                .SetupGet(x => x.CurrentTime)
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

            poeHistoricalTradesViewModelFactory = new Mock<IFactory<IHistoricalTradesViewModel, IReactiveList<IPoeItem>, IReactiveList<IPoeTradeViewModel>>>();
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
            var actualItems = instance.TradesList.Select(x => x.Trade).ToArray();
            CollectionAssert.AreEqual(itemsPack, actualItems);
        }

        [Test]
        public void ShouldPropagateLiveHistoryExceptions()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            var exception = new Exception("msg");

            //When
            poeLiveHistoryUpdateExceptions.OnNext(exception);

            //Then
            instance.LastUpdateException.ShouldBeSameAs(exception);
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
        public void ShouldChangeTradeStateToRemoved()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeLiveHistoryItems.OnNext(new[] { Mock.Of<IPoeItem>() });

            var trade = instance.TradesList.Single();
            Assert.AreEqual(PoeTradeState.New, trade.TradeState);

            //When
            poeLiveHistoryItems.OnNext(new[] { Mock.Of<IPoeItem>() });

            //Then
            trade.TradeState.ShouldBe(PoeTradeState.Removed);
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

            poeLiveHistoryItems.OnNext(new[] { Mock.Of<IPoeItem>() });

            var trade = instance.TradesList.Single();
            Assert.AreEqual(PoeTradeState.New, trade.TradeState);
            trade.TradeState = state;

            //When
            poeLiveHistoryItems.OnNext(new[] { Mock.Of<IPoeItem>() });

            //Then
            trade.TradeState.ShouldBe(state);
        }

        [Test]
        public void ShouldChangeTradeStateToNormalWhenMarkedAsRead()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeLiveHistoryItems.OnNext(new[] { Mock.Of<IPoeItem>() });

            var trade = instance.TradesList.Single();
            Assert.AreEqual(PoeTradeState.New, trade.TradeState);

            //When
            trade.TradeState = PoeTradeState.Normal;

            //Then
            trade.TradeState.ShouldBe(PoeTradeState.Normal);
        }

        [Test]
        public void ShouldRemoveItemWhenTradeStateIsRemovedAndItemIsMarkedAsRead()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeLiveHistoryItems.OnNext(new[] { Mock.Of<IPoeItem>() });

            var trade = instance.TradesList.Single();
            Assert.AreEqual(PoeTradeState.New, trade.TradeState);

            poeLiveHistoryItems.OnNext(new IPoeItem[0]);
            Assert.AreEqual(PoeTradeState.Removed, trade.TradeState);
            Assert.AreEqual(1, instance.TradesList.Count);

            //When
            trade.TradeState = PoeTradeState.Normal;

            //Then
            trade.TradeState.ShouldBe(PoeTradeState.Normal);
            instance.TradesList.Count.ShouldBe(0);
        }

        [Test]
        public void ShouldMoveRemovedItemsToHistoricalTrades()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            poeLiveHistoryItems.OnNext(new[] { Mock.Of<IPoeItem>() });

            var trade = instance.TradesList.Single();

            poeLiveHistoryItems.OnNext(new IPoeItem[0]);

            //When
            trade.TradeState = PoeTradeState.Normal;

            //Then
            CollectionAssert.AreEqual(new IPoeTradeViewModel[0], instance.TradesList);
            CollectionAssert.AreEqual(new[] { trade.Trade }, instance.HistoricalTrades);
        }

        [Test]
        public void ShouldRemoveItemFromHistoricalTradesIfArrivedAgain()
        {
            //Given
            var instance = CreateInstance();
            instance.ActiveQuery = Mock.Of<IPoeQueryInfo>();

            var item = Mock.Of<IPoeItem>();
            poeLiveHistoryItems.OnNext(new[] { item });

            poeLiveHistoryItems.OnNext(new IPoeItem[0]);

            var trade = instance.TradesList.Single();
            trade.TradeState = PoeTradeState.Normal;

            poeItemsComparer
                .Setup(x => x.Equals(It.IsAny<IPoeItem>(), It.IsAny<IPoeItem>()))
                .Returns(true);

            //When
            poeLiveHistoryItems.OnNext(new[] { item });

            //Then
            CollectionAssert.AreEqual(new IPoeItem[0], instance.HistoricalTrades);

            var actualItems = instance.TradesList.Select(x => x.Trade).ToArray();
            CollectionAssert.AreEqual(new[] { item }, actualItems);
        }

        private IPoeTradeViewModel CreateTradeVm(IPoeItem item)
        {
            var result = new Mock<IPoeTradeViewModel>();
            result.SetupGet(x => x.Trade).Returns(item);
            result
                .SetupSet(x => x.TradeState)
                .Callback(value => result.SetPropertyAndNotify(x => x.TradeState, value));
            return result.Object;
        }

        private PoeTradesListViewModel CreateInstance()
        {
            return new PoeTradesListViewModel( 
                poeLiveHistoryFactory.Object,
                poeTradeViewModelFactory.Object,
                poeHistoricalTradesViewModelFactory.Object,
                poeItemsComparer.Object,
                poeQueryInfoToQueryConverter.Object,
                clock.Object,
                Scheduler.Immediate);
        }
    }
}