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
        return $"{clock.UtcNow:yyyyMMddHHmmss}{GenerateId()}";
    }

    private static string GenerateId()
    {
        return ShortId.Generate(IdOptions);
    }
}