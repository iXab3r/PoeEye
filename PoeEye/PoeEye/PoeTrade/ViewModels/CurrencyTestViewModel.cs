﻿using System;
using System.Collections.Generic;
using Guards;
using PoeShared;
using PoeShared.Common;
using ReactiveUI;

namespace PoeEye.PoeTrade.ViewModels
{
    public sealed class CurrencyTestViewModel
    {
        public CurrencyTestViewModel(IRandomNumberGenerator rng)
        {
            Guard.ArgumentNotNull(rng, nameof(rng));
            var currencyByAlias = new Dictionary<string, List<string>>();

            foreach (var kvp in KnownCurrencyNameList.CurrencyByAlias)
            {
                var currency = kvp.Value;

                var aliases = currencyByAlias.ContainsKey(currency)
                    ? currencyByAlias[currency]
                    : currencyByAlias[currency] = new List<string>();

                aliases.Add(kvp.Key);
            }

            foreach (var currency in KnownCurrencyNameList.EnumerateKnownCurrencies())
            {
                var price = new PoePrice(currency, rng.Next(1, 10));
                var aliases = currencyByAlias.ContainsKey(price.CurrencyType)
                    ? currencyByAlias[price.CurrencyType].ToArray()
                    : new string[0];
                Items.Add(new CurrencyInfo(price, aliases));
            }

            Items.Sort((a, b) => string.Compare(a.Price.CurrencyType, b.Price.CurrencyType, StringComparison.OrdinalIgnoreCase));
        }

        public IReactiveList<CurrencyInfo> Items { get; } = new ReactiveList<CurrencyInfo>();
    }

    public struct CurrencyInfo
    {
        public PoePrice Price { get; }
        public string[] Aliases { get; }

        public string AliasesList => string.Join(", ", Aliases);

        public CurrencyInfo(PoePrice price, string[] aliases)
        {
            Price = price;
            Aliases = aliases;
        }
    }
}