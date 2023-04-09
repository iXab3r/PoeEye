using System;

namespace PoeShared.Native;

public interface IWindowTrackerMatcher
{
    bool IsMatch(IWindowHandle window);
}