using JetBrains.Annotations;
using PoeOracle.Models;

namespace PoeOracle.PoeDatabase
{
    internal interface ISkillGemInfoProvider {
        SkillGemModel[] KnownGems { [NotNull] get; }
    }
}