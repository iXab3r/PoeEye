namespace PoeShared.Common
{
    using JetBrains.Annotations;

    public interface IPoeItem
    {
        string ItemName { get; }

        string ItemIconUri { get; }

        string TradeForumUri { get; }

        string UserForumUri { get; }

        string UserForumName { get; }

        string UserIgn { get; }

        string Price { get; }

        string League { get; }

        IPoeItemMod[] Mods { [NotNull] get; }

        IPoeLinksInfo Links { get; }
    }
}