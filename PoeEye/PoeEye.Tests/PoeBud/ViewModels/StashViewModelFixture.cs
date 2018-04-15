using PoeBud.Config;
using PoeBud.Models;
using PoeBud.ViewModels;
using PoeEye.Tests.PoeBud.TestData;
using PoeShared;
using PoeShared.Common;
using PoeShared.PoeTrade;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;

namespace PoeEye.Tests.PoeBud.ViewModels
{
    using System.Linq;

    using Moq;

    using NUnit.Framework;

    using Shouldly;

    [TestFixture]
    public class StashViewModelFixture
    {
        private StashUpdate stashUpdate;

        private Mock<IPoeBudConfig> poeBudConfig;
        private Mock<IPoePriceCalculcator> poePriceCalculcator;
        private Mock<IPriceSummaryViewModel> summaryViewModel;

        [SetUp]
        public void SetUp()
        {
            poeBudConfig = new Mock<IPoeBudConfig>();
            
            poePriceCalculcator = new Mock<IPoePriceCalculcator>();
            poePriceCalculcator
                .Setup(x => x.GetEquivalentInChaosOrbs(It.IsAny<PoePrice>()))
                .Returns((PoePrice x) => new PoePrice(KnownCurrencyNameList.ChaosOrb, x.Value));
            
            stashUpdate = StashUpdate.Empty;
            
            summaryViewModel = new Mock<IPriceSummaryViewModel>();
        }

        [Test]
        public void ShouldCreate()
        {
            //Then
            CreateInstance();
        }

        [Test]
        public void ShouldGetMaps()
        {
            //Given
            stashUpdate = TestDataProvider.Stash1_WithTabs;

            //When
            var instance = CreateInstance();

            //Then
            instance.MapsSolutions.SelectMany(x => x.Items).ShouldContain(x => x.Name == "Sacrifice at Midnight");
            instance.MapsSolutions.SelectMany(x => x.Items).ShouldContain(x => x.Name == "Crystal Ore Map");
        }

        private StashViewModel CreateInstance()
        {
            return new StashViewModel(
                stashUpdate,
                poeBudConfig.Object,
                summaryViewModel.Object);
        }
    }
}