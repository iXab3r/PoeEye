using System;
using System.ComponentModel;

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

    private void OnClosing(object sender, CancelEventArgs e)
    {
    }
}