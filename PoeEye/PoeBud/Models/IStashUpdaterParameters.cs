﻿using System.Collections.Generic;
using JetBrains.Annotations;

namespace PoeBud.Models
{
    public interface IStashUpdaterParameters
    {
        string LoginEmail { [CanBeNull] get; }
        
        string SessionId { [CanBeNull] get; }
        
        string CharacterName { [CanBeNull] get; }

        ICollection<int> StashesToProcess { [CanBeNull] get; }
    }   
}