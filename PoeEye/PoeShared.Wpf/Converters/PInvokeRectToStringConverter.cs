using System;
using PInvoke;

namespace PoeShared.Converters;

public class PInvokeRectToStringConverter : LambdaConverterBase<RECT, string>
{
    private static readonly Lazy<PInvokeRectToStringConverter> InstanceSupplier = new();
    public static PInvokeRectToStringConverter Instance => InstanceSupplier.Value;
    
    protected override string Convert(RECT input)
    {
        return $"L:{input.left},T:{input.top},R:{input.right},B:{input.bottom}";
    }

    protected override RECT ConvertBack(string input)
    {
        throw new System.NotSupportedException();
    }
}