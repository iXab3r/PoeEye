using System.Drawing;
using System.Windows.Forms;

namespace PoePricer
{
    partial class TtForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LabelLeft = new System.Windows.Forms.Label();
            this.LabelRight = new System.Windows.Forms.Label();
            this.eventLog1 = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.eventLog1)).BeginInit();
            this.SuspendLayout();
            // 
            // LabelLeft
            // 
            this.LabelLeft.AutoSize = true;
            this.LabelLeft.Location = new System.Drawing.Point(12, 9);
            this.LabelLeft.Margin = new System.Windows.Forms.Padding(3, 0, 3, 9);
            this.LabelLeft.Name = "LabelLeft";
            this.LabelLeft.Size = new System.Drawing.Size(21, 13);
            this.LabelLeft.TabIndex = 3;
            this.LabelLeft.Text = "left";
            this.LabelLeft.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LabelRight
            // 
            this.LabelRight.AutoSize = true;
            this.LabelRight.BackColor = System.Drawing.Color.Transparent;
            this.LabelRight.Location = new System.Drawing.Point(94, 9);
            this.LabelRight.Margin = new System.Windows.Forms.Padding(3, 0, 3, 9);
            this.LabelRight.Name = "LabelRight";
            this.LabelRight.Size = new System.Drawing.Size(27, 13);
            this.LabelRight.TabIndex = 4;
            this.LabelRight.Text = "right";
            this.LabelRight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // eventLog1
            // 
            this.eventLog1.SynchronizingObject = this;
            // 
            // TtForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.LabelRight);
            this.Controls.Add(this.LabelLeft);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "TtForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = true;
            this.Load += new System.EventHandler(this.TtForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.eventLog1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }



        #endregion

    

        private Label LabelLeft;
        private Label LabelRight;
        private System.Diagnostics.EventLog eventLog1;
        
    }
}