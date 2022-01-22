using System.ComponentModel;

namespace PoeShared.Common;

public enum TriState
{
    [Browsable(false)]
    Unknown,
    Yes,
    No
}