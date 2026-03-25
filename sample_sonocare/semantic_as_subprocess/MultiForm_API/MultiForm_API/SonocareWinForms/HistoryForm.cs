using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SonocareWinForms.Data;

namespace SonocareWinForms
{
    public class HistoryForm : Form
    {
        private int _patientId;
        private Label lblHeader;
        private DataGridView dgvHistory;
        private Button btnClose;

        public HistoryForm(int patientId)
        {
            _patientId = patientId;
            InitializeComponent();
            LoadHistory();
        }

        private void InitializeComponent()
        {
            this.Text = "Patient History";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            lblHeader = new Label 
            { 
                Text = "History", 
                Font = new Font("Segoe UI", 12, FontStyle.Bold), 
                Location = new Point(10, 10), 
                AutoSize = true 
            };

            dgvHistory = new DataGridView
            {
                Location = new Point(10, 40),
                Size = new Size(660, 380),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            btnClose = new Button 
            { 
                Text = "Close", 
                Location = new Point(590, 430), 
                Width = 80, 
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right 
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.Add(lblHeader);
            this.Controls.Add(dgvHistory);
            this.Controls.Add(btnClose);
        }

        private void LoadHistory()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    context.Database.EnsureCreated();
                    
                    var patient = context.Patients.FirstOrDefault(p => p.Id == _patientId);
                    if (patient != null)
                    {
                        lblHeader.Text = $"History for: {patient.Name}";
                    }

                    var reports = context.Reports
                                         .Where(r => r.PatientId == _patientId)
                                         .OrderByDescending(r => r.Id)
                                         .Select(r => new 
                                         {
                                             Date = r.VisitDate,
                                             BPD = r.BPD,
                                             HC = r.HC,
                                             AC = r.AC,
                                             FL = r.FL,
                                             FHR = r.FHR
                                         })
                                         .ToList();
                    
                    dgvHistory.DataSource = reports;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading history: {ex.Message}");
            }
        }
    }
}
