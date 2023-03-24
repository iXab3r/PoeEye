namespace PoeShared.Common;

public interface ICanBeSelected : IHasSelected
{
    new bool IsSelected { get; set; }
}