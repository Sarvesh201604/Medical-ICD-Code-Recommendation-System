using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

using Microsoft.EntityFrameworkCore;
using SonocareWinForms.Data;
using SonocareWinForms.Models;
using SonocareWinForms.Services;
using System.Reflection;

namespace SonocareWinForms
{
    public class ReportForm : Form
    {
        private int _patientId;
        
        // UI Controls
        private Label lblPatientInfo;
        private TabControl mainTabControl;
        private TabPage tabBiometry;
        private TabPage tabScanDetails;
        private TabPage tabFinalImpression;
        private FinalImpressionControl finalImpressionControl;
        
        // Tab 1 Fields
        private TextBox txtBpd;
        private TextBox txtHc;
        private TextBox txtAc;
        private TextBox txtFl;
        private TextBox txtFhr;
        private RichTextBox txtComments;
        
        // Gender
        private RadioButton rbMale;
        private RadioButton rbFemale;
        private RadioButton rbOther;
        
        // Tab 2 Fields
        private TextBox txtGa;
        private TextBox txtPlacenta;
        private TextBox txtAfi;
        private TextBox txtPresentation;
        private TextBox txtEfw;
        private ComboBox cmbScanType;
        private RichTextControl disclaimer;
        
        // Testing Table
        private DataGridView dgvBiometryHistory;
        
        // Voice Controls Removed
        // private AugnitoService _augnitoService;
        public AugnitoService AugnitoService { get; set; } = null!;
        private Button btnFloatingMic = null!;
        private ListBox lbDebugLog = null!;
        // private Label lblMicStatus = null!;
        // private Button btnMic = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private Button btnRecord = null!;
        private Button btnGetIcd = null!;
        private CheckBox chkNormal = null!;
        private CheckBox chkAbnormal = null!;

        private readonly EmbeddedIcdApiClient _icdApiClient;
        private System.Threading.CancellationTokenSource? _icdRequestCts;

        // Voice Command Mapping
        
        // Voice History Tracking
        private class VoiceHistoryEntry
        {
            public Control? Control { get; set; }
            public string PreviousText { get; set; } = "";
            public string AddedText { get; set; } = "";
            public int SelectionStart { get; set; }
        }
        private Stack<VoiceHistoryEntry> _voiceHistory = new Stack<VoiceHistoryEntry>();
        private const int MAX_HISTORY_SIZE = 10;




        public ReportForm(int patientId)
        {
            _patientId = patientId;
            _icdApiClient = EmbeddedIcdApiClient.Instance;
            InitializeComponent();
            LoadPatient();
            InitializeVoiceMap(); // Replaces InitializeVoiceCommands
            
            // ... (rest of constructor) ...
            
            // Initialize Grid Data
            var defaultData = new List<BiometryCaseData>
            {
                new BiometryCaseData { Patient = "Case 1" },
                new BiometryCaseData { Patient = "Case 2" },
                new BiometryCaseData { Patient = "Case 3" },
                new BiometryCaseData { Patient = "Case 4" }
            };
            dgvBiometryHistory.DataSource = defaultData;
        }
        
