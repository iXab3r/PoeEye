namespace PoeShared.Dialogs.ViewModels
{
    public sealed record MessageBoxElement
    {
        public string Caption { get; set; }
        
        public object Value { get; set; }
        
        public static MessageBoxElement Close = new MessageBoxElement() { Caption = "Close" };
        public static MessageBoxElement Cancel = new MessageBoxElement() { Caption = "Cancel" };
        public static MessageBoxElement Yes = new MessageBoxElement() { Caption = "Yes" };
        public static MessageBoxElement No = new MessageBoxElement() { Caption = "No" };
    }
}