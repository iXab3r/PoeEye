namespace WpfAwesomium
{
    using System;

    public sealed class DocumentLoadedEventArgs : EventArgs
    {
        public Uri Uri { get; set; }

        public string Value { get; set; }
    }
}