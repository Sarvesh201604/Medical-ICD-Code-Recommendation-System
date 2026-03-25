using System;
using System.Drawing;
using System.Windows.Forms;

namespace SonocareWinForms
{
    public class RichTextControl : UserControl
    {
        private RichTextBox _richTextBox;

        public string Content
        {
            get { return _richTextBox.Text; }
            set { _richTextBox.Text = value; }
        }

        public RichTextBox Editor => _richTextBox;

        public RichTextControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this._richTextBox = new RichTextBox();
            this.SuspendLayout();
            
            // 
            // _richTextBox
            // 
            this._richTextBox.Dock = DockStyle.Fill;
            this._richTextBox.Location = new Point(0, 0);
            this._richTextBox.Name = "_richTextBox";
            this._richTextBox.Size = new Size(150, 150);
            this._richTextBox.TabIndex = 0;
            this._richTextBox.Text = "";
            
            // 
            // RichTextControl
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Controls.Add(this._richTextBox);
            this.Name = "RichTextControl";
            this.ResumeLayout(false);
        }
    }
}
