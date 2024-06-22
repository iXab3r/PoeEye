namespace PoeShared.Scaffolding;

/// <summary>
/// Represents a value of type T annotated with a string, providing additional context or information about the value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public readonly struct Annotated<T> : IConvertible, IEquatable<Annotated<T>>
{
    /// <summary>
    /// Initializes a new instance of the AnnotatedValue struct with the specified value and annotation.
    /// </summary>
    /// <param name="value">The value of type T.</param>
    /// <param name="annotation">The annotation providing additional context for the value.</param>
    public Annotated(T value, string annotation)
    {
        Value = value;
        Annotation = annotation;
    }

    /// <summary>
    /// Initializes a new instance of the AnnotatedValue struct with the specified value and a function to generate the annotation.
    /// </summary>
    /// <param name="value">The value of type T.</param>
    /// <param name="annotationSupplier">The function to generate the annotation based on the value.</param>
    public Annotated(T value, Func<T, string> annotationSupplier)
    {
        Value = value;
        Annotation = annotationSupplier(value);
    }

    /// <summary>
    /// Initializes a new instance of the AnnotatedValue struct with the specified value and no annotation.
    /// </summary>
    /// <param name="value">The value of type T.</param>
    public Annotated(T value) : this(value, annotation: null)
    {
    }

    /// <summary>
    /// Gets the value of type T.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the annotation associated with the value.
    /// </summary>
    public string Annotation { get; }

    public bool Equals(Annotated<T> other)
    {
        return EqualityComparer<T>.Default.Equals(Value, other.Value) && string.Equals(Annotation, other.Annotation, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is Annotated<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Annotation);
    }

    public static bool operator ==(Annotated<T> left, Annotated<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Annotated<T> left, Annotated<T> right)
    {
        return !left.Equals(right);
    }

    public TypeCode GetTypeCode()
    {
        return ((IConvertible)Value).GetTypeCode();
    }

    public bool ToBoolean(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToBoolean(provider);
    }

    public byte ToByte(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToByte(provider);
    }

    public char ToChar(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToChar(provider);
    }

    public DateTime ToDateTime(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToDateTime(provider);
    }

    public decimal ToDecimal(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToDecimal(provider);
    }

    public double ToDouble(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToDouble(provider);
    }

    public short ToInt16(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToInt16(provider);
    }

    public int ToInt32(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToInt32(provider);
    }

    public long ToInt64(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToInt64(provider);
    }

    public sbyte ToSByte(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToSByte(provider);
    }

    public float ToSingle(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToSingle(provider);
    }

    public object ToType(Type conversionType, IFormatProvider provider)
    {
        return ((IConvertible)Value).ToType(conversionType, provider);
    }

    public ushort ToUInt16(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToUInt16(provider);
    }

    public uint ToUInt32(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToUInt32(provider);
    }

    public ulong ToUInt64(IFormatProvider provider)
    {
        return ((IConvertible)Value).ToUInt64(provider);
    }
    
    public string ToString(IFormatProvider provider)
    {
        return ToString();
    }

    public override string ToString()
    {
        var valueAsString = Value == null ? "null" : Value.ToString();
        if (!string.IsNullOrEmpty(Annotation))
        {
            return $"{valueAsString}: {Annotation}";
        }
        else
        {
            return valueAsString;
        }
    }
}