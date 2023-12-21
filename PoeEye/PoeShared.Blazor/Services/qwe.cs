using System;
using Microsoft.Extensions.Internal;

namespace PoeShared.Blazor.Services;

internal sealed class MicrosoftExtensionsSystemClock : ISystemClock
{
    private readonly IClock clock;

    public MicrosoftExtensionsSystemClock(IClock clock)
    {
        this.clock = clock;
    }

    public DateTimeOffset UtcNow => clock.UtcNow;
}