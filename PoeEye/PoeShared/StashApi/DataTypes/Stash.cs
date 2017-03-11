﻿using System.Collections.Generic;
using RestSharp.Deserializers;

namespace PoeShared.StashApi.DataTypes
{
    internal class Stash : IStash
    {
        [DeserializeAs(Name = "numTabs")]
        public int NumTabs { get; set; }

        [DeserializeAs(Name = "items")]
        public List<StashItem> Items { get; set; } = new List<StashItem>();

        [DeserializeAs(Name = "tabs")]
        public List<Tab> Tabs { get; set; } = new List<Tab>();

        IEnumerable<IStashItem> IStash.Items => Items;

        IEnumerable<IStashTab> IStash.Tabs => Tabs;
    }
}