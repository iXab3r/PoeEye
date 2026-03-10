namespace PoeShared.UI.WinForms;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.TableLayoutPanel rootLayout;
    private System.Windows.Forms.FlowLayoutPanel controlPanel;
    private System.Windows.Forms.Label viewTypeLabel;
    private System.Windows.Forms.ComboBox viewTypeComboBox;
    private System.Windows.Forms.Button replaceContentButton;
    private System.Windows.Forms.Button incrementCountButton;
    private System.Windows.Forms.Button reloadHostButton;
    private System.Windows.Forms.Button devToolsButton;
    private System.Windows.Forms.Button openWindowButton;
    private System.Windows.Forms.Button openDialogWindowButton;
    private System.Windows.Forms.Button closeAllWindowsButton;
    private System.Windows.Forms.CheckBox shareContentCheckBox;
    private System.Windows.Forms.Label statusLabel;
    private System.Windows.Forms.Panel hostPanel;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
        this.controlPanel = new System.Windows.Forms.FlowLayoutPanel();
        this.viewTypeLabel = new System.Windows.Forms.Label();
        this.viewTypeComboBox = new System.Windows.Forms.ComboBox();
        this.replaceContentButton = new System.Windows.Forms.Button();
        this.incrementCountButton = new System.Windows.Forms.Button();
        this.reloadHostButton = new System.Windows.Forms.Button();
        this.devToolsButton = new System.Windows.Forms.Button();
        this.openWindowButton = new System.Windows.Forms.Button();
        this.openDialogWindowButton = new System.Windows.Forms.Button();
        this.closeAllWindowsButton = new System.Windows.Forms.Button();
        this.shareContentCheckBox = new System.Windows.Forms.CheckBox();
        this.statusLabel = new System.Windows.Forms.Label();
        this.hostPanel = new System.Windows.Forms.Panel();
        this.rootLayout.SuspendLayout();
        this.controlPanel.SuspendLayout();
        this.SuspendLayout();
        // 
        // rootLayout
        // 
        this.rootLayout.ColumnCount = 1;
        this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.rootLayout.Controls.Add(this.controlPanel, 0, 0);
        this.rootLayout.Controls.Add(this.hostPanel, 0, 1);
        this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this.rootLayout.Location = new System.Drawing.Point(0, 0);
        this.rootLayout.Name = "rootLayout";
        this.rootLayout.RowCount = 2;
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.rootLayout.Size = new System.Drawing.Size(1280, 800);
        this.rootLayout.TabIndex = 0;
        // 
        // controlPanel
        // 
        this.controlPanel.AutoSize = true;
        this.controlPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
        this.controlPanel.Controls.Add(this.viewTypeLabel);
        this.controlPanel.Controls.Add(this.viewTypeComboBox);
        this.controlPanel.Controls.Add(this.replaceContentButton);
        this.controlPanel.Controls.Add(this.incrementCountButton);
        this.controlPanel.Controls.Add(this.reloadHostButton);
        this.controlPanel.Controls.Add(this.devToolsButton);
        this.controlPanel.Controls.Add(this.openWindowButton);
        this.controlPanel.Controls.Add(this.openDialogWindowButton);
        this.controlPanel.Controls.Add(this.closeAllWindowsButton);
        this.controlPanel.Controls.Add(this.shareContentCheckBox);
        this.controlPanel.Controls.Add(this.statusLabel);
        this.controlPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.controlPanel.Location = new System.Drawing.Point(12, 12);
        this.controlPanel.Margin = new System.Windows.Forms.Padding(12);
        this.controlPanel.Name = "controlPanel";
        this.controlPanel.Size = new System.Drawing.Size(1256, 33);
        this.controlPanel.TabIndex = 0;
        this.controlPanel.WrapContents = false;
        // 
        // viewTypeLabel
        // 
        this.viewTypeLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this.viewTypeLabel.AutoSize = true;
        this.viewTypeLabel.Location = new System.Drawing.Point(3, 9);
        this.viewTypeLabel.Name = "viewTypeLabel";
        this.viewTypeLabel.Size = new System.Drawing.Size(57, 15);
        this.viewTypeLabel.TabIndex = 0;
        this.viewTypeLabel.Text = "View type";
        // 
        // viewTypeComboBox
        // 
        this.viewTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.viewTypeComboBox.FormattingEnabled = true;
        this.viewTypeComboBox.Location = new System.Drawing.Point(66, 3);
        this.viewTypeComboBox.Name = "viewTypeComboBox";
        this.viewTypeComboBox.Size = new System.Drawing.Size(180, 23);
        this.viewTypeComboBox.TabIndex = 1;
        // 
        // replaceContentButton
        // 
        this.replaceContentButton.AutoSize = true;
        this.replaceContentButton.Location = new System.Drawing.Point(252, 3);
        this.replaceContentButton.Name = "replaceContentButton";
        this.replaceContentButton.Size = new System.Drawing.Size(104, 25);
        this.replaceContentButton.TabIndex = 2;
        this.replaceContentButton.Text = "Replace Content";
        this.replaceContentButton.UseVisualStyleBackColor = true;
        // 
        // incrementCountButton
        // 
        this.incrementCountButton.AutoSize = true;
        this.incrementCountButton.Location = new System.Drawing.Point(362, 3);
        this.incrementCountButton.Name = "incrementCountButton";
        this.incrementCountButton.Size = new System.Drawing.Size(113, 25);
        this.incrementCountButton.TabIndex = 3;
        this.incrementCountButton.Text = "Increment Count";
        this.incrementCountButton.UseVisualStyleBackColor = true;
        // 
        // reloadHostButton
        // 
        this.reloadHostButton.AutoSize = true;
        this.reloadHostButton.Location = new System.Drawing.Point(481, 3);
        this.reloadHostButton.Name = "reloadHostButton";
        this.reloadHostButton.Size = new System.Drawing.Size(91, 25);
        this.reloadHostButton.TabIndex = 4;
        this.reloadHostButton.Text = "Reload Host";
        this.reloadHostButton.UseVisualStyleBackColor = true;
        // 
        // devToolsButton
        // 
        this.devToolsButton.AutoSize = true;
        this.devToolsButton.Location = new System.Drawing.Point(578, 3);
        this.devToolsButton.Name = "devToolsButton";
        this.devToolsButton.Size = new System.Drawing.Size(102, 25);
        this.devToolsButton.TabIndex = 5;
        this.devToolsButton.Text = "Open DevTools";
        this.devToolsButton.UseVisualStyleBackColor = true;
        // 
        // openWindowButton
        // 
        this.openWindowButton.AutoSize = true;
        this.openWindowButton.Location = new System.Drawing.Point(686, 3);
        this.openWindowButton.Name = "openWindowButton";
        this.openWindowButton.Size = new System.Drawing.Size(95, 25);
        this.openWindowButton.TabIndex = 6;
        this.openWindowButton.Text = "Open Window";
        this.openWindowButton.UseVisualStyleBackColor = true;
        // 
        // openDialogWindowButton
        // 
        this.openDialogWindowButton.AutoSize = true;
        this.openDialogWindowButton.Location = new System.Drawing.Point(787, 3);
        this.openDialogWindowButton.Name = "openDialogWindowButton";
        this.openDialogWindowButton.Size = new System.Drawing.Size(127, 25);
        this.openDialogWindowButton.TabIndex = 7;
        this.openDialogWindowButton.Text = "Open Modal Window";
        this.openDialogWindowButton.UseVisualStyleBackColor = true;
        // 
        // closeAllWindowsButton
        // 
        this.closeAllWindowsButton.AutoSize = true;
        this.closeAllWindowsButton.Location = new System.Drawing.Point(920, 3);
        this.closeAllWindowsButton.Name = "closeAllWindowsButton";
        this.closeAllWindowsButton.Size = new System.Drawing.Size(124, 25);
        this.closeAllWindowsButton.TabIndex = 8;
        this.closeAllWindowsButton.Text = "Close All Windows";
        this.closeAllWindowsButton.UseVisualStyleBackColor = true;
        // 
        // shareContentCheckBox
        // 
        this.shareContentCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this.shareContentCheckBox.AutoSize = true;
        this.shareContentCheckBox.Location = new System.Drawing.Point(1050, 6);
        this.shareContentCheckBox.Name = "shareContentCheckBox";
        this.shareContentCheckBox.Size = new System.Drawing.Size(127, 19);
        this.shareContentCheckBox.TabIndex = 9;
        this.shareContentCheckBox.Text = "Share host content";
        this.shareContentCheckBox.UseVisualStyleBackColor = true;
        // 
        // statusLabel
        // 
        this.statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this.statusLabel.AutoSize = true;
        this.statusLabel.Location = new System.Drawing.Point(1183, 9);
        this.statusLabel.Name = "statusLabel";
        this.statusLabel.Size = new System.Drawing.Size(39, 15);
        this.statusLabel.TabIndex = 10;
        this.statusLabel.Text = "Ready";
        // 
        // hostPanel
        // 
        this.hostPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.hostPanel.Location = new System.Drawing.Point(12, 69);
        this.hostPanel.Margin = new System.Windows.Forms.Padding(12);
        this.hostPanel.Name = "hostPanel";
        this.hostPanel.Size = new System.Drawing.Size(1256, 719);
        this.hostPanel.TabIndex = 1;
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1280, 800);
        this.Controls.Add(this.rootLayout);
        this.Name = "Form1";
        this.Text = "PoeShared.UI.WinForms";
        this.rootLayout.ResumeLayout(false);
        this.rootLayout.PerformLayout();
        this.controlPanel.ResumeLayout(false);
        this.controlPanel.PerformLayout();
        this.ResumeLayout(false);
    }

    #endregion
}
