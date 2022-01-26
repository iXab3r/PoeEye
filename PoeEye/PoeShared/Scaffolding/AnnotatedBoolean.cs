using JetBrains.Annotations;

namespace PoeShared.Scaffolding;

public readonly struct AnnotatedBoolean : IConvertible, IEquatable<AnnotatedBoolean>
{
    public AnnotatedBoolean(bool value, string annotation)
    {
        Value = value;
        Annotation = annotation;
    }
        
    public AnnotatedBoolean(bool value, Func<bool, string> annotationSupplier)
    {
        Value = value;
        Annotation = annotationSupplier(value);
    }

    public bool Value { get; }
        
    public string Annotation { [UsedImplicitly] get; }
        
        
    public static bool operator true(AnnotatedBoolean x) => x.Value == true;
    public static bool operator false(AnnotatedBoolean x) => x.Value == false;
         
    public static AnnotatedBoolean operator &(AnnotatedBoolean a, AnnotatedBoolean b)
        => new AnnotatedBoolean(a.Value & b.Value, $"{a.Value}({a.Annotation}) && {b.Value}({b.Annotation})");
        
    public static AnnotatedBoolean operator |(AnnotatedBoolean a, AnnotatedBoolean b)
        => new AnnotatedBoolean(a.Value | b.Value, $"{a.Value}({a.Annotation}) && {b.Value}({b.Annotation})");

    public bool Equals(AnnotatedBoolean other)
    {
        return Value == other.Value && Annotation == other.Annotation;
    }

    public override bool Equals(object obj)
    {
        return obj is AnnotatedBoolean other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, Annotation);
    }

    public static bool operator ==(AnnotatedBoolean left, AnnotatedBoolean right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AnnotatedBoolean left, AnnotatedBoolean right)
    {
        return !left.Equals(right);
    }

    public TypeCode GetTypeCode()
    {
        return Value.GetTypeCode();
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

    public string ToString(IFormatProvider provider)
    {
        return Value.ToString(provider);
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

    public override string ToString()
    {
        return $"{Value}: '{Annotation}'";
    }
}