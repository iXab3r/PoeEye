using System;

namespace PoeShared.UI;

public interface IUserInputBlocker
{
    IDisposable Block(UserInputBlockType inputBlockType);
}