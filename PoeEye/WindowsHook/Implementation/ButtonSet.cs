// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System.Windows.Forms;

namespace WindowsHook.Implementation
{
    internal class ButtonSet
    {
        private MouseButtons values;

        public ButtonSet()
        {
            values = MouseButtons.None;
        }

        public void Add(MouseButtons element)
        {
            values |= element;
        }

        public void Remove(MouseButtons element)
        {
            values &= ~element;
        }

        public bool Contains(MouseButtons element)
        {
            return (values & element) != MouseButtons.None;
        }
    }
}