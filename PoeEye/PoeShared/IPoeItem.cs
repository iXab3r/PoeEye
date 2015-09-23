namespace PoeShared
{
    public interface IPoeItem
    {
        string ItemName { get; set; }

        string ItemIconUri { get; set; }

        string TradeForumUri { get; set; }

        string UserForumUri { get; set; }

        string UserForumName { get; set; }

        string UserIgn { get; set; }

        string Price { get; set; }

        string League { get; set; }
    }
}