using System;
using System.Drawing;
using System.Windows.Forms;
using PoePickit.Extensions;


namespace PoePickit
{
    public partial class ToolTipForm : Form
    {
        
        public ToolTipForm()
        {
            InitializeComponent();
            SetFormOptions();
        }

        public void Initialize(PoeToolTip toolTip)
        {
            if (toolTip == null)
                Hide();
            else
            {
                
                SetText(toolTip.ArgText, toolTip.ValueText);
                var ttLocation = new Point();
                //set tooltip location
                var activeWindowWidth = Screen.GetWorkingArea(MousePosition).Width;
                var activeWindowHeight = Screen.GetWorkingArea(MousePosition).Height;
                var mX = MousePosition.X;
                var mY = MousePosition.Y;

                ttLocation.X = this.Size.Width/2 + mX > activeWindowWidth ? ttLocation.X = activeWindowWidth - this.Size.Width : mX - this.Size.Width/2 < 0 ? ttLocation.X = 0 : ttLocation.X = mX - this.Size.Width / 2; 
                ttLocation.Y = this.Size.Height + 40 + mY > activeWindowHeight ? ttLocation.Y = mY - this.Size.Height - 40 : ttLocation.Y = mY + 40;
                
                

                Location = ttLocation;
                ShowInTaskbar = false;
                BackColor = toolTip.BackColor;
                ForeColor = toolTip.TextColor;
                
                SetFormOptions(toolTip.TextColor);
                Show();
                
            }
        }

        public void SetFormOptions(Color color = default(Color))
        {
            int size = 10;
            LabelLeft.Font = new Font("Franklin Gothic", size, FontStyle.Bold);
            LabelRight.Font = new Font("Franklin Gothic", size, FontStyle.Bold);
            LabelLeft.ForeColor = color;
            LabelRight.ForeColor = color;
        }

        
        public void SetText(string leftText, string rightText)
        {

            int leftLabelWidth = 0, rightLabelWidth = 0, rightLabelHeight = 0, leftLabelHeight = 0;
            if (leftText != null)
                foreach (var line in  leftText.Split('\n'))
                {
                    leftLabelWidth = leftLabelWidth > line.Length ? leftLabelWidth : line.Length;
                    leftLabelHeight++;
                }
            else
            {
                leftLabelWidth = 6;
                leftLabelHeight = 6;
            }

            if (rightText != null)
                foreach (var line in rightText.Split('\n'))
                {
                    rightLabelWidth = rightLabelWidth > line.Length ? rightLabelWidth : line.Length;
                    rightLabelHeight++;
                }
            else

            {
                rightLabelWidth = 6;
                rightLabelHeight = 6;
            }

            //leftLabelWidth *= FontSize-1;
            //leftLabelHeight *= FontSize+6;
            //rightLabelWidth *= FontSize-1;
            //rightLabelHeight *= FontSize+6;
            leftLabelWidth *= 9;
            leftLabelHeight *= 15;
            rightLabelWidth *= 9;
            rightLabelHeight *= 15;


            this.LabelLeft.Size = new Size(leftLabelWidth , leftLabelHeight);
            this.LabelRight.Size = new Size(rightLabelWidth , rightLabelHeight);
            this.LabelLeft.Location = new Point(Font.Height , 13);
            this.LabelRight.Location = new Point(LabelLeft.Location.X + LabelLeft.Width,13);
            this.LabelLeft.Text = leftText;
            this.LabelRight.Text = rightText;
            
            //this.Size = new Size(LabelRight.Location.X + LabelRight.Width + Font.Height , LabelLeft.Height + Font.Height*2);
            
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            ShowInTaskbar = true; // Remove from taskbar.
            Hide();
        }
    }
}
