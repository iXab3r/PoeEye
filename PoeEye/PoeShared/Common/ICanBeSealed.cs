namespace PoeShared.Common;

public interface ICanBeSealed
{
    /// <summary>
    /// Seal the current instance 
    /// </summary>
    void Seal();

    /// <summary>
    /// Is the current instance sealed
    /// </summary>
    bool IsSealed
    {
        get;
    }
}