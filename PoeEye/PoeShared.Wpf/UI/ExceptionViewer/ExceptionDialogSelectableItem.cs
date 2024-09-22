using System.Collections.Generic;
using PoeShared.Reporting;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public sealed class ExceptionDialogSelectableItem : DisposableReactiveObject
{
    private static readonly HashSet<string> SupportedImages = new() {".png", ".bmp", ".jpg", ".gif"};

    public ExceptionDialogSelectableItem(ExceptionReportItem item)
    {
        Item = item;
        IsChecked = item.Attached;
    }

    public ExceptionReportItem Item { get; }

    public bool IsImage => SupportedImages.Contains(Item.Attachment?.Extension ?? string.Empty);

    public bool IsChecked { get; set; } = true;
}