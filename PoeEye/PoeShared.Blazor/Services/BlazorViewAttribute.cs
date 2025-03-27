using System;

namespace PoeShared.Blazor.Services;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class BlazorViewAttribute : Attribute
{
    public bool IsForManualRegistrationOnly { get; set; }
    
    public object ViewTypeKey { get; set; }
    
    /// <summary>
    /// Used to build relation View<>DataContext, if not set will be inferred from BlazorReactiveComponent<T>
    /// </summary>
    public Type DataContextType { get; set; }
}