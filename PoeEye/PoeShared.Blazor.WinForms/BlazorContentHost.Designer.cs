#nullable enable
using System.ComponentModel;
using System.Windows.Forms;

namespace PoeShared.Blazor.WinForms;

partial class BlazorContentHost
{
    private IContainer? components;
    private Panel contentPanel = null!;
    private Panel webViewPanel = null!;
    private Panel errorPanel = null!;
    private Panel unavailablePanel = null!;
    private Label errorTitleLabel = null!;
    private TextBox errorDetailsTextBox = null!;
    private FlowLayoutPanel errorActionsPanel = null!;
    private Button recoverButton = null!;
    private Label unavailableLabel = null!;
    private ProgressBar progressBar = null!;

    private void InitializeComponent()
    {
        components = new Container();
        contentPanel = new Panel();
        webViewPanel = new Panel();
        errorPanel = new Panel();
        errorTitleLabel = new Label();
        errorActionsPanel = new FlowLayoutPanel();
        recoverButton = new Button();
        errorDetailsTextBox = new TextBox();
        unavailablePanel = new Panel();
        unavailableLabel = new Label();
        progressBar = new ProgressBar();

        SuspendLayout();

        progressBar.Dock = DockStyle.Bottom;
        progressBar.Height = 4;
        progressBar.MarqueeAnimationSpeed = 30;
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.Visible = false;

        contentPanel.Dock = DockStyle.Fill;

        webViewPanel.Dock = DockStyle.Fill;

        errorPanel.Dock = DockStyle.Fill;
        errorPanel.Padding = new Padding(12);

        errorTitleLabel.AutoSize = true;
        errorTitleLabel.Dock = DockStyle.Top;
        errorTitleLabel.ForeColor = System.Drawing.Color.IndianRed;
        errorTitleLabel.Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold);

        errorActionsPanel.AutoSize = true;
        errorActionsPanel.Dock = DockStyle.Top;
        errorActionsPanel.Padding = new Padding(0, 8, 0, 8);
        errorActionsPanel.WrapContents = false;

        recoverButton.AutoSize = true;
        recoverButton.Text = "Try to recover";
        errorActionsPanel.Controls.Add(recoverButton);

        errorDetailsTextBox.Dock = DockStyle.Fill;
        errorDetailsTextBox.Multiline = true;
        errorDetailsTextBox.ReadOnly = true;
        errorDetailsTextBox.ScrollBars = ScrollBars.Vertical;

        errorPanel.Controls.Add(errorDetailsTextBox);
        errorPanel.Controls.Add(errorActionsPanel);
        errorPanel.Controls.Add(errorTitleLabel);

        unavailablePanel.Dock = DockStyle.Fill;
        unavailablePanel.Padding = new Padding(12);

        unavailableLabel.AutoSize = true;
        unavailableLabel.Text = "WebView2 is not installed";
        unavailablePanel.Controls.Add(unavailableLabel);

        contentPanel.Controls.Add(errorPanel);
        contentPanel.Controls.Add(unavailablePanel);
        contentPanel.Controls.Add(webViewPanel);

        Controls.Add(contentPanel);
        Controls.Add(progressBar);

        Dock = DockStyle.Fill;
        TabStop = false;
        ResumeLayout(performLayout: true);
    }
}
