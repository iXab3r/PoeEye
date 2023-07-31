using shortid;
using shortid.Configuration;

// ReSharper disable StringLiteralTypo

namespace PoeShared.Services;

internal sealed class UniqueIdGenerator : IUniqueIdGenerator
{
    private static readonly GenerationOptions IdOptions = new()
    {
        UseSpecialCharacters = false,
        UseNumbers = true,
        Length = 12
    };
        
    private readonly IClock clock;
        
    public UniqueIdGenerator(IClock clock)
    {
        this.clock = clock;
        ShortId.SetCharacters(@"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
    }

    public string Next()
    {
        return Next(true);
    }
    
    public string Next(bool includeTimestamp)
    {
        return $"{(includeTimestamp ? $"{clock.UtcNow:yyyyMMddHHmmss}" : "")}{GenerateId()}";
    }

    public string Next(string prefix)
    {
        return $"{prefix}{Next()}";
    }

    private static string GenerateId()
    {
        return ShortId.Generate(IdOptions);
    }
}