namespace PoeShared.Native;

public interface ICanBeSelected : IHasSelected
{
    new bool IsSelected { get; set; }
}