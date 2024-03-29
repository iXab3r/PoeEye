﻿namespace PoeShared.Dialogs.ViewModels;

public sealed record MessageBoxElement
{
    public string Caption { get; set; }
        
    public object Value { get; set; }
    
    public bool IsDefault { get; set; }
        
    public static MessageBoxElement Close = new MessageBoxElement() { Caption = "Close", IsDefault = true};
    public static MessageBoxElement Cancel = new MessageBoxElement() { Caption = "Cancel", IsDefault = true};
    public static MessageBoxElement Ok = new MessageBoxElement() { Caption = "OK" };
    public static MessageBoxElement Yes = new MessageBoxElement() { Caption = "Yes" };
    public static MessageBoxElement No = new MessageBoxElement() { Caption = "No" };
}