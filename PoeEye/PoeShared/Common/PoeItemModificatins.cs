using System;

namespace PoeShared.Common {
    [Flags]
    public enum PoeItemModificatins
    {
        None = 0x0,
        Shaped = 1 << 1,
        Elder = 1 << 2,
        Crafted = 1 << 3,
        Corrupted = 1 << 4,
        Mirrored = 1 << 5,
        Unidentified = 1 << 6,
        Enchanted = 1 << 7
    }
}