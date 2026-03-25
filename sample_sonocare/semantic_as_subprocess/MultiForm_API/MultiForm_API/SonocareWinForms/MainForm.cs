using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SonocareWinForms.Data;
using SonocareWinForms.Models;
using SonocareWinForms.Services; // Ensure Services namespace is used
using Microsoft.EntityFrameworkCore;

namespace SonocareWinForms
{
    public class MainForm : Form
    {
        private DataGridView dgvPatients;
        private Button btnAddPatient;
        private Button btnRefresh;
        private Button btnGlobalMic; // Global Mic
        private Button btnFloatingMic; // Persistent Floating Mic
        private Label lblTitle;
        private Label lblMicStatus; // Global Mic Status

        private AugnitoService _augnitoService;
        public AugnitoService AugnitoService => _augnitoService;
        private bool _isListening = false;
        private ListBox lbDebugLog; // Added Debug Log


        public MainForm()
        {
            InitializeComponent();
            InitializeFloatingMic(); // Initialize the floating button
            InitializeAugnito();
            LoadPatients();
        }

        private void InitializeComponent()
        {
            this.Text = "Sonocare App - Dashboard";
            this.Size = new Size(1200, 800); // Increased size for MDI
            this.StartPosition = FormStartPosition.CenterScreen;
            this.StartPosition = FormStartPosition.CenterScreen;
            // this.IsMdiContainer = true; // Removed for separate windows

            // Initialize Controls
            lblTitle = new Label 
            { 
                Text = "Sonocare Patient Dashboard", 
                Font = new Font("Segoe UI", 16, FontStyle.Bold), 
                Location = new Point(10, 10), 
                AutoSize = true 
            };

            btnAddPatient = new Button 
            { 
                Text = "Add New Patient", 
                Location = new Point(10, 50), 
                Width = 120, 
                BackColor = Color.LightGreen 
            };
            btnAddPatient.Click += BtnAddPatient_Click;

            btnRefresh = new Button 
            { 
                Text = "Refresh List", 
                Location = new Point(140, 50), 
                Width = 100 
            };
            btnRefresh.Click += BtnRefresh_Click;

            var btnCalendar = new Button
            {
                Text = "Open Calendar",
                Location = new Point(250, 50),
                Width = 100
            };
            btnCalendar.Click += BtnCalendar_Click;

            // Global Mic Button (Toolbar version)
            btnGlobalMic = new Button
            {
                Text = "Start Mic",
                Location = new Point(360, 50),
                Width = 100,
                BackColor = Color.LightGray
            };
            btnGlobalMic.Click += BtnGlobalMic_Click;

            lblMicStatus = new Label
            {
                Text = "Mic: Ready",
                Location = new Point(470, 55),
                AutoSize = true,
                ForeColor = Color.Gray
            };

            dgvPatients = new DataGridView
            {
                Location = new Point(10, 90),
                Size = new Size(1160, 660), // Adjusted for larger form
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            
            // Interaction to open report/history
            dgvPatients.CellContentClick += DgvPatients_CellContentClick;
            dgvPatients.CellDoubleClick += DgvPatients_CellDoubleClick;

            // Add controls to the Form (not MDI parent directly supports controls, but they overlay)
            // However, for MDI, usually we use a ToolStrip or similar. 
            // For simplicity in WinForms MDI, we can add controls to the form, but they might cover MDI children 
            // if not docked correctly. To avoid this, standard MDI apps use MenuStrips/ToolStrips.
            // But to keep existing UI style, we'll dock a Panel at the top for controls.
            
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150, // Increased height for log
                BackColor = Color.White
            };

            // Move controls to topPanel
            lblTitle.Location = new Point(10, 10);
            btnAddPatient.Location = new Point(10, 50);
            btnRefresh.Location = new Point(140, 50);
            btnCalendar.Location = new Point(250, 50);
            btnGlobalMic.Location = new Point(360, 50);
            lblMicStatus.Location = new Point(470, 55);
            
            // Debug Log
            lbDebugLog = new ListBox 
            { 
                Location = new Point(600, 10), 
                Size = new Size(300, 130),
                Font = new Font("Consolas", 8)
            };

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(btnAddPatient);
            topPanel.Controls.Add(btnRefresh);
            topPanel.Controls.Add(btnCalendar);
            topPanel.Controls.Add(btnGlobalMic);
            topPanel.Controls.Add(lblMicStatus);
            topPanel.Controls.Add(lbDebugLog); // Add log to panel

            this.Controls.Add(dgvPatients); // DataGridView fill
            this.Controls.Add(topPanel); // Panel top

            // Adjust DGV to fill remaining space
            dgvPatients.BringToFront(); // Actually, in MDI, DGV might block children if it's just 'added'.
            // Usually MDI parents DON'T have a central DGV. The DGV should probably be in a "PatientListForm" child.
            // But to satisfy the user request "MDI Forms", and keep current functionality...
            // If we enable IsMdiContainer, the background becomes MDI client area. Controls added to 'this' float over it.
            // The DGV covering the whole background will hide any MDI children.
            // SOLUTION: We will make the DGV "Visible = false" when a child is open? Or dock it?
            // BETTER: Open a "HomeForm" or "ListForm" as the first MDI child.
            // Refactoring to MDI usually implies the main window is just a frame.
            // Let's hide the DGV when an MDI child is active, or dock it to left?
            // OR: Just clear DGV from MDI parent and put it in a child form "DashboardForm".
            // THAT IS THE CLEANEST WAY.
            
            // However, I must stick to the plan which didn't explicitly say "Create DashboardForm". 
            // Check plan: "MainForm becomes MDI Container". 
            // Quickest win: Group the DGV and buttons into a Panel. 
            // If MDI child opens, we can just let it float on top? No, MDI children are *behind* MDI parent controls.
            // Actually, MDI Client area is a specific control. Controls added to Form.Controls are Z-ordered above MDI Client.
            // Use 'Controls.Add(mdiClient)' logic? No, .NET does this auto.
            // Correct approach: Put all "Dashboard" content into a specific "Home" MDI Child form.
            // User requested "MDI Forms". 
            // I will move DGV to a Panel called `pnlDashboard` and simply Hide `pnlDashboard` when a child form is open?
            // Or better: Just open `ReportForm` and `CalendarForm` and let user manage windows. 
            // But the DGV will obscure them if it sits on `MainForm`.
            // DECISION: I will wrap the DGV in a Panel (`pnlDashboard`), and Dock it Fill.
            // When opening a child form, I will `pnlDashboard.Visible = false`.
            // When all children are closed, `pnlDashboard.Visible = true`.
            
            // pnlDashboard = new Panel { Dock = DockStyle.Fill };
            // pnlDashboard.Controls.Add(dgvPatients);
            // this.Controls.Add(pnlDashboard);
            // pnlDashboard.BringToFront();
            
            this.Controls.Add(dgvPatients);
            dgvPatients.BringToFront();
            
            // Re-setup DGV location inside panel
            dgvPatients.Dock = DockStyle.Fill;
        }

