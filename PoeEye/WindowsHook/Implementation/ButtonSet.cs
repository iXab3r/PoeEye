// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System.Windows.Forms;

namespace WindowsHook.Implementation;

internal class ButtonSet
{
    public MouseButtons Values { get; private set; }

    public ButtonSet()
    {
        Values = MouseButtons.None;
    }

    public void Add(MouseButtons element)
    {
        Values |= element;
    }

    public void Remove(MouseButtons element)
    {
        Values &= ~element;
    }

    public bool Contains(MouseButtons element)
    {
        return (Values & element) != MouseButtons.None;
    }
}