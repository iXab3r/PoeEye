using System;
using Newtonsoft.Json;
using PoeShared.StashApi.DataTypes;

namespace PoeEye.PathOfExileTrade.TradeApi.Domain
{
    public static class JsonFetchRequest
    {
        public partial class Response
        {
            [JsonProperty("result")]
            public ResultListing[] Listings { get; set; }
        }

        public partial class ResultListing
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("listing")]
            public ListingDetails Listing { get; set; }

            [JsonProperty("item")]
            public StashItem Item { get; set; }

            [JsonProperty("gone", NullValueHandling = NullValueHandling.Ignore)]
            public bool? Gone { get; set; }
        }
        
        public partial class ListingDetails
        {
            [JsonProperty("method")]
            public string Method { get; set; }

            [JsonProperty("indexed")]
            public DateTime Indexed { get; set; }

            [JsonProperty("stash")]
            public JsonListingStash Stash { get; set; }

            [JsonProperty("whisper")]
            public string Whisper { get; set; }

            [JsonProperty("account")]
            public Account Account { get; set; }

            [JsonProperty("price")]
            public Price Price { get; set; }
        }
    
        public partial class JsonListingStash
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("x")]
            public long X { get; set; }

            [JsonProperty("y")]
            public long Y { get; set; }
        }
    
        public partial class AccountOnline
        {
            [JsonProperty("league")]
            public string League { get; set; }
        }

        public partial class Price
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("amount")]
            public long Amount { get; set; }

            [JsonProperty("currency")]
            public string Currency { get; set; }

            public override string ToString()
            {
                return $"{Type} {Amount} {Currency}";
            }
        }
    
        public partial class Account
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("lastCharacterName")]
            public string LastCharacterName { get; set; }

            [JsonProperty("online")]
            public AccountOnline Online { get; set; }

            [JsonProperty("language")]
            public string Language { get; set; }
        }

    }
}