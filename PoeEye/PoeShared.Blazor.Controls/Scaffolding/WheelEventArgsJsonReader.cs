using System.Text.Json;
using Microsoft.AspNetCore.Components.Web;

namespace PoeShared.Blazor.Controls.Scaffolding;

public static class WheelEventArgsJsonReader
{
    private static readonly JsonEncodedText DeltaX = JsonEncodedText.Encode("deltaX");
    private static readonly JsonEncodedText DeltaY = JsonEncodedText.Encode("deltaY");
    private static readonly JsonEncodedText DeltaZ = JsonEncodedText.Encode("deltaZ");
    private static readonly JsonEncodedText DeltaMode = JsonEncodedText.Encode("deltaMode");

    public static WheelEventArgs Read(JsonElement jsonElement)
    {
        var eventArgs = new WheelEventArgs();

        foreach (var property in jsonElement.EnumerateObject())
        {
            if (property.NameEquals(DeltaX.EncodedUtf8Bytes))
            {
                eventArgs.DeltaX = property.Value.GetDouble();
            }
            else if (property.NameEquals(DeltaY.EncodedUtf8Bytes))
            {
                eventArgs.DeltaY = property.Value.GetDouble();
            }
            else if (property.NameEquals(DeltaZ.EncodedUtf8Bytes))
            {
                eventArgs.DeltaZ = property.Value.GetDouble();
            }
            else if (property.NameEquals(DeltaMode.EncodedUtf8Bytes))
            {
                eventArgs.DeltaMode = property.Value.GetInt64();
            }
            else
            {
                MouseEventArgsJsonReader.ReadProperty(eventArgs, property);
            }
        }

        return eventArgs;
    }
}