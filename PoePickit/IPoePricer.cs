namespace PoePickit
{
    internal interface IPoePricer
    {
        void CreateTooltip(string itemData);
        void ShowToolTip(int coorX, int coorY);
        void HideToolTip();
    }
}