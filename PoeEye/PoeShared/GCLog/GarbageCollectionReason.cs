namespace PoeShared.GCLog;

internal enum GarbageCollectionReason
{
    AllocSmall,
    Induced,
    LowMemory,
    Empty,
    AllocLarge,
    OutOfSpaceSoh,
    OutOfSpaceLoh,
    InducedNotForced,
    Internal,
    InducedLowMemory
}