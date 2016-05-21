using System;

namespace GlobalHotKey
{
    /// <summary>
    /// Arguments for key pressed event which contain information about pressed hot key.
    /// </summary>
    public class GKeyPressedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GKeyPressedEventArgs"/> class.
        /// </summary>
        /// <param name="hotKey">The hot key.</param>
        public GKeyPressedEventArgs(HotKey hotKey)
        {
            HotKey = hotKey;
        }

        /// <summary>
        /// Gets the hot key.
        /// </summary>
        public HotKey HotKey { get; private set; }
    }
}