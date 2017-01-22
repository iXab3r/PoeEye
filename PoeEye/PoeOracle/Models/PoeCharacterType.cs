using System;

namespace PoeOracle.Models
{
    [Flags]
    internal enum PoeCharacterType
    {
        Unknown = 0,
        Scion = 1 << 0,
        Marauder = 1 << 1,
        Witch = 1 << 2,
        Shadow = 1 << 3,
        Templar = 1 << 4,
        Duelist = 1 << 5,
        Ranger = 1 << 6
    }
}