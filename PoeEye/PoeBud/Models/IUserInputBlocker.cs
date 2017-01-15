using System;

namespace PoeBud.Models
{
    using JetBrains.Annotations;

    internal interface IUserInputBlocker
    {
        [NotNull] 
        IDisposable Block();
    }
}