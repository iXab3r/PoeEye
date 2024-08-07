using System.Text.RegularExpressions;

namespace PoeShared.Scaffolding;

/// <summary>
/// Implements a natural comparer for strings.
/// </summary>
public class NaturalStringComparer : IComparer<string>
{
    private static readonly Lazy<NaturalStringComparer> InstanceSupplier = new();

    public static NaturalStringComparer Instance => InstanceSupplier.Value;
    
    /// <summary>
    /// The comparer for sequences of objects.
    /// </summary>
    private static readonly EnumerableComparer<object> EnumerableOfObjectComparer = new EnumerableComparer<object>();

    /// <summary>
    /// The regular expression used to split numbers and text.
    /// </summary>
    private static readonly Regex Digits = new Regex("([0-9]+)", RegexOptions.Compiled);

    /// <summary>
    /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>
    /// A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />.Zero<paramref name="x" /> equals <paramref name="y" />.Greater than zero<paramref name="x" /> is greater than <paramref name="y" />.
    /// </returns>
    public int Compare(string x, string y)
    {
        if (x == null)
        {
            return y == null ? 0 : -1;
        }

        if (y == null)
        {
            return 1;
        }

        Func<string, object> convert = str =>
        {
            int result;
            if (int.TryParse(str, out result))
            {
                return result;
            }

            return str;
        };

        Func<string, string> removeWhiteSpace = str => str.Replace(" ", string.Empty);

        // convert to sequences of int/string
        var xitems = Digits.Split(removeWhiteSpace(x)).Select(convert);
        var yitems = Digits.Split(removeWhiteSpace(y)).Select(convert);

        // compare the sequences
        return EnumerableOfObjectComparer.Compare(xitems, yitems);
    }
}