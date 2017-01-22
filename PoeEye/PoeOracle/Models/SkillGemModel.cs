using System;

namespace PoeOracle.Models
{
    internal sealed class SkillGemModel
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public PoeCharacterType CanBeBoughtBy { get; set; }
        
        public PoeCharacterType RewardFor { get; set; }
        
        public string SoldBy { get; set; } 

        public string QualityBonus { get; set; }

        public Uri IconUri { get; set; }
    }
}