namespace PoeShared.Blazor.Wpf;

internal enum TrackedPropertyUpdateSource
{
    /// <summary>
    /// A value which must not enqueue the property's automatic native command: native readback,
    /// a derived value owned by another property request, or part of a composite operation which
    /// explicitly owns its native command. The holder raises the property notification immediately.
    /// </summary>
    Internal,

    /// <summary>
    /// A scalar public property request whose authoritative notification comes from the Fody-woven
    /// setter. Native-backed properties may also route this origin to an automatic native command.
    /// </summary>
    External
}
