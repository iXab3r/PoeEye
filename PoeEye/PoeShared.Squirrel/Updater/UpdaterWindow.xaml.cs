using System;
using System.ComponentModel;
using System.Windows.Automation.Peers;

namespace PoeShared.Squirrel.Updater;

internal partial class UpdaterWindow
{
    public UpdaterWindow()
    {
        InitializeComponent();
        
        this.Closing += OnClosing;
        
        this.Closed += OnClosed;
    }

    private void OnClosed(object sender, EventArgs e)
    {
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        // Expose the native updater controls to accessibility and desktop automation.
        return new WindowAutomationPeer(this);
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
    }
}
