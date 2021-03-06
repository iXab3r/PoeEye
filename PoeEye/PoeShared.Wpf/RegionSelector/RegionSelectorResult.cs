using System.Drawing;
using PoeShared.Native;

namespace PoeShared.RegionSelector
{
    public sealed class RegionSelectorResult
    {
        public IWindowHandle Window { get; set; }
        
        public Rectangle Selection { get; set; }
        
        public Rectangle AbsoluteSelection { get; set; }
        
        public string Reason { get; set; }

        public bool IsValid => Selection.Width > 0 && Selection.Height > 0 && Window != null;

        public override string ToString()
        {
            return new { Window, AbsoluteSelection, Selection, Reason }.ToString();
        }
    }
}