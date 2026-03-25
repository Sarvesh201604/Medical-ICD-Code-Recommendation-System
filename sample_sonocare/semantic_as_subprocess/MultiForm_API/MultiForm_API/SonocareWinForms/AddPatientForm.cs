using System;
using System.Drawing;
using System.Windows.Forms;
using SonocareWinForms.Data;
using SonocareWinForms.Models;

namespace SonocareWinForms
{
    public class AddPatientForm : Form
    {
        private Label lblName;
        private TextBox txtName;
        private Label lblAge;
        private TextBox txtAge;
        private Label lblId;
        private TextBox txtId;
        private Button btnSave;
        private Button btnCancel;

        public AddPatientForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Add Patient";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblName = new Label { Text = "Name:", Location = new Point(20, 20), AutoSize = true };
            txtName = new TextBox { Location = new Point(20, 45), Width = 340 };

            lblAge = new Label { Text = "Age:", Location = new Point(20, 80), AutoSize = true };
            txtAge = new TextBox { Location = new Point(20, 105), Width = 340 };

            lblId = new Label { Text = "ID Number:", Location = new Point(20, 140), AutoSize = true };
            txtId = new TextBox { Location = new Point(20, 165), Width = 340 };

            btnSave = new Button { Text = "Save", Location = new Point(190, 210), Width = 80, DialogResult = DialogResult.None };
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button { Text = "Cancel", Location = new Point(280, 210), Width = 80, DialogResult = DialogResult.Cancel };

            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblAge);
            this.Controls.Add(txtAge);
            this.Controls.Add(lblId);
            this.Controls.Add(txtId);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
            
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || 
                string.IsNullOrWhiteSpace(txtAge.Text) || 
                string.IsNullOrWhiteSpace(txtId.Text))
            {
                MessageBox.Show("Please fill all fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtAge.Text, out int age))
            {
                MessageBox.Show("Age must be a number.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                {
                    // Ensure DB exists
                    context.Database.EnsureCreated();

                    var patient = new Patient
                    {
                        Name = txtName.Text.Trim(),
                        Age = age,
                        IdNumber = txtId.Text.Trim()
                    };
                    context.Patients.Add(patient);
                    context.SaveChanges();
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving patient: {ex.Message}", "Default Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
