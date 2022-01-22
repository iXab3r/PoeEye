namespace PoeShared.Prism;

public interface IConverter<in TSource, out TTarget> : IConverter
{
    /// <summary>
    ///     Converts the given value of type TSource into an object of type TTarget.
    /// </summary>
    /// <param name="value">The source value to be converted.</param>
    TTarget Convert(TSource value);
}
    
public interface IConverter
{
}