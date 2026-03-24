namespace PoeShared.Blazor.Wpf.Automation;

public enum BlazorWindowViewRole
{
    Body = 0,
    TitleBar = 1
}

internal static class BlazorWindowViewRoleExtensions
{
    public static string ToAutomationSegment(this BlazorWindowViewRole role)
    {
        return role switch
        {
            BlazorWindowViewRole.Body => "body",
            BlazorWindowViewRole.TitleBar => "titlebar",
            _ => role.ToString().ToLowerInvariant()
        };
    }
}
