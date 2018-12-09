using System;
using Guards;
using JetBrains.Annotations;
using PoeShared.StashApi.DataTypes;
using TypeConverter;

namespace PoeShared.Common
{
    public sealed class PoeItemBuilder
    {
        private readonly PoeItem additionalDetails = new PoeItem();
        [NotNull] private readonly IClock clock;
        private readonly IConverter<IStashItem, PoeItem> itemConverter;
        private readonly IConverter<string, PoePrice> priceConverter;

        private IStashItem stashItem;

        public PoeItemBuilder(
            [NotNull] IConverter<string, PoePrice> priceConverter,
            [NotNull] IConverter<IStashItem, PoeItem> itemConverter,
            [NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(itemConverter, nameof(itemConverter));
            Guard.ArgumentNotNull(priceConverter, nameof(priceConverter));
            Guard.ArgumentNotNull(clock, nameof(clock));

            this.itemConverter = itemConverter;
            this.clock = clock;
            this.priceConverter = priceConverter;
        }

        public PoeItemBuilder WithPrivateMessage(string source)
        {
            additionalDetails.SuggestedPrivateMessage = source;
            return this;
        }

        public PoeItemBuilder WithStashItem(IStashItem source)
        {
            stashItem = source;
            return this;
        }

        public PoeItemBuilder WithRawPrice(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return this;
            }

            var price = priceConverter.Convert(source);
            if (!price.IsEmpty)
            {
                additionalDetails.Price = price.ToString();
            }

            return this;
        }

        public PoeItemBuilder WithIndexationTimestamp(DateTime? source)
        {
            additionalDetails.FirstSeen = source;
            return this;
        }

        public PoeItemBuilder WithUserIgn(string source)
        {
            additionalDetails.UserIgn = source;
            return this;
        }

        public PoeItemBuilder WithUserForumName(string source)
        {
            additionalDetails.UserForumName = source;
            return this;
        }

        public PoeItemBuilder WithOnline(bool isOnline)
        {
            additionalDetails.UserIsOnline = isOnline;
            return this;
        }

        public PoeItemBuilder WithItemState(PoeTradeState state)
        {
            additionalDetails.ItemState = state;
            return this;
        }

        public PoeItemBuilder WithTimestamp(DateTime? source)
        {
            additionalDetails.Timestamp = source;
            return this;
        }

        public IPoeItem Build()
        {
            if (stashItem == null)
            {
                throw new ArgumentException("StashItem is not set");
            }

            var poeItem = itemConverter.Convert(stashItem);
            if (string.IsNullOrWhiteSpace(poeItem.Price))
            {
                poeItem.Price = additionalDetails.Price;
            }

            if (string.IsNullOrWhiteSpace(poeItem.SuggestedPrivateMessage))
            {
                poeItem.SuggestedPrivateMessage = additionalDetails.SuggestedPrivateMessage;
            }

            if (string.IsNullOrWhiteSpace(poeItem.UserIgn))
            {
                poeItem.UserIgn = additionalDetails.UserIgn;
            }

            if (string.IsNullOrWhiteSpace(poeItem.UserForumName))
            {
                poeItem.UserForumName = additionalDetails.UserForumName;
            }

            if (poeItem.FirstSeen == null)
            {
                poeItem.FirstSeen = additionalDetails.FirstSeen;
            }

            if (poeItem.Timestamp == null)
            {
                poeItem.Timestamp = additionalDetails.Timestamp ?? clock.Now;
            }

            if (!poeItem.UserIsOnline)
            {
                poeItem.UserIsOnline = additionalDetails.UserIsOnline;
            }

            if (poeItem.ItemState == PoeTradeState.Unknown)
            {
                poeItem.ItemState = additionalDetails.ItemState;
            }

            if (!string.IsNullOrWhiteSpace(poeItem.Note))
            {
                var notePrice = priceConverter.Convert(poeItem.Note);
                if (!notePrice.IsEmpty && poeItem.Price == notePrice.ToString())
                {
                    // price is already set
                    poeItem.Note = null;
                }
            }

            return poeItem;
        }
    }
}