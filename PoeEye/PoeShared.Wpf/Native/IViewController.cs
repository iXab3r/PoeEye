using System;
using System.Reactive;
using System.Runtime.CompilerServices;

namespace PoeShared.Native;

public interface IViewController
{
    void Hide();

    void Show();
}