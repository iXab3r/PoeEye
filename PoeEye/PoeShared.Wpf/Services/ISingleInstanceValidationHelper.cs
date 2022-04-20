using System;

namespace PoeShared.Services;

public interface ISingleInstanceValidationHelper : IDisposable
{
    string MutexId { get; }
}