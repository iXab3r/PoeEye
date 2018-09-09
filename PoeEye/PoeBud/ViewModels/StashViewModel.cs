using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Guards;
using JetBrains.Annotations;
using PoeBud.Config;
using PoeBud.Models;
using PoeBud.Scaffolding;
using PoeShared;
using PoeShared.Converters;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.ViewModels
{
    internal sealed class StashViewModel : DisposableReactiveObject
    {
        public StashViewModel(
            [NotNull] StashUpdate stashUpdate,
            [NotNull] IPoeBudConfig config,
            [NotNull] IPriceSummaryViewModel summaryViewModel)
        {
            Guard.ArgumentNotNull(stashUpdate, nameof(stashUpdate));
            Guard.ArgumentNotNull(config, nameof(config));
            Guard.ArgumentNotNull(summaryViewModel, nameof(summaryViewModel));

            StashUpdate = stashUpdate;
            PriceSummary = summaryViewModel;

            var rareItems = stashUpdate.Items.GetChaosSetItems();
            MaxSlotsPerSolution = config.MaxSlotsPerSolution;

            Log.Instance.Debug(
                $"[StashViewModel] Stash dump(itemsCount: {stashUpdate.Items.Count()}, tabsCount: {stashUpdate.Tabs.Count()}), maxSlotsPerSolution: {config.MaxSlotsPerSolution}");

            var chests = rareItems.Where(x => x.ItemType == GearType.Chest).ToArray();
            var weapons = rareItems.Where(x => x.IsWeapon()).Select(x => new {Item = x, Score = x.GetTradeScore()}).ToArray();
            var helmets = rareItems.Where(x => x.ItemType == GearType.Helmet).ToArray();
            var rings = rareItems.Where(x => x.ItemType == GearType.Ring).ToArray();
            var amulets = rareItems.Where(x => x.ItemType == GearType.Amulet).ToArray();
            var gloves = rareItems.Where(x => x.ItemType == GearType.Gloves).ToArray();
            var boots = rareItems.Where(x => x.ItemType == GearType.Boots).ToArray();
            var belts = rareItems.Where(x => x.ItemType == GearType.Belt).ToArray();
            Log.Instance.DebugFormat(
                "[StashViewModel] Rare items:\r\n{0}",
                string.Join(
                    "\r\n",
                    new object[]
                    {
                        new {ItemType = $"Chests ({chests.Length})", Items = chests.Select(x => x.ToString())},
                        new
                        {
                            ItemType = $"Weapons ({weapons.Length})",
                            Items = weapons.Select(x => $"{x.Item.ToString()} ({x.Score}, W:{x.Item.Width}, H:{x.Item.Height})")
                        },
                        new {ItemType = $"Helmets ({helmets.Length})", Items = helmets.Select(x => x.ToString())},
                        new {ItemType = $"Rings ({rings.Length})", Items = rings.Select(x => x.ToString())},
                        new {ItemType = $"Amulets ({amulets.Length})", Items = amulets.Select(x => x.ToString())},
                        new {ItemType = $"Gloves ({gloves.Length})", Items = gloves.Select(x => x.ToString())},
                        new {ItemType = $"Boots ({boots.Length})", Items = boots.Select(x => x.ToString())},
                        new {ItemType = $"Belts ({belts.Length})", Items = belts.Select(x => x.ToString())}
                    }.Select(x => x.DumpToText())));

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
                    ChestsCount, WeaponsCount, HelmetsCount, RingsNormalizedCount, AmuletsCount, GlovesCount, BootsCount, BeltsCount
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

            ChaosSetSolutions = BuildChaosSetSolutions(stashUpdate);
            CurrencySolutions = BuildSolutions(stashUpdate, BuildCurrencySolution).ToArray();
            DivinationCardsSolutions = BuildSolutions(stashUpdate, BuildDivinationCardsSolution).ToArray();
            MapsSolutions = BuildSolutions(stashUpdate, BuildMapsSolution).ToArray();
            MiscellaneousItemsSolutions = BuildSolutions(stashUpdate, BuildMiscellaneousItemsSolution).ToArray();
            SellableSolutions = BuildSolutions(stashUpdate, BuildSixLinksAndChromesSolution).ToArray();

            PriceSummary.Solution = BuildCurrencySolution(stashUpdate);
        }

        public StashUpdate StashUpdate { get; }

        public int MaxSlotsPerSolution { get; }

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

        public IPoeTradeSolution[] ChaosSetSolutions { get; }

        public IPoeTradeSolution[] CurrencySolutions { get; }

        public IPoeTradeSolution[] DivinationCardsSolutions { get; }

        public IPoeTradeSolution[] MapsSolutions { get; }

        public IPoeTradeSolution[] MiscellaneousItemsSolutions { get; }

        public IPoeTradeSolution[] SellableSolutions { get; }

        public IPriceSummaryViewModel PriceSummary { get; }

        private IEnumerable<IPoeTradeSolution> BuildSolutions(StashUpdate stashUpdate, Func<StashUpdate, IPoeTradeSolution> supplier)
        {
            do
            {
                var solution = supplier(stashUpdate);
                solution = new PoeTradeSolution(solution.Items.Take(MaxSlotsPerSolution).ToArray(), solution.Tabs);
                stashUpdate = stashUpdate.RemoveItems(solution.Items);

                if (solution.Items.Any())
                {
                    yield return solution;
                }
                else
                {
                    break;
                }
            } while (true);
        }

        private static IPoeTradeSolution BuildMapsSolution(StashUpdate stashUpdate)
        {
            var tabsByInventoryId = stashUpdate.Tabs.ToDictionary(x => x.GetInventoryId(), x => x);

            var maps = stashUpdate
                       .Items
                       .Where(x => x.GetTabIndex() != null)
                       .Where(x => tabsByInventoryId.ContainsKey(x.InventoryId))
                       .Where(x => x.Categories.Contains("maps"))
                       .Where(x => x.TypeLine != "Divine Vessel")
                       .OrderBy(x => x.ItemLevel)
                       .Select(x => new PoeSolutionItem(x, tabsByInventoryId[x.InventoryId]))
                       .OfType<IPoeSolutionItem>()
                       .ToArray();

            return new PoeTradeSolution(maps, stashUpdate.Tabs);
        }

        private static IPoeTradeSolution BuildSixLinksAndChromesSolution(StashUpdate stashUpdate)
        {
            var tabsByInventoryId = stashUpdate.Tabs.ToDictionary(x => x.GetInventoryId(), x => x);

            var validItems = stashUpdate.Items
                                        .Where(x => x.GetTabIndex() != null)
                                        .ToArray();

            var itemsToInclude = new List<IStashItem>();

            const int maxSixLinksToTransfer = 5;
            validItems
                .Where(x => x.Sockets.EmptyIfNull().Count() == 6)
                .Take(maxSixLinksToTransfer)
                .ForEach(itemsToInclude.Add);

            var result = itemsToInclude
                         .Select(x => new PoeSolutionItem(x, tabsByInventoryId[x.InventoryId]))
                         .OfType<IPoeSolutionItem>()
                         .ToArray();

            return new PoeTradeSolution(result, stashUpdate.Tabs);
        }

        private static IPoeTradeSolution BuildMiscellaneousItemsSolution(StashUpdate stashUpdate)
        {
            var tabsByInventoryId = stashUpdate.Tabs.ToDictionary(x => x.GetInventoryId(), x => x);

            var validItems = stashUpdate.Items
                                        .Where(x => x.GetTabIndex() != null)
                                        .ToArray();

            var itemsToInclude = new List<IStashItem>();

            validItems
                .Where(x => x.Categories.Contains("flasks"))
                .ForEach(itemsToInclude.Add);

            validItems
                .Where(x => x.Categories.Contains("gems"))
                .ForEach(itemsToInclude.Add);

            validItems
                .Where(x => x.TypeLine == "Divine Vessel")
                .ForEach(itemsToInclude.Add);

            validItems
                .Where(x => x.TypeLine != null)
                .Where(x => x.TypeLine.Contains("Eye Jewel") ||
                            x.TypeLine.Contains("Viridian Jewel") ||
                            x.TypeLine.Contains("Cobalt Jewel") ||
                            x.TypeLine.Contains("Crimson Jewel"))
                .ForEach(itemsToInclude.Add);

            var result = itemsToInclude
                         .Where(x => tabsByInventoryId.ContainsKey(x.InventoryId))
                         .Select(x => new PoeSolutionItem(x, tabsByInventoryId[x.InventoryId]))
                         .OfType<IPoeSolutionItem>()
                         .ToArray();

            return new PoeTradeSolution(result, stashUpdate.Tabs);
        }

        private static IPoeTradeSolution BuildCurrencySolution(StashUpdate stashUpdate)
        {
            var tabsByInventoryId = stashUpdate.Tabs.ToDictionary(x => x.GetInventoryId(), x => x);

            var currency = stashUpdate
                           .Items
                           .Where(x => x.GetTabIndex() != null)
                           .Where(x => tabsByInventoryId.ContainsKey(x.InventoryId))
                           .Where(x => x.Categories.Contains("currency"))
                           .Where(x => StringToPoePriceConverter.Instance.Convert($"{x.StackSize} {x.TypeLine}").HasValue)
                           .Select(x => new PoeSolutionItem(x, tabsByInventoryId[x.InventoryId]))
                           .OfType<IPoeSolutionItem>()
                           .ToArray();

            return new PoeTradeSolution(currency, stashUpdate.Tabs);
        }

        private static IPoeTradeSolution BuildDivinationCardsSolution(StashUpdate stashUpdate)
        {
            var tabsByInventoryId = stashUpdate.Tabs.ToDictionary(x => x.GetInventoryId(), x => x);

            var cards = stashUpdate
                        .Items
                        .Where(x => x.GetTabIndex() != null)
                        .Where(x => tabsByInventoryId.ContainsKey(x.InventoryId))
                        .Where(x => x.Categories.Contains("cards"))
                        .Select(x => new PoeSolutionItem(x, tabsByInventoryId[x.InventoryId]))
                        .OfType<IPoeSolutionItem>()
                        .ToArray();

            return new PoeTradeSolution(cards, stashUpdate.Tabs);
        }


        private static IPoeTradeSolution[] BuildChaosSetSolutions(StashUpdate stashUpdate)
        {
            var result = new List<IPoeTradeSolution>();

            var rareItems = stashUpdate.Items.GetChaosSetItems();

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
                                        rings
                                    }
                                    .Select(x => new ConcurrentQueue<IStashItem>(x))
                                    .ToArray();

            const float targetScoreForEachCategory = 1;
            const float targetScoreForSolution = 8;

            var tabsByInventoryId = stashUpdate.Tabs.ToDictionary(x => x.GetInventoryId(), x => x);

            do
            {
                var tierScore = 0f;
                var ongoingSolutionItems = new List<IPoeSolutionItem>();
                foreach (var items in itemsByCategories)
                {
                    var categoryScore = 0f;

                    IStashItem nextItem;
                    while (categoryScore < targetScoreForEachCategory && items.TryDequeue(out nextItem))
                    {
                        categoryScore += nextItem.GetTradeScore();

                        var tradeItem = new PoeSolutionItem(nextItem, tabsByInventoryId[nextItem.InventoryId]);
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
    }
}