using System;

namespace PoeShared.Services
{
    public interface IUserInputBlocker
    {
        IDisposable Block();
    }
}