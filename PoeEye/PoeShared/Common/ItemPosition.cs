using System.Text;

namespace PoeShared.Common
{
    public struct ItemPosition
    {
        public ItemPosition(int x, int y) : this(x, y, 0, 0)
        {
        }

        public ItemPosition(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public bool IsEmpty => X == 0 && Y == 0 && Width == 0 && Height == 0;

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "Empty";
            }

            var result = new StringBuilder();
            if (X != 0)
            {
                result.Append($"{nameof(X)}: {X}");
            }

            if (Y != 0)
            {
                result.Append($"{nameof(Y)}: {Y}");
            }

            if (Width != 0)
            {
                result.Append($"{nameof(Width)}: {Width}");
            }

            if (Height != 0)
            {
                result.Append($"{nameof(Height)}: {Height}");
            }

            return result.ToString();
        }
    }
}