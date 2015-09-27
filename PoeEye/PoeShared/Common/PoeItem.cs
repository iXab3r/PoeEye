namespace PoeShared.Common
{
    public sealed class PoeItem : IPoeItem
    {
        private IPoeItemMod[] mods = new IPoeItemMod[0];

        public string ItemName { get; set; }

        public string ItemIconUri { get; set; }

        public string TradeForumUri { get; set; }

        public string UserForumUri { get; set; }

        public string UserForumName { get; set; }

        public string UserIgn { get; set; }

        public string Price { get; set; }

        public string League { get; set; }

        public IPoeItemMod[] Mods
        {
            get { return mods; }
            set { mods = value ?? new IPoeItemMod[0]; }
        }
    }
}