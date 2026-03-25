using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SonocareWinForms.Services;

namespace SonocareWinForms
{
    public class CalendarForm : Form
    {
        private TextBox txtPatientName;
        private TextBox txtPatientId;
        private TextBox txtVisitDate;
        private TextBox txtDOB; // New Field
        private RichTextBox txtNotes;
        private MonthCalendar calendar;
        
        // Helper to track which box is asking for date
        private TextBox _dateTargetBox; 

        public AugnitoService AugnitoService { get; set; } = null!;
        private Button btnFloatingMic;
        
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

        public CalendarForm()
        {
            InitializeComponent();
            this.Load += CalendarForm_Load;
        }

        private void CalendarForm_Load(object sender, EventArgs e)
        {
            if (AugnitoService != null)
            {
                btnFloatingMic = MicButtonHelper.CreateFloatingMic(this, (s, args) => 
                {
                    AugnitoService.ToggleListening();
                });
                
                AugnitoService.OnListeningStateChanged += (listening) => 
                {
                     MicButtonHelper.UpdateState(btnFloatingMic, listening);
                };
                
                // Initial State Sync
                MicButtonHelper.UpdateState(btnFloatingMic, AugnitoService.IsListening);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Sonocare - Calendar & Notes";
            this.Size = new Size(600, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // REMOVED: lblTitle ("Doctor's Schedule Notes")

            // Layout Constants
            int labelX = 50;
            int textX = 180;
            int startY = 30;
            int gap = 40;

            // 1. Patient Name
            var lblName = new Label { Text = "Patient Name:", Location = new Point(labelX, startY), AutoSize = true };
            txtPatientName = new TextBox { Location = new Point(textX, startY), Width = 200 };
            
            // 2. Patient ID
            int y = startY + gap;
            var lblId = new Label { Text = "Patient ID:", Location = new Point(labelX, y), AutoSize = true };
            txtPatientId = new TextBox { Location = new Point(textX, y), Width = 200 };

            // 3. DOB (New)
            y += gap;
            var lblDOB = new Label { Text = "Date of Birth:", Location = new Point(labelX, y), AutoSize = true };
            txtDOB = new TextBox { Location = new Point(textX, y), Width = 200 };
            // Event to show calendar
            txtDOB.Click += (s, e) => ShowCalendar(txtDOB);
            txtDOB.Enter += (s, e) => ShowCalendar(txtDOB);

            // 4. Visit Date
            y += gap;
            var lblDate = new Label { Text = "Visit Date:", Location = new Point(labelX, y), AutoSize = true };
            txtVisitDate = new TextBox { Location = new Point(textX, y), Width = 200 };
            // Event to show calendar
            txtVisitDate.Click += (s, e) => ShowCalendar(txtVisitDate);
            txtVisitDate.Enter += (s, e) => ShowCalendar(txtVisitDate);

            // 5. Notes
            y += gap + 20;
            var lblNotes = new Label
            {
                Text = "Daily Notes (Dictation Supported):",
                Location = new Point(labelX, y),
                AutoSize = true
            };
            
            y += 20;
            txtNotes = new RichTextBox
            {
                Location = new Point(labelX, y),
                Size = new Size(500, 150),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            // 6. Calendar (Hidden initially)
            calendar = new MonthCalendar
            {
                Location = new Point(textX, y), // Default pos, will move
                MaxSelectionCount = 1,
                Visible = false // HIDDEN
            };
            calendar.DateSelected += Calendar_DateSelected;

            var btnClose = new Button
            {
                Text = "Close",
                Location = new Point(450, 500),
                Size = new Size(100, 30)
            };
            btnClose.Click += (s, e) => this.Close();

            // Add Controls
            this.Controls.Add(lblName);
            this.Controls.Add(txtPatientName);
            this.Controls.Add(lblId);
            this.Controls.Add(txtPatientId);
            this.Controls.Add(lblDOB);
            this.Controls.Add(txtDOB); // Add DOB
            this.Controls.Add(lblDate);
            this.Controls.Add(txtVisitDate);
            this.Controls.Add(lblNotes);
            this.Controls.Add(txtNotes);
            this.Controls.Add(calendar); // Add Calendar logic
            this.Controls.Add(btnClose);
            
            // Clicking form hides calendar
            this.Click += (s,e) => calendar.Visible = false;
        }

        private void ShowCalendar(TextBox target)
        {
            _dateTargetBox = target;
            // Position calendar near the textbox
            calendar.Location = new Point(target.Left, target.Bottom + 5);
            calendar.Visible = true;
            calendar.BringToFront();
        }

        private void Calendar_DateSelected(object sender, DateRangeEventArgs e)
        {
            if (_dateTargetBox != null)
            {
                _dateTargetBox.Text = e.Start.ToShortDateString();
                calendar.Visible = false;
                
                // Move focus to next field?
                if (_dateTargetBox == txtDOB) txtVisitDate.Focus();
                else if (_dateTargetBox == txtVisitDate) txtNotes.Focus();
            }
        }

        public void HandleVoiceCommand(string text)
        {
            if (InvokeRequired) { Invoke(new Action<string>(HandleVoiceCommand), text); return; }

            string lowerText = text.ToLower();

            // CLEAR COMMANDS (Moved to top)
            if (lowerText.Equals("clear") || lowerText.Equals("clear field") || lowerText.Equals("delete"))
            {
                if (this.ActiveControl is TextBox tb) tb.Clear();
                return;
            }

            if (lowerText.Contains("clear all") || lowerText.Contains("reset form"))
            {
                txtPatientName.Clear();
                txtPatientId.Clear();
                txtDOB.Clear();
                txtVisitDate.Clear();
                txtNotes.Clear();
                calendar.Visible = false;
                return;
            }
            
            // FIELD NAVIGATION COMMANDS
            // "Next field" - Move to next control in tab order
            if (lowerText.Contains("next field") || lowerText.Contains("next box") || lowerText.Contains("go next"))
            {
                Control? currentControl = this.ActiveControl;
                
                if (currentControl != null)
                {
                    NavigateToNextField(currentControl, forward: true);
                }
                else
                {
                    // No control focused, focus the first one
                    var firstField = GetNextFocusableControl(this, null, forward: true);
                    if (firstField != null)
                    {
                        firstField.Focus();
                    }
                }
                return;
            }
            
            // "Previous field" - Move to previous control in tab order
            if (lowerText.Contains("previous field") || lowerText.Contains("last field") || lowerText.Contains("go back") || lowerText.Contains("back field"))
            {
                Control? currentControl = this.ActiveControl;
                
                if (currentControl != null)
                {
                    NavigateToNextField(currentControl, forward: false);
                }
                else
                {
                    // No control focused, focus the last one
                    var lastField = GetNextFocusableControl(this, null, forward: false);
                    if (lastField != null)
                    {
                        lastField.Focus();
                    }
                }
                return;
            }
            
            // 1. NOTES MODE (Dictation Support)
            if (this.ActiveControl == txtNotes)
            {
                if (lowerText.Contains("exit notes") || lowerText.Contains("exit note") ||
                    lowerText.Contains("stop notes") || lowerText.Contains("stop note") ||
                    lowerText.Contains("stop dictation"))
                {
                    // Exit Notes Mode
                    this.Focus(); // Unfocus text box
                    return;
                }
                
                // Handle editing commands in Notes mode
                bool isNextLineCommand = lowerText.Contains("next line") || 
                                       lowerText.Contains("nextline") ||
                                       lowerText.Contains("new line") ||
                                       lowerText.Contains("newline");
                
                if (isNextLineCommand)
                {
                    txtNotes.AppendText("\n");
                    txtNotes.SelectionStart = txtNotes.Text.Length;
                    return;
                }
                
                if (lowerText.Contains("delete last word"))
                {
                    if (!string.IsNullOrEmpty(txtNotes.Text))
                    {
                        string val = txtNotes.Text.TrimEnd();
                        // Find the last space OR newline (whichever comes last)
                        int lastSpace = val.LastIndexOf(' ');
                        int lastNewlineRN = val.LastIndexOf("\r\n");
                        int lastNewlineN = val.LastIndexOf('\n');
                        int lastSeparator = Math.Max(Math.Max(lastSpace, lastNewlineRN), lastNewlineN);
                        
                        if (lastSeparator >= 0)
                        {
                            txtNotes.Text = val.Substring(0, lastSeparator);
                        }
                        else
                        {
                            // No separator found - this is a single word, clear it
                            txtNotes.Text = "";
                        }
                        txtNotes.SelectionStart = txtNotes.Text.Length;
                    }
                    return;
                }
                
                bool isDeleteLastLineCommand = lowerText.Contains("delete last line") ||
                                              lowerText.Contains("remove last line") ||
                                              lowerText.Contains("clear last line");
                
                if (isDeleteLastLineCommand)
                {
                    if (!string.IsNullOrEmpty(txtNotes.Text))
                    {
                        // Handle both \r\n (Windows) and \n (Unix) line endings
                        string textContent = txtNotes.Text;
                        
                        // Check for \r\n first (Windows style)
                        int lastCRLF = textContent.LastIndexOf("\r\n");
                        int lastLF = textContent.LastIndexOf('\n');
                        
                        // If we found \r\n, use that position. Otherwise use \n position
                        int lastNewLine = -1;
                        if (lastCRLF >= 0)
                        {
                            lastNewLine = lastCRLF;
                        }
                        else if (lastLF >= 0)
                        {
                            lastNewLine = lastLF;
                        }
                        
                        if (lastNewLine >= 0)
                        {
                            // Delete from the last newline onwards (keeps previous lines)
                            txtNotes.Text = textContent.Substring(0, lastNewLine);
                        }
                        else
                        {
                            // No newline found - only one line exists, clear it
                            txtNotes.Text = "";
                        }
                        txtNotes.SelectionStart = txtNotes.Text.Length;
                    }
                    return;
                }
                
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
                    ReplaceWordInControl(txtNotes, oldWord, newWord);
                    return;
                }
                
                // Append everything else as dictation
                TrackVoiceHistory(txtNotes, text);
                txtNotes.AppendText(text + " ");
                return;
            }

            Control targetControl = null;
            string keyword = "";

            // Mapping Logic (Order matters for overlapping keywords like "date")
            if (lowerText.Contains("patient name") || lowerText.Contains("patient's name")) { targetControl = txtPatientName; keyword = "patient name"; }
            else if (lowerText.Contains("patient id") || lowerText.Contains("id number")) { targetControl = txtPatientId; keyword = "patient id"; }
            else if (lowerText.Contains("date of birth") || lowerText.Contains("dob")) { targetControl = txtDOB; keyword = "dob"; if (keyword=="dob" && !lowerText.Contains("dob")) keyword="date of birth"; }
            else if (lowerText.Contains("visit date")) { targetControl = txtVisitDate; keyword = "visit date"; }
            else if (lowerText.Contains("date")) { targetControl = txtVisitDate; keyword = "date"; } // Fallback for "date" -> visit date
            else if (lowerText.Contains("notes") || lowerText.Contains("note")) { targetControl = txtNotes; keyword = "note"; }

            // Hide Calendar if navigating
            if (targetControl != null && targetControl != _dateTargetBox)
            {
                calendar.Visible = false;
            }

            if (targetControl != null)
            {
                targetControl.Focus();
                string cleanText = text;
                
                // Keyword stripping
                if (lowerText.Contains(keyword))
                {
                     string pattern = $@"^\s*{keyword}[:\s-]*";
                     // Special case for multi-word
                     if (keyword == "date of birth") pattern = @"^\s*date of birth[:\s-]*";
                     
                     cleanText = System.Text.RegularExpressions.Regex.Replace(cleanText, pattern, "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }

                if (targetControl is RichTextBox rtb)
                {
                    rtb.AppendText(cleanText + " ");
                }
                else if (targetControl is TextBox tb)
                {
                    if (!string.IsNullOrWhiteSpace(cleanText)) tb.Text = cleanText.Trim();
                }
                
                HighlightField(targetControl);
            }
            // ... (Editing commands and default typing logic same as before) ...
            // EDITING COMMANDS for TextBox
            if (this.ActiveControl is TextBox activeTxt)
            {
                if (lowerText.Contains("next line"))
                {
                    activeTxt.AppendText(Environment.NewLine);
                    activeTxt.SelectionStart = activeTxt.Text.Length;
                    return;
                }
                if (lowerText.Contains("delete last word"))
                {
                   if (!string.IsNullOrEmpty(activeTxt.Text))
                   {
                        string val = activeTxt.Text.TrimEnd();
                        // Find the last space OR newline (whichever comes last)
                        int lastSpace = val.LastIndexOf(' ');
                        int lastNewlineRN = val.LastIndexOf("\r\n");
                        int lastNewlineN = val.LastIndexOf('\n');
                        int lastSeparator = Math.Max(Math.Max(lastSpace, lastNewlineRN), lastNewlineN);
                        
                        if (lastSeparator >= 0)
                        {
                            activeTxt.Text = val.Substring(0, lastSeparator);
                        }
                        else
                        {
                            // No separator found - this is a single word, clear it
                            activeTxt.Text = "";
                        }
                        activeTxt.SelectionStart = activeTxt.Text.Length;
                   }
                   return;
                }
                if (lowerText.Contains("delete last line"))
                {
                   if (!string.IsNullOrEmpty(activeTxt.Text))
                   {
                        int lastNewLine = activeTxt.Text.LastIndexOf(Environment.NewLine);
                        if (lastNewLine >= 0) activeTxt.Text = activeTxt.Text.Substring(0, lastNewLine);
                        else activeTxt.Text = "";
                        activeTxt.SelectionStart = activeTxt.Text.Length;
                   }
                   return;
                }
            }

            // Default: If no keyword matched, just type
            if (targetControl == null && this.ActiveControl is TextBox currentBox)
            {
                 currentBox.AppendText(text + " ");
            }
            else if (targetControl == null && this.ActiveControl is RichTextBox currentRtb)
            {
                 currentRtb.AppendText(text + " ");
            }
        }
        
        private void HighlightField(Control control)
        {
            control.BackColor = Color.LightYellow;
            var t = new Timer { Interval = 1000 };
            t.Tick += (s, e) => { control.BackColor = Color.White; t.Stop(); };
            t.Start();
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
        }
        
        private void UndoLastVoiceEntry()
        {
            if (_voiceHistory.Count == 0)
            {
                return;
            }
            
            var entry = _voiceHistory.Pop();
            
            if (entry.Control == null)
            {
                return;
            }
            
            if (entry.Control is TextBox txt)
            {
                txt.Text = entry.PreviousText;
                txt.SelectionStart = entry.SelectionStart;
            }
            else if (entry.Control is RichTextBox rtb)
            {
                rtb.Text = entry.PreviousText;
                rtb.SelectionStart = entry.SelectionStart;
            }
        }
        
        private void DeleteLastVoiceEntry()
        {
            if (_voiceHistory.Count == 0)
            {
                return;
            }
            
            var entry = _voiceHistory.Pop();
            
            if (entry.Control == null)
            {
                return;
            }
            
            // Remove the added text from the end
            if (entry.Control is TextBox txt)
            {
                string currentText = txt.Text;
                string addedTextWithSpace = entry.AddedText + " ";
                
                if (currentText.EndsWith(addedTextWithSpace))
                {
                    txt.Text = currentText.Substring(0, currentText.Length - addedTextWithSpace.Length);
                    txt.SelectionStart = txt.Text.Length;
                }
                else if (currentText.EndsWith(entry.AddedText))
                {
                    txt.Text = currentText.Substring(0, currentText.Length - entry.AddedText.Length);
                    txt.SelectionStart = txt.Text.Length;
                }
                else
                {
                    // Text was modified, restore previous state
                    txt.Text = entry.PreviousText;
                    txt.SelectionStart = entry.SelectionStart;
                }
            }
            else if (entry.Control is RichTextBox rtb)
            {
                string currentText = rtb.Text;
                string addedTextWithSpace = entry.AddedText + " ";
                
                if (currentText.EndsWith(addedTextWithSpace))
                {
                    rtb.Text = currentText.Substring(0, currentText.Length - addedTextWithSpace.Length);
                    rtb.SelectionStart = rtb.Text.Length;
                }
                else if (currentText.EndsWith(entry.AddedText))
                {
                    rtb.Text = currentText.Substring(0, currentText.Length - entry.AddedText.Length);
                    rtb.SelectionStart = rtb.Text.Length;
                }
                else
                {
                    // Text was modified, restore previous state
                    rtb.Text = entry.PreviousText;
                    rtb.SelectionStart = entry.SelectionStart;
                }
            }
        }
        
        private void ReplaceWordInControl(Control? control, string oldWord, string newWord)
        {
            if (control == null)
            {
                return;
            }
            
            if (control is TextBox txt)
            {
                if (txt.Text.IndexOf(oldWord, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    txt.Text = System.Text.RegularExpressions.Regex.Replace(
                        txt.Text, 
                        System.Text.RegularExpressions.Regex.Escape(oldWord), 
                        newWord, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    txt.SelectionStart = txt.Text.Length;
                }
            }
            else if (control is RichTextBox rtb)
            {
                if (rtb.Text.IndexOf(oldWord, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    rtb.Text = System.Text.RegularExpressions.Regex.Replace(
                        rtb.Text, 
                        System.Text.RegularExpressions.Regex.Escape(oldWord), 
                        newWord, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    rtb.SelectionStart = rtb.Text.Length;
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
    }
}
