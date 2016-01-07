namespace PoeEyeUi.PoeTrade.ViewModels
{
    internal interface IPoeModViewModel
    {
        float? Max { get; set; }

        float? Min { get; set; }

        string SelectedMod { get; set; }
    }
}