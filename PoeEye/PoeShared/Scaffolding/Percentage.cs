namespace PoeShared.Scaffolding;

/// <summary>
/// Represents a percentage value (0-100).
/// Provides utility methods for conversion and scaling.
/// </summary>
public readonly struct Percentage : IEquatable<Percentage>
{
    private readonly float value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Percentage"/> struct.
    /// </summary>
    /// <param name="value">The percentage value (0 to 100).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is out of range.</exception>
    public Percentage(float value)
    {
        this.value = value;
    }

    /// <summary>
    /// Returns the percentage value as a float (0-100).
    /// </summary>
    public float Value => value;

    /// <summary>
    /// Converts the percentage to a decimal value (0.0 to 1.0).
    /// </summary>
    public float ToDecimal() => value / 100f;

    /// <summary>
    /// Converts a decimal value (0.0 to 1.0) to a percentage.
    /// </summary>
    public static Percentage FromDecimal(float decimalValue)
    {
        return new Percentage(decimalValue * 100);
    }

    /// <summary>
    /// Implicit conversion from <see cref="float"/> to <see cref="Percentage"/>.
    /// </summary>
    public static implicit operator Percentage(float value) => new Percentage(value);

    /// <summary>
    /// Implicit conversion from <see cref="Percentage"/> to <see cref="float"/>.
    /// </summary>
    public static implicit operator float(Percentage percentage) => percentage.value;

    /// <summary>
    /// Multiplies a value by this percentage.
    /// </summary>
    public float ApplyTo(float target) => target * ToDecimal();

    /// <inheritdoc/>
    public bool Equals(Percentage other) => value.Equals(other.value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Percentage other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => value.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => $"{value}%";

    public static bool operator ==(Percentage left, Percentage right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Percentage left, Percentage right)
    {
        return !(left == right);
    }
}