        private void InitializeFloatingMic()
        {
            // Use Helper
            btnFloatingMic = MicButtonHelper.CreateFloatingMic(this, BtnGlobalMic_Click);
        }

        // private Panel pnlDashboard; // Removed

        private void InitializeAugnito()
        {
            _augnitoService = new AugnitoService();
            _augnitoService.OnLog += (msg) => Invoke((Action)(() => 
            {
                lbDebugLog.Items.Insert(0, $"{DateTime.Now.ToLongTimeString()}: {msg}");
                if (lbDebugLog.Items.Count > 100) lbDebugLog.Items.RemoveAt(100);
            }));
            
            _augnitoService.OnTranscriptReceived += AugnitoService_OnRecognized;
            _augnitoService.OnError += (err) => Invoke((Action)(() => lblMicStatus.Text = $"Error: {err}"));
            _augnitoService.OnListeningStateChanged += (listening) => Invoke((Action)(() => 
            {
                lblMicStatus.Text = listening ? "Mic: Listening..." : "Mic: Ready";
                lblMicStatus.ForeColor = listening ? Color.Red : Color.Gray;
                
                // Update Toolbar Button
                btnGlobalMic.BackColor = listening ? Color.LightCoral : Color.LightGray;
                btnGlobalMic.Text = listening ? "Stop Mic" : "Start Mic";

                // Update Floating Button
                MicButtonHelper.UpdateState(btnFloatingMic, listening);
            }));
        }

        private void BtnGlobalMic_Click(object sender, EventArgs e)
        {
            if (!_isListening) StartMic(); else StopMic();
        }

