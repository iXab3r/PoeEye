namespace PoeEyeUi.PoeTrade.Common
{
    using System.ComponentModel;

    public sealed class SortDescriptionData
    {
        public SortDescriptionData(string propertyName, ListSortDirection direction)
        {
            PropertyName = propertyName;
            Direction = direction;
        }

        public string PropertyName { get; }

        public ListSortDirection Direction { get; }

        public SortDescription ToSortDescription()
        {
            return new SortDescription(PropertyName, Direction);
        }

        public override string ToString()
        {
            return $"{PropertyName} { GetDirectionDescription() }";
        }

        private string GetDirectionDescription()
        {
            return Direction == ListSortDirection.Ascending ? "Asc ↑" : "Desc ↓";
        }
    }
}