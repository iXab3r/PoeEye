using System;

namespace PoeShared.Blazor.Services;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class BlazorViewAttribute : Attribute
{
    public bool IsForManualRegistrationOnly { get; set; }
    
    public object ViewKey { get; set; }
}