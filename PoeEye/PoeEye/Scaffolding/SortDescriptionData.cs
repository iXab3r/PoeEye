using System.ComponentModel;

namespace PoeEye.Scaffolding
{
    public struct SortDescriptionData
    {
        public static readonly SortDescriptionData Empty = new SortDescriptionData();

        public SortDescriptionData(string propertyName, ListSortDirection direction)
        {
            PropertyName = propertyName;
            Direction = direction;
        }

        public string PropertyName { get; }

        public ListSortDirection Direction { get; }

        public bool IsEmpty => string.IsNullOrWhiteSpace(PropertyName);

        public override string ToString()
        {
            return $"{PropertyName} {GetDirectionDescription()}";
        }

        private string GetDirectionDescription()
        {
            return Direction == ListSortDirection.Ascending ? "Asc ↑" : "Desc ↓";
        }
    }
}