        private async void StartMic()
        {
            try { await _augnitoService.InitializeAsync(); await _augnitoService.StartListeningAsync(); _isListening = true; }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private async void StopMic()
        {
            try { await _augnitoService.StopListeningAsync(); _isListening = false; }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void AugnitoService_OnRecognized(string text)
        {
            if (InvokeRequired) { Invoke(new Action<string>(AugnitoService_OnRecognized), text); return; }

            // GLOBAL COMMANDS
            string micCommand = text.ToLower().Trim();
            if (micCommand == "stop mic" || micCommand == "mute")
            {
                StopMic();
                return;
            }

            // DEBUG FEEDBACK
            lblMicStatus.Text = $"Heard: {text}";
            
             // Log active child
            // GLOBAL ROUTING LOGIC
            // 1. Check if we have an Active Form (Separate Window)
            Form activeWindow = Form.ActiveForm;
            
            // Log active child
            string childName = activeWindow != null ? activeWindow.GetType().Name : "None";
            lbDebugLog.Items.Insert(0, $"Routing '{text}' to {childName}");

            if (activeWindow != null)
            {
                if (activeWindow is CalendarForm calendarForm)
                {
                    calendarForm.HandleVoiceCommand(text);
                    return;
                }
                else if (activeWindow is ReportForm reportForm)
                {
                    reportForm.HandleVoiceCommand(text);
                    return;
                }
                // If active form is MainForm, fall through to Dashboard commands
            }

            // 2. If no specific child (or MainForm is active), handle dashboard commands
            string lower = text.ToLower();
            if (lower.Contains("open calendar") || lower.Contains("calendar"))
            {
                BtnCalendar_Click(this, EventArgs.Empty);
            }
            else if (lower.Contains("add patient") || lower.Contains("new patient"))
            {
                BtnAddPatient_Click(this, EventArgs.Empty);
            }
            else if (lower.Contains("refresh"))
            {
                BtnRefresh_Click(this, EventArgs.Empty);
            }
        }

        private void LoadPatients()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    context.Database.EnsureCreated();
                    var patients = context.Patients.ToList();
                    
                    dgvPatients.DataSource = null;
                    dgvPatients.Columns.Clear();

                    dgvPatients.DataSource = patients;
                    
                    // Width cannot be set when AutoSizeColumnsMode is Fill
                    // if (dgvPatients.Columns["Id"] != null) dgvPatients.Columns["Id"].Width = 50;

                    var btnReport = new DataGridViewButtonColumn();
                    btnReport.HeaderText = "Action";
                    btnReport.Text = "New Report";
                    btnReport.UseColumnTextForButtonValue = true;
                    btnReport.Name = "btnReport";
                    dgvPatients.Columns.Add(btnReport);

                    var btnHistory = new DataGridViewButtonColumn();
                    btnHistory.HeaderText = "History";
                    btnHistory.Text = "View History";
                    btnHistory.UseColumnTextForButtonValue = true;
                    btnHistory.Name = "btnHistory";
                    dgvPatients.Columns.Add(btnHistory);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    string logDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Sonocare",
                        "Logs"
                    );
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    string exceptionFile = Path.Combine(logDir, "exception.txt");
                    System.IO.File.WriteAllText(exceptionFile, ex.ToString());
                }
                catch { }
                MessageBox.Show($"Error loading patients: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}");
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadPatients();
        }

        private void BtnAddPatient_Click(object sender, EventArgs e)
        {
            // AddPatientForm is a small dialog, probably fine to keep as Dialog?
            // MDI applications often have modal dialogs for small inputs.
            // Let's keep it as Dialog for now to be safe, or make it MDI child if requested.
            // User asked for "MDI forms".
            var addForm = new AddPatientForm();
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadPatients();
            }
        }

        private void DgvPatients_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var patient = dgvPatients.Rows[e.RowIndex].DataBoundItem as Patient;
            if (patient == null) return;

            if (dgvPatients.Columns[e.ColumnIndex].Name == "btnReport")
            {
                OpenReport(patient.Id);
            }
            else if (dgvPatients.Columns[e.ColumnIndex].Name == "btnHistory")
            {
                var historyForm = new HistoryForm(patient.Id);
                historyForm.ShowDialog();
            }
        }

        private void DgvPatients_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var patient = dgvPatients.Rows[e.RowIndex].DataBoundItem as Patient;
            if (patient != null)
            {
                OpenReport(patient.Id);
            }
        }

        private void OpenReport(int patientId)
        {
             // Check if already open
             foreach(Form f in Application.OpenForms)
             {
                 if (f is ReportForm && (f.Tag as int?) == patientId)
                 {
                     f.Activate();
                     f.BringToFront();
                     return;
                 }
             }

             var reportForm = new ReportForm(patientId);
             reportForm.AugnitoService = this.AugnitoService; // Inject Service
             reportForm.Tag = patientId; // Track ID
             // reportForm.FormClosed += ChildFormClosed; // Removed
             
             reportForm.Show(this); // Open separate but Owned (Child)
        }

        private void BtnCalendar_Click(object sender, EventArgs e)
        {
            // Check if open
             foreach(Form f in Application.OpenForms)
             {
                 if (f is CalendarForm)
                 {
                     f.Activate();
                     f.BringToFront();
                     return;
                 }
             }
            var calendarForm = new CalendarForm();
            calendarForm.AugnitoService = this.AugnitoService; // Inject Service
            
            calendarForm.Show(this); // Open separate but Owned (Child)
        }
        
        // ChildFormClosed removed as it's not needed for separate windows
    }
}
