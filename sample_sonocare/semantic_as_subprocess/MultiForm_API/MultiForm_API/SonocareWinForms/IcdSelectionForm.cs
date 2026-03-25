using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SonocareWinForms.Models;

namespace SonocareWinForms
{
    public class IcdSelectionForm : Form
    {
        private CheckedListBox clbCodes;
        private Button btnOk;
        private Button btnCancel;
        private Label lblHeader;

        public List<IcdCodeResult> SelectedCodes { get; private set; } = new List<IcdCodeResult>();

        public IcdSelectionForm(IcdPredictionResponse response)
        {
            InitializeComponent(response);
        }

        private void InitializeComponent(IcdPredictionResponse response)
        {
            this.Text = "Select ICD Recommendations";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblHeader = new Label
            {
                Text = "Select the ICD codes you want to add to the report:",
                Location = new Point(15, 15),
                Size = new Size(550, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            clbCodes = new CheckedListBox
            {
                Location = new Point(15, 50),
                Size = new Size(555, 340),
                CheckOnClick = true,
                Font = new Font("Segoe UI", 9)
            };

            if (response.IcdCodes != null)
            {
                foreach (var item in response.IcdCodes)
                {
                    string display = $"{item.Code} - {item.Description}";
                    clbCodes.Items.Add(new IcdItemWrapper { Code = item.Code, Description = item.Description, Display = display });
                }
            }

            btnOk = new Button
            {
                Text = "Add Selected",
                Location = new Point(360, 410),
                Width = 100,
                Height = 35,
                DialogResult = DialogResult.OK
            };
            btnOk.Click += (s, e) => {
                foreach (var item in clbCodes.CheckedItems)
                {
                    var wrapper = (IcdItemWrapper)item;
                    SelectedCodes.Add(new IcdCodeResult { Code = wrapper.Code, Description = wrapper.Description });
                }
                this.Close();
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(470, 410),
                Width = 100,
                Height = 35,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(lblHeader);
            this.Controls.Add(clbCodes);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private class IcdItemWrapper
        {
            public string Code { get; set; } = "";
            public string Description { get; set; } = "";
            public string Display { get; set; } = "";
            public override string ToString() => Display;
        }
    }
}
