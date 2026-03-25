using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SonocareWinForms
{
    public static class MicButtonHelper
    {
        public static Button CreateFloatingMic(Form hostForm, EventHandler clickHandler)
        {
            var btn = new Button
            {
                Text = "🎤",
                Font = new Font("Segoe UI Emoji", 24, FontStyle.Regular),
                Size = new Size(60, 60),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                // Position bottom-right, relative to client area
                // We use Anchor so it moves if resized
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            // Initial Position
            btn.Location = new Point(hostForm.ClientSize.Width - 80, hostForm.ClientSize.Height - 80);

            // Round Region
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, 60, 60);
            btn.Region = new Region(path);

            btn.FlatAppearance.BorderSize = 0;
            btn.Click += clickHandler;

            hostForm.Controls.Add(btn);
            btn.BringToFront();

            return btn;
        }

        public static void UpdateState(Button btn, bool isListening)
        {
            if (btn == null || btn.IsDisposed) return;
            
            // Safe Invoke check
            if (btn.InvokeRequired)
            {
                btn.Invoke(new Action(() => UpdateState(btn, isListening)));
                return;
            }

            btn.BackColor = isListening ? Color.Red : Color.White;
            btn.ForeColor = isListening ? Color.White : Color.Black;
        }
    }
}
