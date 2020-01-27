using System;
using System.IO;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.Audio.Services
{
    internal interface IAudioPlayer : IDisposableReactiveObject
    {
        [NotNull]
        IDisposable Play([NotNull] Stream rawStream);
    }
}