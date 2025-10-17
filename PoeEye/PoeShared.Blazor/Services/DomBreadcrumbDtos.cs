using System.Collections.Generic;

namespace PoeShared.Blazor.Services;

/// <summary>
/// Rectangle info for a DOM element relative to the viewport.
/// </summary>
public sealed record HtmlDomRect(double X, double Y, double Width, double Height);

/// <summary>
/// A single node in a DOM breadcrumb path.
/// </summary>
public sealed record HtmlElementBreadcrumbNode
{
    public string Tag { get; init; }
    public string Id { get; init; }
    public string[] ClassList { get; init; }
    public Dictionary<string, string> Attributes { get; init; }
    public int Depth { get; init; }
}

/// <summary>
/// Describes an element at a specific point along with its ancestors.
/// </summary>
public sealed record HtmlElementBreadcrumb
{
    /// <summary>
    /// Zero-based index of this element in the hit-test stack (0 is the topmost element returned by elementsFromPoint).
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Bounding client rect of the element.
    /// </summary>
    public HtmlDomRect Rect { get; init; }

    public string Tag => Node.Tag;
    
    public string Id => Node.Id;
    
    public string[] ClassList => Node.ClassList;
    
    public Dictionary<string, string> Attributes => Node.Attributes;
    
    public int Depth => Node.Depth;

    /// <summary>
    /// The element itself summarized as a node.
    /// </summary>
    public HtmlElementBreadcrumbNode Node { get; init; }

    /// <summary>
    /// Breadcrumb path starting at the element and going up through ancestors (element first).
    /// </summary>
    public HtmlElementBreadcrumbNode[] Path { get; init; }
}
