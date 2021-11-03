using shortid;
using shortid.Configuration;

// ReSharper disable StringLiteralTypo

namespace PoeShared.Services
{
    internal sealed class UniqueIdGenerator : IUniqueIdGenerator
    {
        private readonly GenerationOptions defaultOptions = new()
        {
            Length = 8,
            UseSpecialCharacters = false,
            UseNumbers = false
        };
        
        public UniqueIdGenerator()
        {
            ShortId.SetCharacters(@"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }

        public string Next()
        {
            return ShortId.Generate(defaultOptions);
        }

        public string Next(int length)
        {
            return ShortId.Generate(new GenerationOptions()
            {
                Length = length,
                UseNumbers = defaultOptions.UseNumbers,
                UseSpecialCharacters = defaultOptions.UseSpecialCharacters
            });
        }
    }
}