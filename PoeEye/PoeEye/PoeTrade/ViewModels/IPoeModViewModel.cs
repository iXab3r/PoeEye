using JetBrains.Annotations;
using WpfAutoCompleteControls.Editors;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeModViewModel
    {
        float? Max { get; set; }

        float? Min { get; set; }

        string SelectedMod { get; set; }

        bool IsEmpty { get; }

        void Reset();
        
        ISuggestionProvider SuggestionProvider { [CanBeNull] get; [CanBeNull] set; } 
    }
}