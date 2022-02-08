using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;

namespace PoeShared.UI;

public sealed class NoopWindowAutomationPeer : FrameworkElementAutomationPeer
{
    private static readonly List<AutomationPeer> EmptyList = new();
    
    public NoopWindowAutomationPeer(FrameworkElement owner) : base(owner) { }

    protected override string GetNameCore()
    {
        return "CustomWindowAutomationPeer";
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Window;
    }

    protected override List<AutomationPeer> GetChildrenCore()
    {
        return EmptyList;
    }
}