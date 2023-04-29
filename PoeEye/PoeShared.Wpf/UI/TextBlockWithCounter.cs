using System.Threading;
using System.Windows.Controls;

namespace PoeShared.UI;

public sealed class TextBlockWithCounter : TextBlock
{
    public TextBlockWithCounter()
    {
        Count++;
        Thread.Sleep(100);
        Text = $"Block #{Count}";
    }

    public static int Count;
}