        private void InitializeComponent()
        {
            this.Text = "Report Generator";
            this.Size = new Size(1000, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            
            lblPatientInfo = new Label { Location = new Point(10, 10), AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            
            mainTabControl = new TabControl { Location = new Point(10, 50), Size = new Size(960, 500) };
            
            tabBiometry = new TabPage("Biometry & Comments");
            tabScanDetails = new TabPage("Scan Details");
            tabFinalImpression = new TabPage("Final Impression");
            
            mainTabControl.TabPages.Add(tabBiometry);
            mainTabControl.TabPages.Add(tabScanDetails);
            mainTabControl.TabPages.Add(tabFinalImpression);
            
            // Setup Tab 1
            SetupBiometryTab();
            
            // Setup Tab 2
            SetupScanDetailsTab();

            // Setup Tab 3
            finalImpressionControl = new FinalImpressionControl { Dock = DockStyle.Fill };
            tabFinalImpression.Controls.Add(finalImpressionControl);
            
            // Default Buttons and Status
            // lblMicStatus = new Label { Text = "Mic Status: Paused", Location = new Point(10, 560), AutoSize = true };
            // btnMic = new Button { Text = "Start Mic", Location = new Point(150, 555), Width = 100 };
            // btnMic.Click += MicBtn_Click;
            
            btnRecord = new Button { Text = "Record Audio", Location = new Point(640, 560), Width = 100, BackColor = Color.LightGray };
            btnRecord.Click += Record_Click;

            chkNormal = new CheckBox 
            { 
                Name = "chkNormal", 
                Text = "Normal", 
                Location = new Point(310, 560), 
                AutoSize = true,
                Checked = true
            };
            
            chkAbnormal = new CheckBox 
            { 
                Name = "chkAbnormal", 
                Text = "Abnormal", 
                Location = new Point(410, 560), 
                AutoSize = true,
                Checked = true
            };

            btnGetIcd = new Button
            {
                Name = "btnGetIcd",
                Text = "Get ICD",
                Location = new Point(520, 560),
                Width = 110,
                BackColor = Color.LightBlue
            };
            btnGetIcd.Click += GetIcd_Click;

            btnSave = new Button { Text = "Save Report", Location = new Point(750, 560), Width = 100, BackColor = Color.LightGreen };
            btnSave.Click += Save_Click;
            
            btnCancel = new Button { Text = "Cancel", Location = new Point(860, 560), Width = 100 };
            btnCancel.Click += Cancel_Click;
            
            lbDebugLog = new ListBox { Location = new Point(10, 600), Size = new Size(960, 150) };
            
            
            this.Controls.Add(lblPatientInfo);
            this.Controls.Add(mainTabControl);
            this.Controls.Add(chkNormal);
            this.Controls.Add(chkAbnormal);
            // this.Controls.Add(lblMicStatus);
            // this.Controls.Add(btnMic);
            this.Controls.Add(btnGetIcd);
            this.Controls.Add(btnRecord);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
            this.Controls.Add(lbDebugLog);
            this.Controls.Add(lbDebugLog);



            this.Load += ReportForm_Load;
            this.FormClosed += ReportForm_FormClosed;
        }




        private void SetupBiometryTab()
        {
            // Simple absolute layout for speed
            int y = 20;
            int xLabel = 20;
            int xBox = 150;
            
            tabBiometry.Controls.Add(new Label { Text = "BPD:", Location = new Point(xLabel, y) });
            txtBpd = new TextBox { Name = "txtBpd", Location = new Point(xBox, y), Width = 200 };
            tabBiometry.Controls.Add(txtBpd);
            y += 40;
            
            tabBiometry.Controls.Add(new Label { Text = "HC:", Location = new Point(xLabel, y) });
            txtHc = new TextBox { Name = "txtHc", Location = new Point(xBox, y), Width = 200 };
            tabBiometry.Controls.Add(txtHc);
            y += 40;
            
            tabBiometry.Controls.Add(new Label { Text = "AC:", Location = new Point(xLabel, y) });
            txtAc = new TextBox { Name = "txtAc", Location = new Point(xBox, y), Width = 200 };
            tabBiometry.Controls.Add(txtAc);
            y += 40;
            
            tabBiometry.Controls.Add(new Label { Text = "FL:", Location = new Point(xLabel, y) });
            txtFl = new TextBox { Name = "txtFl", Location = new Point(xBox, y), Width = 200 };
            tabBiometry.Controls.Add(txtFl);
            y += 40;
            
            tabBiometry.Controls.Add(new Label { Text = "FHR:", Location = new Point(xLabel, y) });
            txtFhr = new TextBox { Name = "txtFhr", Location = new Point(xBox, y), Width = 200 };
            tabBiometry.Controls.Add(txtFhr);
            y += 40;
            
            // Gender
            var grpGender = new GroupBox { Text = "Gender", Location = new Point(400, 20), Size = new Size(200, 150) };
            rbMale = new RadioButton { Name = "rbMale", Text = "Male", Location = new Point(10, 30) };
            rbFemale = new RadioButton { Name = "rbFemale", Text = "Female", Location = new Point(10, 60) };
            rbOther = new RadioButton { Name = "rbOther", Text = "Others", Location = new Point(10, 90) };
            grpGender.Controls.Add(rbMale);
            grpGender.Controls.Add(rbFemale);
            grpGender.Controls.Add(rbOther);
            tabBiometry.Controls.Add(grpGender);
            
            // Comments
            tabBiometry.Controls.Add(new Label { Text = "Comments:", Location = new Point(xLabel, y) });
            txtComments = new RichTextBox { Name = "txtComments", Location = new Point(xBox, y), Width = 500, Height = 100, ScrollBars = RichTextBoxScrollBars.Vertical };
            tabBiometry.Controls.Add(txtComments);
            y += 120;
            
            // Grid
            tabBiometry.Controls.Add(new Label { Text = "Biometry History:", Location = new Point(xLabel, y) });
            y += 25;
            dgvBiometryHistory = new DataGridView 
            { 
                Name = "dgvBiometryHistory",
                Location = new Point(xLabel, y), 
                Size = new Size(600, 150),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            
            tabBiometry.Controls.Add(dgvBiometryHistory);
        }

        private void SetupScanDetailsTab()
        {
            int y = 20;
            int xLabel = 20;
            int xBox = 150;
            
            tabScanDetails.Controls.Add(new Label { Text = "GA:", Location = new Point(xLabel, y) });
            txtGa = new TextBox { Name = "txtGa", Location = new Point(xBox, y), Width = 200 };
            tabScanDetails.Controls.Add(txtGa);
            y += 40;
            
            tabScanDetails.Controls.Add(new Label { Text = "Placenta:", Location = new Point(xLabel, y) });
            txtPlacenta = new TextBox { Name = "txtPlacenta", Location = new Point(xBox, y), Width = 200 };
            tabScanDetails.Controls.Add(txtPlacenta);
            y += 40;
            
            tabScanDetails.Controls.Add(new Label { Text = "AFI:", Location = new Point(xLabel, y) });
            txtAfi = new TextBox { Name = "txtAfi", Location = new Point(xBox, y), Width = 200 };
            tabScanDetails.Controls.Add(txtAfi);
            y += 40;
            
            tabScanDetails.Controls.Add(new Label { Text = "Presentation:", Location = new Point(xLabel, y) });
            txtPresentation = new TextBox { Name = "txtPresentation", Location = new Point(xBox, y), Width = 200 };
            tabScanDetails.Controls.Add(txtPresentation);
            y += 40;
            
            tabScanDetails.Controls.Add(new Label { Text = "EFW (Weight):", Location = new Point(xLabel, y) });
            txtEfw = new TextBox { Name = "txtEfw", Location = new Point(xBox, y), Width = 200 };
            tabScanDetails.Controls.Add(txtEfw);
            y += 40;
            
            tabScanDetails.Controls.Add(new Label { Text = "Scan Type:", Location = new Point(xLabel, y) });
            cmbScanType = new ComboBox { Name = "cmbScanType", Location = new Point(xBox, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbScanType.Items.AddRange(new object[] { "Early Pregnancy", "Growth Scan", "Anomaly Scan", "Nuchal Translucency", "Wellbeing" });
            tabScanDetails.Controls.Add(cmbScanType);
            y += 40;

            // Disclaimer
            tabScanDetails.Controls.Add(new Label { Text = "Disclaimer:", Location = new Point(xLabel, y) });
            disclaimer = new RichTextControl { Name = "disclaimer", Location = new Point(xBox, y), Size = new Size(400, 100) };
            tabScanDetails.Controls.Add(disclaimer);
        }

        private async void ReportForm_Load(object sender, EventArgs e)
        {
             // Initialize Floating Mic if Service is available
             if (AugnitoService != null)
             {
                 btnFloatingMic = MicButtonHelper.CreateFloatingMic(this, (s, args) => 
                 {
                     AugnitoService.ToggleListening();
                 });
                 
                 // Subscribe to updates
                 AugnitoService.OnListeningStateChanged += (listening) => 
                 {
                     MicButtonHelper.UpdateState(btnFloatingMic, listening);
                 };
                 
                 // Initial State Sync
                 MicButtonHelper.UpdateState(btnFloatingMic, AugnitoService.IsListening); 
             }
        }

        
        // Voice Control Reflection Logic

        // Voice Command Mapping
        private class VoiceCommandDefinition
        {
            public string ControlName { get; set; } = "";
            public ControlActionMapper.ControlAction Action { get; set; } = ControlActionMapper.ControlAction.Focus;
        }
        
        // Grid Configuration for dynamic navigation
        private class GridConfig
        {
            public string GridName { get; set; } = "";
            public string RowPattern { get; set; } = "case"; // e.g., "case", "row", "line", "patient"
            public List<string> ValidColumns { get; set; } = new List<string>();
        }

        private Dictionary<string, VoiceCommandDefinition> _voiceMap = new();
        private Dictionary<string, GridConfig> _gridConfigs = new(); // Store grid configurations

        private void InitializeVoiceMap()
        {
             GlobalFuncs.InitializeVoiceCommands();
             
             string formName = this.GetType().Name;
             
             if (GlobalFuncs.allforms.ContainsKey(formName))
             {
                 var formMappings = GlobalFuncs.allforms[formName];
                 foreach (var kvp in formMappings)
                 {
                     string controlName = kvp.Key;
                     
                     // Parse grid configuration metadata (keys starting with _grid_)
                     if (controlName.StartsWith("_grid_"))
                     {
                         ParseGridConfigEntry(controlName, kvp.Value);
                         continue; // Don't add to voice map
                     }
                     
                     foreach (string spokenPhrase in kvp.Value)
                     {
                         _voiceMap[spokenPhrase.ToLower()] = new VoiceCommandDefinition 
                         { 
                             ControlName = controlName, 
                             Action = GetDefaultActionForControl(controlName) 
                         };
                     }
                 }
             }

             // Virtual Navigation
             _voiceMap["biometry"] = new() { ControlName = "tabBiometry", Action = ControlActionMapper.ControlAction.Click };
             _voiceMap["scan details"] = new() { ControlName = "tabScanDetails", Action = ControlActionMapper.ControlAction.Click };
             _voiceMap["final impression"] = new() { ControlName = "tabFinalImpression", Action = ControlActionMapper.ControlAction.Click };
             _voiceMap["impression"] = new() { ControlName = "tabFinalImpression", Action = ControlActionMapper.ControlAction.Click };
             
             // ComboBox explicitly wants Dropdown to open the list for the user
             _voiceMap["scan type"] = new() { ControlName = "cmbScanType", Action = ControlActionMapper.ControlAction.Dropdown }; 
             _voiceMap["scan"] = new() { ControlName = "cmbScanType", Action = ControlActionMapper.ControlAction.Dropdown };
             _voiceMap["template"] = new() { ControlName = "cmbTemplate", Action = ControlActionMapper.ControlAction.Dropdown };
             _voiceMap["final template"] = new() { ControlName = "cmbTemplate", Action = ControlActionMapper.ControlAction.Dropdown };
             _voiceMap["impression template"] = new() { ControlName = "cmbTemplate", Action = ControlActionMapper.ControlAction.Dropdown };
               _voiceMap["get icd"] = new() { ControlName = "btnGetIcd", Action = ControlActionMapper.ControlAction.Click };
               _voiceMap["icd recommendation"] = new() { ControlName = "btnGetIcd", Action = ControlActionMapper.ControlAction.Click };
        }

        private ControlActionMapper.ControlAction GetDefaultActionForControl(string controlName)
        {
            if (controlName.StartsWith("txt") || controlName.StartsWith("rtb")) return ControlActionMapper.ControlAction.Type;
            if (controlName.StartsWith("cmb")) return ControlActionMapper.ControlAction.SelectByValue;
            if (controlName.StartsWith("btn") || controlName.StartsWith("rb") || controlName.StartsWith("chk")) return ControlActionMapper.ControlAction.Click;
            if (controlName.StartsWith("dgv")) return ControlActionMapper.ControlAction.Focus; // DataGridView
            return ControlActionMapper.ControlAction.Focus;
        }
        
        private void ParseGridConfigEntry(string key, List<string> values)
        {
            // Expected formats:
            // _grid_{GridName}_rowPattern: ["case", "row", "line"]
            // _grid_{GridName}_columns: ["BPD", "HC", "AC", "FL"]
            
            if (!key.StartsWith("_grid_")) return;
            
            string[] parts = key.Split('_');
            if (parts.Length < 4) return;
            
            string gridName = parts[2]; // _grid_{GridName}_xxx
            string configType = parts[3]; // rowPattern or columns
            
            if (!_gridConfigs.ContainsKey(gridName))
            {
                _gridConfigs[gridName] = new GridConfig { GridName = gridName };
            }
            
            if (configType.Equals("rowPattern", StringComparison.OrdinalIgnoreCase) && values.Count > 0)
            {
                _gridConfigs[gridName].RowPattern = values[0].ToLower();
            }
            else if (configType.Equals("columns", StringComparison.OrdinalIgnoreCase))
            {
                _gridConfigs[gridName].ValidColumns = values.Select(v => v.ToUpper()).ToList();
            }
        }


        public void HandleVoiceCommand(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            // --- Voice Corrections ---
            // Handle common speech recognition errors for "Case 2" and "Case 4"
            string correctedText = System.Text.RegularExpressions.Regex.Replace(text, "case to", "case 2", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            correctedText = System.Text.RegularExpressions.Regex.Replace(correctedText, "case for", "case 4", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            lbDebugLog.Items.Insert(0, $"[Heard] {correctedText}");
            string lowerText = correctedText.Trim(); 

            // --- NAVIGATION OVERRIDE (Always check these first) ---
            // Only trigger navigation if it's a pure navigation command, not a field command
            bool isBiometryNav = lowerText.Equals("biometry", StringComparison.OrdinalIgnoreCase) ||
                                 (lowerText.Contains("biometry") && !lowerText.Contains("bpd") && !lowerText.Contains("hc") && !lowerText.Contains("ac") && !lowerText.Contains("fl"));
            if (isBiometryNav)
            {
                mainTabControl.SelectedTab = tabBiometry;
                lbDebugLog.Items.Insert(0, "[Nav] Switched to Biometry");
                return;
            }
            
            if (lowerText.Equals("scan details", StringComparison.OrdinalIgnoreCase) || lowerText.Equals("scan", StringComparison.OrdinalIgnoreCase))
            {
                mainTabControl.SelectedTab = tabScanDetails;
                lbDebugLog.Items.Insert(0, "[Nav] Switched to Scan Details");
                return;
            }
            
            // Only navigate to impression tab if it's JUST "impression" or "final impression", not "impression BPD" etc.
            bool isPureImpressionNav = lowerText.Equals("impression", StringComparison.OrdinalIgnoreCase) || 
                                       lowerText.Equals("final impression", StringComparison.OrdinalIgnoreCase);
            if (isPureImpressionNav)
            {
                mainTabControl.SelectedTab = tabFinalImpression;
                lbDebugLog.Items.Insert(0, "[Nav] Switched to Final Impression");
                return;
            }
            
            // --- FIELD NAVIGATION COMMANDS ---
            // "Next field" - Move to next control in tab order
            if (lowerText.Contains("next field") || lowerText.Contains("next box") || lowerText.Contains("go next"))
            {
                Control? currentControl = this.ActiveControl;
                while (currentControl is ContainerControl container && container.ActiveControl != null)
                {
                    currentControl = container.ActiveControl;
                }
                
                if (currentControl != null)
                {
                    NavigateToNextField(currentControl, forward: true);
                    lbDebugLog.Items.Insert(0, $"[Nav] Moved to next field from {currentControl.Name}");
                }
                else
                {
                    // No control focused, focus the first one
                    var firstField = GetNextFocusableControl(this, null, forward: true);
                    if (firstField != null)
                    {
                        firstField.Focus();
                        lbDebugLog.Items.Insert(0, $"[Nav] Focused first field: {firstField.Name}");
                    }
                }
                return;
            }
            
            // "Previous field" - Move to previous control in tab order
            if (lowerText.Contains("previous field") || lowerText.Contains("last field") || lowerText.Contains("go back") || lowerText.Contains("back field"))
            {
                Control? currentControl = this.ActiveControl;
                while (currentControl is ContainerControl container && container.ActiveControl != null)
                {
                    currentControl = container.ActiveControl;
                }
                
                if (currentControl != null)
                {
                    NavigateToNextField(currentControl, forward: false);
                    lbDebugLog.Items.Insert(0, $"[Nav] Moved to previous field from {currentControl.Name}");
                }
                else
                {
                    // No control focused, focus the last one
                    var lastField = GetNextFocusableControl(this, null, forward: false);
                    if (lastField != null)
                    {
                        lastField.Focus();
                        lbDebugLog.Items.Insert(0, $"[Nav] Focused last field: {lastField.Name}");
                    }
                }
                return;
            }

            // --- 0. Special Handling for Comments Mode (Lock) ---
            // If the user is currently focused on the Comments box, we want to capture everything as text
            // UNLESS they say a specific "Escape" command like "Stop dictation" or "Scan Details" (if we want to allow nav).
            // For now, let's just allow navigation commands to break out, OR explicitly check for "Next/Stop".
            
            // Note: We need to check if the specific control is focused. 
            // In WinForms, ActiveControl might be the TabControl or SplitContainer, so we find the nested focused control.
            Control? focusedControl = this.ActiveControl;
            while (focusedControl is ContainerControl container && container.ActiveControl != null)
            {
                focusedControl = container.ActiveControl;
            }

            // --- MULTILINE LOCK MODE (Comments, Summary, etc.) ---
            if (IsMultilineTextControl(focusedControl))
            {
                lbDebugLog.Items.Insert(0, $"[Multiline] Received: '{text}' in {focusedControl.Name}");
                
                // check for "clear" and "clear all" strict matches to allow clearing while in lock mode
                bool isClearCommand = lowerText.Equals("clear", StringComparison.OrdinalIgnoreCase);
                bool isClearAllCommand = lowerText.Equals("clear all", StringComparison.OrdinalIgnoreCase);

                if (isClearCommand || isClearAllCommand)
                {
                    // Allow these to fall through to the global handler below
                }
                else
                {
                    // Check for UNDO LAST command
                    if (lowerText.Contains("undo last") || lowerText.Contains("undo that"))
                    {
                        UndoLastVoiceEntry();
                        return;
                    }
                    
                    // Check for DELETE THAT command
                    if (lowerText.Contains("delete that") || lowerText.Contains("remove that"))
                    {
                        DeleteLastVoiceEntry();
                        return;
                    }
                    
                    // Check for REPLACE command (e.g., "replace normal with abnormal")
                    var replaceMatch = System.Text.RegularExpressions.Regex.Match(lowerText, 
                        @"replace\s+(.+?)\s+with\s+(.+)", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (replaceMatch.Success)
                    {
                        string oldWord = replaceMatch.Groups[1].Value.Trim();
                        string newWord = replaceMatch.Groups[2].Value.Trim();
                        ReplaceWordInControl(focusedControl, oldWord, newWord);
                        return;
                    }
                    
                    // Check for exit commands - support multiple variations
                    string fieldName = focusedControl.Name.ToLower();
                    bool isEscapeCommand = lowerText.IndexOf("stop dictation", StringComparison.OrdinalIgnoreCase) >= 0
                                           || lowerText.IndexOf("exit comment", StringComparison.OrdinalIgnoreCase) >= 0
                                           || lowerText.IndexOf("exit common", StringComparison.OrdinalIgnoreCase) >= 0
                                           || lowerText.IndexOf("exit summary", StringComparison.OrdinalIgnoreCase) >= 0
                                           || lowerText.IndexOf("exit notes", StringComparison.OrdinalIgnoreCase) >= 0
                                           || lowerText.IndexOf("stop comments", StringComparison.OrdinalIgnoreCase) >= 0
                                           || lowerText.IndexOf("stop summary", StringComparison.OrdinalIgnoreCase) >= 0
                                           || lowerText.IndexOf("stop notes", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isEscapeCommand)
                    {
                         // Break the lock by moving focus away
                         this.ActiveControl = null; 
                         lbDebugLog.Items.Insert(0, $"[System] Exited {focusedControl.Name} Mode");
                         return;
                    }

                    // Handle editing commands for multiline controls
                    // Check for "next line" with flexible matching
                    bool isNextLineCommand = lowerText.Contains("next line") || 
                                           lowerText.Contains("nextline") ||
                                           lowerText.Contains("new line") ||
                                           lowerText.Contains("newline");
                    
                    if (isNextLineCommand)
                    {
                        lbDebugLog.Items.Insert(0, $"[Edit] NEXT LINE command detected from: '{text}'");
                        if (focusedControl is TextBox txt)
                        {
                            txt.AppendText("\n");
                            txt.SelectionStart = txt.Text.Length;
                        }
                        else if (focusedControl is RichTextBox rtb)
                        {
                            rtb.AppendText("\n");
                            rtb.SelectionStart = rtb.Text.Length;
                        }
                        lbDebugLog.Items.Insert(0, $"[Edit] New line INSERTED successfully");
                        return;
                    }

                    if (lowerText.Contains("delete last word"))
                    {
                        if (focusedControl is TextBox txt)
                        {
                            if (!string.IsNullOrEmpty(txt.Text))
                            {
                                string val = txt.Text.TrimEnd();
                                // Find the last space OR newline (whichever comes last)
                                int lastSpace = val.LastIndexOf(' ');
                                int lastNewlineRN = val.LastIndexOf("\r\n");
                                int lastNewlineN = val.LastIndexOf('\n');
                                int lastSeparator = Math.Max(Math.Max(lastSpace, lastNewlineRN), lastNewlineN);
                                
                                if (lastSeparator >= 0)
                                {
                                    txt.Text = val.Substring(0, lastSeparator);
                                    lbDebugLog.Items.Insert(0, $"[Edit] Last word deleted (separator at {lastSeparator})");
                                }
                                else
                                {
                                    // No separator found - this is a single word, clear it
                                    txt.Text = "";
                                    lbDebugLog.Items.Insert(0, "[Edit] Last word deleted (single word cleared)");
                                }
                                txt.SelectionStart = txt.Text.Length;
                            }
                        }
                        else if (focusedControl is RichTextBox rtb)
                        {
                            if (!string.IsNullOrEmpty(rtb.Text))
                            {
                                string val = rtb.Text.TrimEnd();
                                // Find the last space OR newline (whichever comes last)
                                int lastSpace = val.LastIndexOf(' ');
                                int lastNewlineRN = val.LastIndexOf("\r\n");
                                int lastNewlineN = val.LastIndexOf('\n');
                                int lastSeparator = Math.Max(Math.Max(lastSpace, lastNewlineRN), lastNewlineN);
                                
                                if (lastSeparator >= 0)
                                {
                                    rtb.Text = val.Substring(0, lastSeparator);
                                    lbDebugLog.Items.Insert(0, $"[Edit] Last word deleted (separator at {lastSeparator})");
                                }
                                else
                                {
                                    // No separator found - this is a single word, clear it
                                    rtb.Text = "";
                                    lbDebugLog.Items.Insert(0, "[Edit] Last word deleted (single word cleared)");
                                }
                                rtb.SelectionStart = rtb.Text.Length;
                            }
                        }
                        return;
                    }

                    // Check for "delete last line" with flexible matching
                    bool isDeleteLastLineCommand = lowerText.Contains("delete last line") ||
                                                  lowerText.Contains("remove last line") ||
                                                  lowerText.Contains("clear last line");
                    
                    if (isDeleteLastLineCommand)
                    {
                        if (focusedControl is TextBox txt)
                        {
                            if (!string.IsNullOrEmpty(txt.Text))
                            {
                                // Handle both \r\n (Windows) and \n (Unix) line endings
                                string textContent = txt.Text;
                                int lastNewLine = Math.Max(textContent.LastIndexOf("\r\n"), textContent.LastIndexOf("\n"));
                                
                                if (lastNewLine >= 0)
                                {
                                    // Found a newline - delete from that point
                                    txt.Text = textContent.Substring(0, lastNewLine);
                                    lbDebugLog.Items.Insert(0, $"[Edit] Last line deleted (newline at pos {lastNewLine})");
                                }
                                else
                                {
                                    // No newline found - this is the only line, clear it
                                    txt.Text = "";
                                    lbDebugLog.Items.Insert(0, "[Edit] Last line deleted (single line cleared)");
                                }
                                txt.SelectionStart = txt.Text.Length;
                            }
                        }
                        else if (focusedControl is RichTextBox rtb)
                        {
                            if (!string.IsNullOrEmpty(rtb.Text))
                            {
                                // Handle both \r\n (Windows) and \n (Unix) line endings
                                string textContent = rtb.Text;
                                
                                // Check for \r\n first (Windows style)
                                int lastCRLF = textContent.LastIndexOf("\r\n");
                                int lastLF = textContent.LastIndexOf('\n');
                                
                                // If we found \r\n, use that position. Otherwise use \n position
                                int lastNewLine = -1;
                                if (lastCRLF >= 0)
                                {
                                    // Found \r\n - this is the start of the last line break
                                    lastNewLine = lastCRLF;
                                }
                                else if (lastLF >= 0)
                                {
                                    // Found \n but not \r\n - use \n position
                                    lastNewLine = lastLF;
                                }
                                
                                if (lastNewLine >= 0)
                                {
                                    // Found a newline - delete from that point
                                    rtb.Text = textContent.Substring(0, lastNewLine);
                                    lbDebugLog.Items.Insert(0, $"[Edit] Last line deleted (newline at pos {lastNewLine}, text length was {textContent.Length})");
                                }
                                else
                                {
                                    // No newline found - this is the only line, clear it
                                    rtb.Text = "";
                                    lbDebugLog.Items.Insert(0, "[Edit] Last line deleted (single line cleared)");
                                }
                                rtb.SelectionStart = rtb.Text.Length;
                            }
                        }
                        return;
                    }

                    // If not escaping/clearing/editing, treat as dictation
                    if (focusedControl is TextBox txt2)
                    {
                        TrackVoiceHistory(txt2, text);
                        txt2.AppendText((txt2.TextLength > 0 ? " " : "") + text);
                    }
                    else if (focusedControl is RichTextBox rtb2)
                    {
                        TrackVoiceHistory(rtb2, text);
                        rtb2.AppendText((rtb2.TextLength > 0 ? " " : "") + text);
                    }
                    return; // STOP processing as a command
                }
            }
            // --- End Multiline Lock Mode ---
            
            // --- 0.1 Grid Lock Mode ---
            // Check if focused control is ANY DataGridView registered in voice map
            if (focusedControl is DataGridView dgvLock && IsGridInVoiceMap(dgvLock.Name))
            {
                // Check if user wants to exit (including "Exit Grade" alias)
                if (lowerText.Contains("exit grid") || lowerText.Contains("stop grid") || lowerText.Contains("exit grade"))
                {
                     this.ActiveControl = null; // Unfocus logic
                     lbDebugLog.Items.Insert(0, "[System] Exited Grid Mode");
                     return;
                }
                
                // Allow "Clear All" in grid too
                 if (lowerText.Equals("clear all", StringComparison.OrdinalIgnoreCase))
                {
                    PerformClearAll();
                    return;
                }
                
                // Allow "Clear" (Active Cell) in grid
                if (lowerText.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    PerformClearActive();
                    return;
                }

                // Get grid configuration for current grid
                GridConfig? currentGridConfig = _gridConfigs.ContainsKey(dgvLock.Name) ? _gridConfigs[dgvLock.Name] : null;
                
                // Allow Navigation within Grid (e.g., "Case 1 BPD", "Row 2 HC") to work normally
                bool isGridNavigation = false;
                if (currentGridConfig != null)
                {
                    string navPattern = $@"^{currentGridConfig.RowPattern}\s+\d+";
                    isGridNavigation = System.Text.RegularExpressions.Regex.IsMatch(lowerText, navPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
                
                if (isGridNavigation)
                {
                     // Let it fall through to the Navigation Logic below
                }
                else
                {
                    // Column + Value Entry (e.g. "HC 10", "BPD 25", "FL20")
                    // Build dynamic pattern based on valid columns for this grid
                    string columnPattern = currentGridConfig != null && currentGridConfig.ValidColumns.Count > 0
                        ? string.Join("|", currentGridConfig.ValidColumns)
                        : "BPD|HC|AC|FL|FHR"; // Default fallback
                    
                    var colValMatch = System.Text.RegularExpressions.Regex.Match(lowerText, $@"^({columnPattern})\s*(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (colValMatch.Success)
                    {
                        string targetColName = colValMatch.Groups[1].Value;
                        string valueToEnter = colValMatch.Groups[2].Value;

                        // Find and switch to column
                        foreach(DataGridViewColumn col in dgvLock.Columns)
                        {
                             if (col.Name.Equals(targetColName, StringComparison.OrdinalIgnoreCase) || 
                                 col.HeaderText.Equals(targetColName, StringComparison.OrdinalIgnoreCase))
                             {
                                 if (dgvLock.CurrentCell != null)
                                 {
                                     // 1. Switch to Column
                                     dgvLock.CurrentCell = dgvLock[col.Index, dgvLock.CurrentCell.RowIndex];
                                     
                                     // 2. Enter Value
                                     if (!dgvLock.CurrentCell.ReadOnly)
                                     {
                                         dgvLock.CurrentCell.Value = valueToEnter;
                                         lbDebugLog.Items.Insert(0, $"[Grid Entry] {targetColName} = {valueToEnter}");
                                         
                                         // 3. Auto-Advance
                                         int currentRow = dgvLock.CurrentCell.RowIndex;
                                         int currentCol = dgvLock.CurrentCell.ColumnIndex;
                                         for (int i = currentCol + 1; i < dgvLock.Columns.Count; i++)
                                         {
                                             if (dgvLock.Columns[i].Visible)
                                             {
                                                 dgvLock.CurrentCell = dgvLock[i, currentRow];
                                                 break;
                                             }
                                         }
                                     }
                                 }
                                 return;
                             }
                        }
                    }

                    // Column Selection Jumping (e.g. just "HC", "AC")
                    string[] possibleCols = currentGridConfig != null && currentGridConfig.ValidColumns.Count > 0
                        ? currentGridConfig.ValidColumns.ToArray()
                        : new[] { "BPD", "HC", "AC", "FL", "FHR" }; // Default fallback
                    
                    string? targetColNameSimple = possibleCols.FirstOrDefault(c => c.Equals(lowerText, StringComparison.OrdinalIgnoreCase));
                    
                    if (targetColNameSimple != null)
                    {
                         // Find this column
                         foreach(DataGridViewColumn col in dgvLock.Columns)
                         {
                             if (col.Name.Equals(targetColNameSimple, StringComparison.OrdinalIgnoreCase) || 
                                 col.HeaderText.Equals(targetColNameSimple, StringComparison.OrdinalIgnoreCase))
                             {
                                 if (dgvLock.CurrentCell != null)
                                 {
                                     dgvLock.CurrentCell = dgvLock[col.Index, dgvLock.CurrentCell.RowIndex];
                                     lbDebugLog.Items.Insert(0, $"[Grid Nav] Switched to {targetColNameSimple}");
                                 }
                                 return;
                             }
                         }
                    }

                    // Treat as INPUT for the current cell
                    if (dgvLock.CurrentCell != null && !dgvLock.CurrentCell.ReadOnly)
                    {
                        dgvLock.CurrentCell.Value = text;
                        lbDebugLog.Items.Insert(0, $"[Grid Input] {text}");
                        
                        // Auto-Advance to next column
                        int currentRow = dgvLock.CurrentCell.RowIndex;
                        int currentCol = dgvLock.CurrentCell.ColumnIndex;
                        
                        // Find next visible column
                        for (int i = currentCol + 1; i < dgvLock.Columns.Count; i++)
                        {
                            if (dgvLock.Columns[i].Visible)
                            {
                                dgvLock.CurrentCell = dgvLock[i, currentRow];
                                break;
                            }
                        }
                    }
                    return; // Stop processing
                }
            }
            // --- End Grid Lock ---

            // --- 0.5 Global Commands (Clear / Clear All) ---
            if (lowerText.Equals("clear all", StringComparison.OrdinalIgnoreCase))
            {
                PerformClearAll();
                return;
            }

            if (lowerText.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                PerformClearActive();
                return;
            }
            
            // --- 0.6 Grid Navigation (Dynamic from JSON: "Case 1 BPD", "Row 2 HC", etc.) ---
            // Try all configured grid row patterns
            foreach (var gridConfig in _gridConfigs.Values)
            {
                // Build dynamic regex: "case \d+ columnName" or "row \d+ columnName"
                string pattern = $@"^{gridConfig.RowPattern}\s+(\d+)\s+(.+)$";
                var gridNavMatch = System.Text.RegularExpressions.Regex.Match(lowerText, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (gridNavMatch.Success)
                {
                    if (int.TryParse(gridNavMatch.Groups[1].Value, out int rowNum))
                    {
                        string colName = gridNavMatch.Groups[2].Value.Trim().ToUpper();
                        
                        // Validate column name against configured columns
                        if (gridConfig.ValidColumns.Count == 0 || gridConfig.ValidColumns.Contains(colName))
                        {
                            // Find the actual grid control
                            Control? gridControl = ControlActionMapper.FindControlByName(this, gridConfig.GridName);
                            
                            if (gridControl is DataGridView targetGrid)
                            {
                                // Row number to zero-based index
                                int rowIndex = rowNum - 1;
                                
                                if (rowIndex >= 0 && rowIndex < targetGrid.Rows.Count)
                                {
                                    // Find column by name or header text
                                    DataGridViewColumn? targetCol = null;
                                    foreach(DataGridViewColumn col in targetGrid.Columns)
                                    {
                                        if (col.HeaderText.Equals(colName, StringComparison.OrdinalIgnoreCase) || 
                                            col.Name.Equals(colName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            targetCol = col;
                                            break;
                                        }
                                    }
                                    
                                    if (targetCol != null)
                                    {
                                        EnsureControlVisible(targetGrid);
                                        targetGrid.Focus();
                                        targetGrid.CurrentCell = targetGrid[targetCol.Index, rowIndex];
                                        lbDebugLog.Items.Insert(0, $"[Grid] Selected {gridConfig.RowPattern} {rowNum} {colName} in {gridConfig.GridName}");
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // 1. Identify Keyword and Action from Map
            string matchedKeyword = "";
            string targetControlName = "";
            ControlActionMapper.ControlAction action = ControlActionMapper.ControlAction.Focus; // Use Mapper Enum

            // Sort keys by length descending to match "Gender Male" before "Male"
            foreach (var key in _voiceMap.Keys.OrderByDescending(k => k.Length)) 
            {
               // Check if text STARTS with keyword (e.g. "BPD 20") or EQUALS it
               if (lowerText.StartsWith(key, StringComparison.OrdinalIgnoreCase))
               {
                   matchedKeyword = key;
                   var mapping = _voiceMap[key];
                   targetControlName = mapping.ControlName;
                   action = (ControlActionMapper.ControlAction)mapping.Action; // Cast or Update Map Definition
                   lbDebugLog.Items.Insert(0, $"[Match] Key: '{key}' -> Control: '{targetControlName}'");
                   break;
               }
            }

            // check for navigation commands if no direct match found (backwards compatibility or global commands)
            if (string.IsNullOrEmpty(targetControlName))
            {
                // 2. Dynamic Discovery (No explicit map needed)
                if (ControlActionMapper.TryFindControlByVoice(this, lowerText, out Control dynamicControl, out string dynamicValue))
                {
                    targetControlName = dynamicControl.Name;
                    correctedText = dynamicValue; // The payload (e.g. "20")
                    
                    // Determine Action based on Type
                    // Determine Action based on Type
                    if (dynamicControl is ComboBox) 
                    {
                        action = ControlActionMapper.ControlAction.SelectByValue;
                    }
                    else if (dynamicControl is RadioButton || dynamicControl is Button || dynamicControl is CheckBox) 
                    {
                        action = ControlActionMapper.ControlAction.Click;
                    }
                    else 
                    {
                         // Default to Type for TextBoxes, BUT if value is empty, just Focus
                         if (string.IsNullOrEmpty(correctedText)) action = ControlActionMapper.ControlAction.Focus;
                         else action = ControlActionMapper.ControlAction.Type;
                    }
                    
                    DebugLogger.Log($"[Dynamic] Matched: {dynamicControl.Name}, Value: '{correctedText}', Action: {action}");

                    matchedKeyword = dynamicControl.Name; // Just for logging

                    matchedKeyword = dynamicControl.Name; // Just for logging
                    lbDebugLog.Items.Insert(0, $"[Wrapper] Dynamic Match: '{lowerText}' -> {targetControlName}");
                    
                    // NEW: Ensure Tab is Selected!
                    Control parent = dynamicControl.Parent;
                    while (parent != null)
                    {
                        if (parent is TabPage page && page.Parent is TabControl tabCtrl)
                        {
                            tabCtrl.SelectedTab = page;
                            DebugLogger.Log($"[Tab] Switched to {page.Text}");
                            break;
                        }
                        parent = parent.Parent;
                    }
                }
                else
                {
                    DebugLogger.Log($"[Dynamic] NO MATCH for '{lowerText}'");
                }
            }



            if (string.IsNullOrEmpty(targetControlName))
            {
                // Fallback: Check if we have an ACTIVE CONTROL that accepts text
                // This enables "Sequential" flow: "BPD" (Focus) -> "10" (Type)
                // Also enables ComboBox value selection: "Scan Type" (Opens dropdown) -> "Growth" (Selects item)
                
                Control? focused = this.ActiveControl;
                while (focused is ContainerControl container && container.ActiveControl != null)
                {
                    focused = container.ActiveControl;
                }

                if (focused != null && (focused is TextBox || focused is ComboBox || focused is RichTextBox))
                {
                    // Treat input as value for the focused control
                    lbDebugLog.Items.Insert(0, $"[Context] Typing '{text}' into {focused.Name}");
                    
                    // Use Mapper with "Type" action (or SelectByValue for Combo)
                    ControlActionMapper.ControlAction contextAction = ControlActionMapper.ControlAction.Type;
                    ComboBox? focusedCombo = null;
                    if (focused is ComboBox combo) 
                    {
                        focusedCombo = combo;
                        contextAction = ControlActionMapper.ControlAction.SelectByValue;
                        lbDebugLog.Items.Insert(0, $"[ComboBox] Attempting to select '{text}' in {combo.Name} (Items: {combo.Items.Count})");
                    }
                    
                    ControlActionMapper.ExecuteAction(this, focused.Name, contextAction, text);
                    
                    // For ComboBox, log success/failure
                    if (focusedCombo != null)
                    {
                        if (focusedCombo.SelectedIndex >= 0)
                        {
                            lbDebugLog.Items.Insert(0, $"[ComboBox] Selected: {focusedCombo.SelectedItem} (Index: {focusedCombo.SelectedIndex})");
                        }
                        else
                        {
                            lbDebugLog.Items.Insert(0, $"[ComboBox] No match found for '{text}'");
                        }
                    }
                    return;
                }

                // Global commands only if not consumed as active input
                ProcessGlobalCommands(text);
                return;
            }

            // 2. Find the Control (Let Mapper handle verification, but we need it for ensure visible/highlight)
            // Actually Mapper does finding internally, but we want to Highlight/Ensure Visible FIRST.
            // So we still need to find it here, OR we move Highlight/ensure visible INTO Mapper?
            // "EnsureVisible" is app-specific (TabControl logic). Mapper is generic.
            // So we keep Find logic here for Visibility/Highlight, then delegate Action.
            
            Control[] foundControls = this.Controls.Find(targetControlName, true);
            Control targetControl = null;
            
            // Special handling for controls in FinalImpressionControl (cmbTemplate, rtbSummary, etc.)
            if (foundControls.Length == 0 && finalImpressionControl != null)
            {
                lbDebugLog.Items.Insert(0, $"[Search] Looking in FinalImpressionControl for: {targetControlName}");
                
                // Try direct property access for public controls
                if (targetControlName == "cmbTemplate")
                {
                    targetControl = finalImpressionControl.cmbTemplate;
                    lbDebugLog.Items.Insert(0, $"[Found] cmbTemplate in FinalImpressionControl via property");
                }
                else if (targetControlName == "rtbSummary")
                {
                    targetControl = finalImpressionControl.rtbSummary;
                    lbDebugLog.Items.Insert(0, $"[Found] rtbSummary in FinalImpressionControl via property");
                }
                else if (targetControlName == "txtHeading")
                {
                    targetControl = finalImpressionControl.txtHeading;
                    lbDebugLog.Items.Insert(0, $"[Found] txtHeading in FinalImpressionControl via property");
                }
                else if (targetControlName == "rbNormal")
                {
                    targetControl = finalImpressionControl.rbNormal;
                    lbDebugLog.Items.Insert(0, $"[Found] rbNormal in FinalImpressionControl via property");
                }
                else if (targetControlName == "rbAbnormal")
                {
                    targetControl = finalImpressionControl.rbAbnormal;
                    lbDebugLog.Items.Insert(0, $"[Found] rbAbnormal in FinalImpressionControl via property");
                }
                else
                {
                    // Try recursive search in finalImpressionControl
                    var foundInFinal = finalImpressionControl.Controls.Find(targetControlName, true);
                    if (foundInFinal.Length > 0)
                    {
                        targetControl = foundInFinal[0];
                        lbDebugLog.Items.Insert(0, $"[Found] {targetControlName} in FinalImpressionControl via recursive search");
                    }
                }
            }
            else if (foundControls.Length > 0)
            {
                targetControl = foundControls[0];
                lbDebugLog.Items.Insert(0, $"[Found] {targetControlName} via standard search");
            }
            
            // Special handling for measurement fields in FinalImpressionControl
            if (targetControl == null && targetControlName.StartsWith("field") && targetControlName.EndsWith("_value"))
            {
                lbDebugLog.Items.Insert(0, $"[Search] Looking for measurement field textbox: {targetControlName}");
                
                // Extract field name (e.g., "fieldBPD_value" -> "fieldBPD")
                string fieldName = targetControlName.Replace("_value", "");
                
                // Search in finalImpressionControl
                if (finalImpressionControl != null)
                {
                    var field = finalImpressionControl.Controls.Find(fieldName, true).FirstOrDefault();
                    if (field is MeasurementFieldControl measurementField)
                    {
                        // Only get the textbox - checkbox and dropdown will be auto-filled
                        targetControl = measurementField.ValueTextBox;
                        lbDebugLog.Items.Insert(0, $"[Found] Measurement field: {fieldName} -> TextBox (auto-fills checkbox & dropdown)");
                    }
                    else
                    {
                        lbDebugLog.Items.Insert(0, $"[Error] Field not found or wrong type: {fieldName}");
                    }
                }
                else
                {
                    lbDebugLog.Items.Insert(0, $"[Error] finalImpressionControl is null");
                }
            }
            
            if (targetControl == null) 
            {
                lbDebugLog.Items.Insert(0, $"[Error] targetControl is null after all searches, cannot proceed");
                return;
            }

            // 3. Extract Value (Payload)
            // Start with the raw corrected text to preserve casing (e.g. "BPD 60" or "Remarks Looks Good")
            string valuePayload = correctedText.Trim(); 
            
            // If matched via Keyword Map, we need to remove the keyword from the payload!
            if (!string.IsNullOrEmpty(matchedKeyword))
            {
                // To be extremely safe, check if the string starts with the matched keyword (case insensitive)
                if (valuePayload.StartsWith(matchedKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    // Cut it out entirely
                    valuePayload = valuePayload.Substring(matchedKeyword.Length).Trim();
                }
                else
                {
                    // Fallback Regex if there were strange space characters not caught by StartsWith (e.g. "bp d" mapping vs "bpd 60" text)
                    string pattern = "^" + System.Text.RegularExpressions.Regex.Escape(matchedKeyword) + @"\s*";
                    valuePayload = System.Text.RegularExpressions.Regex.Replace(valuePayload, pattern, "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                }
            }

            // Fallback for Dynamic Dictionary Match
            // (If not from Keyboard map but we have dynamic value)
            if (string.IsNullOrEmpty(matchedKeyword) && string.IsNullOrEmpty(valuePayload) && correctedText.Length > 0)
            {
                 valuePayload = correctedText;
            }
            
            // 4. Pre-Action Checks (Visibility, Highlight)
            EnsureControlVisible(targetControl);
            HighlightField(targetControl);

            // 5. Execute Action via Mapper
            // If we have a payload, we might need to change Action?
            // e.g. "BPD 20" -> Focus BPD (Action=Focus) but we have "20".
            // If mapping says "Focus" but we have value, we should probably "Type" or "Set".
            // Implementation Decision: 
            // - If Action is Focus AND Payload exists -> Change to Type/SelectedByValue?
            // - Or rely on mapping to be smart?
            // The current mapping (VoiceControlAction) has "Type" and "SelectByValue".
            // But for "BPD" the mapping is "Focus".
            // Legacy logic: if (valuePayload.Length > 0) -> Type/Select logic.
            
            // Let's adapt the action dynamically:
            // CRITICAL FIX: Only change action if payload is NOT empty!
            if (!string.IsNullOrEmpty(valuePayload))
            {
                // If explicit action is Dropdown, keep it! (Unless user said "Scan Type Growth")
                if (action != ControlActionMapper.ControlAction.Dropdown)
                {
                    if (targetControl is TextBox || targetControl is RichTextBox) 
                        action = ControlActionMapper.ControlAction.Type;
                    else if (targetControl is ComboBox || targetControl is ListBox) 
                        action = ControlActionMapper.ControlAction.SelectByValue;
                }
            }

            // Execute via Mapper
            ControlActionMapper.ExecuteAction(this, targetControlName, action, valuePayload);

            // Special Case: Helper for ComboBox Dropdown
            // The mapping might use "Focus", but for ComboBox we want "Dropdown" to help the user.
            if (targetControl is ComboBox cb && (action == ControlActionMapper.ControlAction.Focus || action == ControlActionMapper.ControlAction.Dropdown))
            {
                // Use a Timer to ensure the tab switch and focus and painting are 100% complete
                // Increased to 500ms to allow full render. 200ms was too fast for some machines.
                var t = new System.Windows.Forms.Timer { Interval = 500 }; 
                t.Tick += (s, e) => 
                { 
                    t.Stop(); 
                    
                    // Force Logic: Aggressive Expansion
                    try
                    {
                        cb.Focus(); 
                        cb.DroppedDown = true; 
                        
                        // Fallback: If Property didn't work (common in some UI states), use Alt+Down
                        if (!cb.DroppedDown)
                        {
                            SendKeys.Send("%{DOWN}");
                            lbDebugLog.Items.Insert(0, $"[System] Dropdown {cb.Name} forced via SendKeys");
                        }
                        else
                        {
                            lbDebugLog.Items.Insert(0, $"[System] Dropdown {cb.Name} expanded via Property");
                        }
                    }
                    catch (Exception ex)
                    {
                        lbDebugLog.Items.Insert(0, $"[System] Dropdown Error: {ex.Message}");
                    }
                    
                    t.Dispose(); 
                };
                t.Start();
            }

            // 6. Post-Action Logic (Sequential Entry)
            // If we just entered a value into a Grid-like field (TextBox in Tab 1), maybe auto-advance?
            // "BPD 20" -> Enter 20 -> Go to HC?
            // Only if Sequential Entry is enabled.
            // For now, respect legacy behavior (User didn't ask to change this specifically, just "Integrate").
            // But user *did* ask for "Sequential Grid Entry" in the previous task. 
            // In the "Speech" based entry (this block), we might want similar logic?
            // Original code didn't have auto-advance for main fields, only Grid.
            // We stick to current behavior.
            
            lbDebugLog.Items.Insert(0, $"[Action] {targetControlName} -> {action} : {valuePayload}");
        }

        // Helper to get all controls recursively for "Clear All"
        private IEnumerable<Control> GetAllControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (Control child in GetAllControls(c))
                {
                    yield return child;
                }
            }
        }

        private void EnsureControlVisible(Control control)
        {
            Control? parent = control.Parent;
            while (parent != null)
            {
                if (parent is TabPage page && parent.Parent is TabControl tabControl)
                {
                    tabControl.SelectedTab = page;
                }
                parent = parent.Parent;
            }
        }

        private void ProcessGlobalCommands(string text)
        {
            string lower = text.ToLower();
            // Fallback for global clear
            if (lower.Contains("clear all") || lower.Contains("reset"))
            {
                 // Call existing clear logic via reflection or simple method if kept?
                 // For now, simple inline
                 foreach(var tb in this.Controls.Find("txtBpd", true)) ((TextBox)tb).Clear(); 
                 // ... (Simplified for brevity, can re-add full clear logic if requested)
            }
             
            // Navigation Fallback
            if (lower.Contains("scan details")) mainTabControl.SelectedTab = tabScanDetails;
            if (lower.Contains("biometry")) mainTabControl.SelectedTab = tabBiometry;
        }
        
        private void PerformClearAll()
        {
            // Clear standard controls
            foreach (Control c in GetAllControls(this))
            {
                if (c is TextBox tb) tb.Clear();
                if (c is RichTextBox rtb) rtb.Clear();
                if (c is ComboBox cb) cb.SelectedIndex = -1;
                if (c is RadioButton rb) rb.Checked = false;
                if (c is CheckBox chk) chk.Checked = false;
            }

            // Clear Grid Values (retain Cases)
            if (dgvBiometryHistory != null && dgvBiometryHistory.DataSource is List<BiometryCaseData> dataList)
            {
                foreach (var item in dataList)
                {
                    // Clear properties - simplistic approach or reflection
                    item.BPD = ""; item.HC = ""; item.AC = ""; item.FL = ""; item.FHR = ""; 
                    // ... other fields if any
                }
                // Refresh grid to show empty values
                dgvBiometryHistory.Refresh();
            }
            
            lbDebugLog.Items.Insert(0, "[System] Form & Grid Cleared");
        }

        private void PerformClearActive()
        {
            Control? focusedControl = this.ActiveControl;
            while (focusedControl is ContainerControl container && container.ActiveControl != null)
            {
                focusedControl = container.ActiveControl;
            }

            if (focusedControl is TextBox tb) tb.Clear();
            if (focusedControl is RichTextBox rtb) rtb.Clear();
            if (focusedControl is ComboBox cb) cb.SelectedIndex = -1;
            if (focusedControl is DataGridView dgv && dgv.CurrentCell != null) 
            {
                if (!dgv.CurrentCell.ReadOnly) dgv.CurrentCell.Value = null;
            }
            
            lbDebugLog.Items.Insert(0, "[System] Active Field Cleared");
        }

        private bool IsWord(string input, string word) =>
            System.Text.RegularExpressions.Regex.IsMatch(input, $@"\b{word}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Helper to check if control is a multiline text entry control (for lock mode)
        private bool IsMultilineTextControl(Control? control)
        {
            if (control == null) return false;
            
            // RichTextBox is always multiline
            if (control is RichTextBox) return true;
            
            // Check if TextBox is multiline
            if (control is TextBox tb && tb.Multiline) return true;
            
            return false;
        }
        
        // Helper to check if a control name is registered as a grid in voice map
        private bool IsGridInVoiceMap(string controlName)
        {
            if (string.IsNullOrEmpty(controlName)) return false;
            
            // Check if any voice command maps to this control name and it's a DataGridView
            return _voiceMap.Values.Any(v => v.ControlName.Equals(controlName, StringComparison.OrdinalIgnoreCase));
        }
        
        // Helper to find the first DataGridView registered in voice map
        private DataGridView? FindGridFromVoiceMap()
        {
            // Get all unique control names that are DataGridViews
            var gridControlNames = _voiceMap.Values
                .Select(v => v.ControlName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            
            foreach (var controlName in gridControlNames)
            {
                Control? control = ControlActionMapper.FindControlByName(this, controlName);
                if (control is DataGridView dgv)
                {
                    return dgv;
                }
            }
            
            return null;
        }
        
        private void TrackVoiceHistory(Control control, string addedText)
        {
            string previousText = "";
            int selectionStart = 0;
            
            if (control is TextBox txt)
            {
                previousText = txt.Text;
                selectionStart = txt.SelectionStart;
            }
            else if (control is RichTextBox rtb)
            {
                previousText = rtb.Text;
                selectionStart = rtb.SelectionStart;
            }
            
            var entry = new VoiceHistoryEntry
            {
                Control = control,
                PreviousText = previousText,
                AddedText = addedText,
                SelectionStart = selectionStart
            };
            
            _voiceHistory.Push(entry);
            
            // Limit history size
            if (_voiceHistory.Count > MAX_HISTORY_SIZE)
            {
                var historyList = _voiceHistory.ToList();
                historyList.RemoveAt(historyList.Count - 1);
                _voiceHistory = new Stack<VoiceHistoryEntry>(historyList.Reverse<VoiceHistoryEntry>());
            }
            
            lbDebugLog.Items.Insert(0, $"[History] Tracked: '{addedText}' in {control.Name}");
        }
        
        private void UndoLastVoiceEntry()
        {
            if (_voiceHistory.Count == 0)
            {
                lbDebugLog.Items.Insert(0, "[Undo] No history to undo");
                return;
            }
            
            var entry = _voiceHistory.Pop();
            
            if (entry.Control == null)
            {
                lbDebugLog.Items.Insert(0, "[Undo] Control no longer exists");
                return;
            }
            
            if (entry.Control is TextBox txt)
            {
                txt.Text = entry.PreviousText;
                txt.SelectionStart = entry.SelectionStart;
                lbDebugLog.Items.Insert(0, $"[Undo] Restored previous text in {txt.Name}");
            }
            else if (entry.Control is RichTextBox rtb)
            {
                rtb.Text = entry.PreviousText;
                rtb.SelectionStart = entry.SelectionStart;
                lbDebugLog.Items.Insert(0, $"[Undo] Restored previous text in {rtb.Name}");
            }
        }
        
        private void DeleteLastVoiceEntry()
        {
            if (_voiceHistory.Count == 0)
            {
                lbDebugLog.Items.Insert(0, "[Delete] No history to delete");
                return;
            }
            
            var entry = _voiceHistory.Pop();
            
            if (entry.Control == null)
            {
                lbDebugLog.Items.Insert(0, "[Delete] Control no longer exists");
                return;
            }
            
            // Remove the added text from the end
            if (entry.Control is TextBox txt)
            {
                string currentText = txt.Text;
                string addedTextWithSpace = (entry.PreviousText.Length > 0 ? " " : "") + entry.AddedText;
                
                if (currentText.EndsWith(addedTextWithSpace))
                {
                    txt.Text = currentText.Substring(0, currentText.Length - addedTextWithSpace.Length);
                    txt.SelectionStart = txt.Text.Length;
                    lbDebugLog.Items.Insert(0, $"[Delete] Removed: '{entry.AddedText}' from {txt.Name}");
                }
                else if (currentText.EndsWith(entry.AddedText))
                {
                    txt.Text = currentText.Substring(0, currentText.Length - entry.AddedText.Length);
                    txt.SelectionStart = txt.Text.Length;
                    lbDebugLog.Items.Insert(0, $"[Delete] Removed: '{entry.AddedText}' from {txt.Name}");
                }
                else
                {
                    // Text was modified, restore previous state
                    txt.Text = entry.PreviousText;
                    txt.SelectionStart = entry.SelectionStart;
                    lbDebugLog.Items.Insert(0, $"[Delete] Text changed, restored previous in {txt.Name}");
                }
            }
            else if (entry.Control is RichTextBox rtb)
            {
                string currentText = rtb.Text;
                string addedTextWithSpace = (entry.PreviousText.Length > 0 ? " " : "") + entry.AddedText;
                
                if (currentText.EndsWith(addedTextWithSpace))
                {
                    rtb.Text = currentText.Substring(0, currentText.Length - addedTextWithSpace.Length);
                    rtb.SelectionStart = rtb.Text.Length;
                    lbDebugLog.Items.Insert(0, $"[Delete] Removed: '{entry.AddedText}' from {rtb.Name}");
                }
                else if (currentText.EndsWith(entry.AddedText))
                {
                    rtb.Text = currentText.Substring(0, currentText.Length - entry.AddedText.Length);
                    rtb.SelectionStart = rtb.Text.Length;
                    lbDebugLog.Items.Insert(0, $"[Delete] Removed: '{entry.AddedText}' from {rtb.Name}");
                }
                else
                {
                    // Text was modified, restore previous state
                    rtb.Text = entry.PreviousText;
                    rtb.SelectionStart = entry.SelectionStart;
                    lbDebugLog.Items.Insert(0, $"[Delete] Text changed, restored previous in {rtb.Name}");
                }
            }
        }
        
        private void ReplaceWordInControl(Control? control, string oldWord, string newWord)
        {
            if (control == null)
            {
                lbDebugLog.Items.Insert(0, "[Replace] No control focused");
                return;
            }
            
            if (control is TextBox txt)
            {
                if (txt.Text.IndexOf(oldWord, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string previousText = txt.Text;
                    txt.Text = System.Text.RegularExpressions.Regex.Replace(
                        txt.Text, 
                        System.Text.RegularExpressions.Regex.Escape(oldWord), 
                        newWord, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    txt.SelectionStart = txt.Text.Length;
                    lbDebugLog.Items.Insert(0, $"[Replace] '{oldWord}' → '{newWord}' in {txt.Name}");
                }
                else
                {
                    lbDebugLog.Items.Insert(0, $"[Replace] '{oldWord}' not found in {txt.Name}");
                }
            }
            else if (control is RichTextBox rtb)
            {
                if (rtb.Text.IndexOf(oldWord, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string previousText = rtb.Text;
                    rtb.Text = System.Text.RegularExpressions.Regex.Replace(
                        rtb.Text, 
                        System.Text.RegularExpressions.Regex.Escape(oldWord), 
                        newWord, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    rtb.SelectionStart = rtb.Text.Length;
                    lbDebugLog.Items.Insert(0, $"[Replace] '{oldWord}' → '{newWord}' in {rtb.Name}");
                }
                else
                {
                    lbDebugLog.Items.Insert(0, $"[Replace] '{oldWord}' not found in {rtb.Name}");
                }
            }
        }
        
        private void NavigateToNextField(Control currentControl, bool forward)
        {
            Control? nextControl = GetNextFocusableControl(this, currentControl, forward);
            if (nextControl != null)
            {
                nextControl.Focus();
            }
        }
        
        private Control? GetNextFocusableControl(Control container, Control? currentControl, bool forward)
        {
            // Get all focusable controls in tab order
            var focusableControls = GetAllFocusableControls(container);
            if (focusableControls.Count == 0) return null;
            
            if (currentControl == null)
            {
                // Return first or last control
                return forward ? focusableControls[0] : focusableControls[focusableControls.Count - 1];
            }
            
            // Find current control index
            int currentIndex = focusableControls.IndexOf(currentControl);
            if (currentIndex == -1) return null;
            
            // Get next/previous index with wrapping
            int nextIndex;
            if (forward)
            {
                nextIndex = (currentIndex + 1) % focusableControls.Count;
            }
            else
            {
                nextIndex = currentIndex - 1;
                if (nextIndex < 0) nextIndex = focusableControls.Count - 1;
            }
            
            return focusableControls[nextIndex];
        }
        
        private List<Control> GetAllFocusableControls(Control container)
        {
            var focusableControls = new List<Control>();
            
            void CollectControls(Control parent)
            {
                foreach (Control child in parent.Controls)
                {
                    // Check if it's a focusable input control
                    if (child is TextBox || child is RichTextBox || child is ComboBox || 
                        child is DateTimePicker || child is NumericUpDown)
                    {
                        if (child.Enabled && child.Visible && child.TabStop)
                        {
                            focusableControls.Add(child);
                        }
                    }
                    
                    // Recursively search child controls (including UserControls)
                    if (child.HasChildren)
                    {
                        CollectControls(child);
                    }
                }
            }
            
            CollectControls(container);
            
            // Sort by TabIndex for proper order
            focusableControls.Sort((a, b) => 
            {
                // Compare TabIndex, considering parent TabIndex as well
                int tabA = GetEffectiveTabIndex(a);
                int tabB = GetEffectiveTabIndex(b);
                if (tabA != tabB) return tabA.CompareTo(tabB);
                
                // If TabIndex is same, sort by position (top-to-bottom, left-to-right)
                Point posA = a.PointToScreen(Point.Empty);
                Point posB = b.PointToScreen(Point.Empty);
                if (posA.Y != posB.Y) return posA.Y.CompareTo(posB.Y);
                return posA.X.CompareTo(posB.X);
            });
            
            return focusableControls;
        }
        
        private int GetEffectiveTabIndex(Control control)
        {
            int tabIndex = control.TabIndex;
            Control? parent = control.Parent;
            
            // Add parent TabIndex values to get effective position
            while (parent != null && parent != this)
            {
                tabIndex += parent.TabIndex * 1000; // Weight parent TabIndex higher
                parent = parent.Parent;
            }
            
            return tabIndex;
        }

        private void HighlightField(Control tb)
        {
            tb.BackColor = Color.LightYellow;
            var t = new Timer { Interval = 1000 };
            t.Tick += (s, e) => { tb.BackColor = Color.White; t.Stop(); };
            t.Start();
        }

        private async void GetIcd_Click(object sender, EventArgs e)
        {
            string query = finalImpressionControl.rtbSummary.Text.Trim();

            if (string.IsNullOrWhiteSpace(query))
            {
                mainTabControl.SelectedTab = tabFinalImpression;
                finalImpressionControl.rtbSummary.Focus();
                MessageBox.Show(
                    "Please enter final impression summary first.",
                    "ICD Recommendation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string previousText = btnGetIcd.Text;
            btnGetIcd.Enabled = false;
            btnGetIcd.Text = "Loading...";

            try
            {
                _icdRequestCts?.Cancel();
                _icdRequestCts?.Dispose();
                _icdRequestCts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(35));

                string category = "both";
                if (chkNormal.Checked && !chkAbnormal.Checked) category = "normal";
                else if (!chkNormal.Checked && chkAbnormal.Checked) category = "abnormal";
                else if (!chkNormal.Checked && !chkAbnormal.Checked)
                {
                    MessageBox.Show("Please select at least one category (Normal or Abnormal).", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    btnGetIcd.Enabled = true;
                    btnGetIcd.Text = previousText;
                    return;
                }

                var request = new IcdPredictionRequest
                {
                    Query = query,
                    Description = string.IsNullOrWhiteSpace(txtComments.Text) ? null : txtComments.Text.Trim(),
                    Category = category
                };

                var result = await _icdApiClient.PredictIcdAsync(request, _icdRequestCts.Token);
                ShowIcdRecommendationResult(result);
            }
            catch (OperationCanceledException)
            {
                lbDebugLog.Items.Insert(0, "[ICD] Request canceled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to fetch ICD recommendations.{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Make sure ICD API is running on http://127.0.0.1:8080",
                    "ICD Recommendation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                lbDebugLog.Items.Insert(0, $"[ICD] Error: {ex.Message}");
            }
            finally
            {
                btnGetIcd.Text = previousText;
                btnGetIcd.Enabled = true;
            }
        }

        private void ShowIcdRecommendationResult(IcdPredictionResponse result)
        {
            if (result.IcdCodes == null || result.IcdCodes.Count == 0)
            {
                MessageBox.Show(
                    "No ICD recommendations found for this impression.",
                    "ICD Recommendations",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                lbDebugLog.Items.Insert(0, "[ICD] No recommendations returned.");
                return;
            }

            using (var selectionForm = new IcdSelectionForm(result))
            {
                if (selectionForm.ShowDialog(this) == DialogResult.OK)
                {
                    var selected = selectionForm.SelectedCodes;
                    if (selected != null && selected.Any())
                    {
                        string addedInfo = string.Join(", ", selected.Select(c => $"{c.Code} {c.Description}"));
                        finalImpressionControl.rtbSummary.AppendText($"{Environment.NewLine}{Environment.NewLine}Selected ICD Codes: {addedInfo}");
                        lbDebugLog.Items.Insert(0, $"[ICD] Added {selected.Count} codes to summary.");
                    }
                }
                else
                {
                    lbDebugLog.Items.Insert(0, "[ICD] Selection canceled.");
                }
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            using (var context = new AppDbContext())
            {
                string gender = "Others";
                if (rbMale.Checked) gender = "Male";
                if (rbFemale.Checked) gender = "Female";

                var report = new Report
                {
                    PatientId = _patientId,
                    VisitDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    BPD = txtBpd.Text,
                    HC = txtHc.Text,
                    AC = txtAc.Text,
                    FL = txtFl.Text,
                    FHR = txtFhr.Text,
                    Comments = txtComments.Text,
                    GA = txtGa.Text,
                    Placenta = txtPlacenta.Text,
                    AFI = txtAfi.Text,
                    Presentation = txtPresentation.Text,
                    EFW = txtEfw.Text,
                    ScanType = cmbScanType.Text,
                    Gender = gender,
                    BiometryHistory = JsonSerializer.Serialize(dgvBiometryHistory.DataSource)
                };
                context.Reports.Add(report);
                context.SaveChanges();
            }
            MessageBox.Show("Report Saved.");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ReportForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _icdRequestCts?.Cancel();
            _icdRequestCts?.Dispose();
            if (_icdApiClient is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void LoadPatient()
        {
            using (var context = new AppDbContext())
            {
                var patient = context.Patients.FirstOrDefault(p => p.Id == _patientId);
                if (patient != null) lblPatientInfo.Text = $"Report for: {patient.Name} (ID: {patient.IdNumber})";
            }
        }
        private bool _isRecordingLocally = false;
        private void Record_Click(object sender, EventArgs e)
        {
            var mainForm = this.MdiParent as MainForm;
            if (mainForm?.AugnitoService == null)
            {
                lbDebugLog.Items.Insert(0, "[Record] Error: Audio Service Unavailable.");
                return;
            }

            if (!_isRecordingLocally)
            {
                string folder = Path.Combine(Application.StartupPath, "Recordings");
                Directory.CreateDirectory(folder);
                string file = Path.Combine(folder, $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
                
                mainForm.AugnitoService.StartLocalRecording(file);
                
                _isRecordingLocally = true;
                btnRecord.Text = "Stop Rec";
                btnRecord.BackColor = Color.Red;
                btnRecord.ForeColor = Color.White;
                lbDebugLog.Items.Insert(0, $"[Record] Started: {Path.GetFileName(file)}");
            }
            else
            {
                mainForm.AugnitoService.StopLocalRecording();
                _isRecordingLocally = false;
                btnRecord.Text = "Record Audio";
                btnRecord.BackColor = Color.LightGray;
                btnRecord.ForeColor = Color.Black;
                lbDebugLog.Items.Insert(0, "[Record] Stopped.");
            }
        }
    }
    
    public class BiometryCaseData
    {
        public string Patient { get; set; }
        public string BPD { get; set; }
        public string HC { get; set; }
        public string AC { get; set; }
        public string FL { get; set; }
        public string FHR { get; set; }
    }
}
