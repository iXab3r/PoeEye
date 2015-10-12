﻿namespace PoeEye.PoeTrade
{
    using Guards;

    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    internal sealed class PoeClipboardParser : IPoeClipboardParser
    {
        public IPoeQueryInfo Parse(string clipboardContent)
        {
            Guard.ArgumentNotNull(() => clipboardContent);

            return null;
        }
    }
}