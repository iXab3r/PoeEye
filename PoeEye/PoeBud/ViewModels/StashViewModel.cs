using PoeBud.Scaffolding;
using PoeShared;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.ViewModels
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Config;

    using Guards;

    using JetBrains.Annotations;

    using Models;

    internal sealed class StashViewModel
    {
        public StashViewModel([NotNull] StashUpdate stashUpdate, [NotNull] IPoeBudConfig config)
        {
            Guard.ArgumentNotNull(stashUpdate, nameof(stashUpdate));
            Guard.ArgumentNotNull(config, nameof(config));

            StashUpdate = stashUpdate;

            var rareItems = stashUpdate.Items.GetTradeableItems();

            var chests = rareItems.Where(x => x.ItemType == GearType.Chest).ToArray();
            var weapons = rareItems.Where(x => x.IsWeapon()).Select(x => new { Item = x, Score = x.GetTradeScore() }).ToArray();
            var helmets = rareItems.Where(x => x.ItemType == GearType.Helmet).ToArray();
            var rings = rareItems.Where(x => x.ItemType == GearType.Ring).ToArray();
            var amulets = rareItems.Where(x => x.ItemType == GearType.Amulet).ToArray();
            var gloves = rareItems.Where(x => x.ItemType == GearType.Gloves).ToArray();
            var boots = rareItems.Where(x => x.ItemType == GearType.Boots).ToArray();
            var belts = rareItems.Where(x => x.ItemType == GearType.Belt).ToArray();

            Log.Instance.Debug($"[StashViewModel] Stash dump(itemsCount: {stashUpdate.Items.Count()}, tabsCount: {stashUpdate.Tabs.Count()})");
            Log.Instance.DebugFormat(
                "[StashViewModel] Rare items:\r\n{0}",
                string.Join(
                    "\r\n",
                    new object[]
                    {
                        new {ItemType = $"Chests ({chests.Length})", Items = chests.Select(x => x.ToString())},
                        new {ItemType = $"Weapons ({weapons.Length})", Items = weapons.Select(x => $"{x.Item.ToString()} ({x.Score}, W:{x.Item.Width}, H:{x.Item.Height})")},
                        new {ItemType = $"Helmets ({helmets.Length})", Items = helmets.Select(x => x.ToString())},
                        new {ItemType = $"Rings ({rings.Length})", Items = rings.Select(x => x.ToString())},
                        new {ItemType = $"Amulets ({amulets.Length})", Items = amulets.Select(x => x.ToString())},
                        new {ItemType = $"Gloves ({gloves.Length})", Items = gloves.Select(x => x.ToString())},
                        new {ItemType = $"Boots ({boots.Length})", Items = boots.Select(x => x.ToString())},
                        new {ItemType = $"Belts ({belts.Length})", Items = belts.Select(x => x.ToString())}
                    }.Select(x => x.DumpToText())));


            Solutions = GetSolutions(stashUpdate);

            ChestsCount = chests.Count();
            WeaponsCount = (int)weapons.Sum(x => x.Score);
            HelmetsCount = helmets.Count();
            RingsNormalizedCount = rings.Count() / 2;
            AmuletsCount = amulets.Count();
            GlovesCount = gloves.Count();
            BootsCount = boots.Count();
            BeltsCount = belts.Count();


            var minItemsCount = 0;
            if (config.ExpectedSetsCount > 0)
            {
                minItemsCount = config.ExpectedSetsCount;
            }
            else
            {
                minItemsCount = new[]
                {
                    ChestsCount,WeaponsCount,HelmetsCount,RingsNormalizedCount,AmuletsCount,GlovesCount,BootsCount,BeltsCount
                }.Max();
            }

            IsInsufficientChestsCount = ChestsCount < minItemsCount;
            IsInsufficientWeaponsCount = WeaponsCount < minItemsCount;
            IsInsufficientHelmetsCount = HelmetsCount < minItemsCount;
            IsInsufficientRingsNormalizedCount = RingsNormalizedCount < minItemsCount;
            IsInsufficientAmuletsCount = AmuletsCount < minItemsCount;
            IsInsufficientGlovesCount = GlovesCount < minItemsCount;
            IsInsufficientBootsCount = BootsCount < minItemsCount;
            IsInsufficientBeltsCount = BeltsCount < minItemsCount;
        }

        public StashUpdate StashUpdate { get; }

        public int ChestsCount { get; }

        public int WeaponsCount { get; }

        public int HelmetsCount { get; }

        public int RingsNormalizedCount { get; }

        public int AmuletsCount { get; }

        public int GlovesCount { get; }

        public int BootsCount { get; }

        public int BeltsCount { get; }

        public bool IsInsufficientChestsCount { get; }

        public bool IsInsufficientWeaponsCount { get; }

        public bool IsInsufficientHelmetsCount { get; }

        public bool IsInsufficientRingsNormalizedCount { get; }

        public bool IsInsufficientAmuletsCount { get; }

        public bool IsInsufficientGlovesCount { get; }

        public bool IsInsufficientBootsCount { get; }

        public bool IsInsufficientBeltsCount { get; }

        public IPoeTradeSolution[] Solutions { get; }

        private static IPoeTradeSolution[] GetSolutions(StashUpdate stashUpdate)
        {
            var result = new List<IPoeTradeSolution>();

            var rareItems = stashUpdate.Items.GetTradeableItems();

            var chests = rareItems.Where(x => x.ItemType == GearType.Chest);
            var weapons = rareItems.Where(x => x.IsWeapon());
            var helmets = rareItems.Where(x => x.ItemType == GearType.Helmet);
            var rings = rareItems.Where(x => x.ItemType == GearType.Ring);
            var amulets = rareItems.Where(x => x.ItemType == GearType.Amulet);
            var gloves = rareItems.Where(x => x.ItemType == GearType.Gloves);
            var boots = rareItems.Where(x => x.ItemType == GearType.Boots);
            var belts = rareItems.Where(x => x.ItemType == GearType.Belt);

            var itemsByCategories = new[]
            {
                chests,
                helmets,
                gloves,
                belts,
                boots,
                weapons.OrderByDescending(x => x.GetTradeScore()), // required to make sure that we do not mix-up .5 and 1 score items
                amulets,
                rings,
            }
                .Select(x => new ConcurrentQueue<IStashItem>(x))
                .ToArray();

            const float targetScoreForEachCategory = 1;
            const float targetScoreForSolution = 8;

            do
            {
                var tierScore = 0f;
                var ongoingSolutionItems = new List<IPoeTradeItem>();
                foreach (var items in itemsByCategories)
                {
                    var categoryScore = 0f;

                    IStashItem nextItem;
                    while (categoryScore < targetScoreForEachCategory && items.TryDequeue(out nextItem))
                    {
                        categoryScore += nextItem.GetTradeScore();

                        var tradeItem = new PoeTradeItem(nextItem);
                        ongoingSolutionItems.Add(tradeItem);
                    }

                    tierScore += categoryScore;
                }

                if (tierScore >= targetScoreForSolution)
                {
                    result.Add(new PoeTradeSolution(ongoingSolutionItems.ToArray(), stashUpdate.Tabs));
                }
                else
                {
                    break;
                }
            } while (true);

            return result.ToArray();
        }

        private class PoeTradeItem : IPoeTradeItem
        {
            public PoeTradeItem(IStashItem item)
            {
                Name = item.ToString();
                X = item.X;
                Y = item.Y;
                TabIndex = item.GetTabIndex() ?? -1;
                ItemType = item.ItemType;
            }

            public string Name { get; }

            public int X { get; }

            public int Y { get; }

            public int TabIndex { get; }

            public GearType ItemType { get; }
        }

        private class PoeTradeSolution : IPoeTradeSolution
        {
            public PoeTradeSolution(IPoeTradeItem[] items, IStashTab[] tabs)
            {
                Items = items;
                Tabs = tabs;
            }

            public IPoeTradeItem[] Items { get; }

            public IStashTab[] Tabs { get; }
        }
    }
}
