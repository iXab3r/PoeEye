using MahApps.Metro.Controls;
using PoeShared.UI;

namespace PoeShared.Native;

public class FastMetroWindow : MetroWindow
{
    /// <summary>
    /// https://stackoverflow.com/questions/17297539/can-ui-automation-be-disabled-for-an-entire-wpf-4-0-app
    /// https://stackoverflow.com/questions/6362367/wpf-ui-automation-issue
    /// https://stackoverflow.com/questions/5716078/wpf-performance-issue-due-to-ui-automation
    /// </summary>
    /// <returns></returns>
    protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
    {
        return new NoopWindowAutomationPeer(this);
    }
}