namespace PoeShared.Blazor.Wpf;

/// <summary>
/// Optional window capability which constrains interactive and single-dimension resizing to a target aspect ratio.
/// Full rectangle/size assignments remain explicit and are not rewritten.
/// </summary>
public interface IWindowAspectRatioController
{
    /// <summary>
    /// Gets or sets the desired width-to-height ratio. A positive value enables the constraint;
    /// <c>null</c>, zero, negative and non-finite values disable it.
    /// </summary>
    double? TargetAspectRatio { get; set; }
}
