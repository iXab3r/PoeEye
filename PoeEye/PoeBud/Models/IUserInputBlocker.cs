using System;
using JetBrains.Annotations;

namespace PoeBud.Models
{
    internal interface IUserInputBlocker
    {
        [NotNull] 
        IDisposable Block();
    }
}