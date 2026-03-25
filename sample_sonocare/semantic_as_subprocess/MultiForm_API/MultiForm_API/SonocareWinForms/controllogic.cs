using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;

/// <summary>
/// Uses Reflection to identify the exact control type at runtime from its Name,
/// then dispatches the correct Focus / Click / Select action automatically.
/// No prefix guessing — GetType() returns the real control type.
/// </summary>
public static class ControlActionMapper
{
    // ---------------------------------------------------------------
    // Action enum
    // ---------------------------------------------------------------
    public enum ControlAction { Focus, Click, Select, Type, Dropdown, SelectByValue }


    // STEP 3 — Main entry: auto-detect type via reflection + dispatch action
    // ---------------------------------------------------------------

    /// <summary>
    /// Finds the control by name on the form, uses reflection to get its exact
    /// runtime type, then calls the correct action handler automatically.
    /// Works for any control type — no naming conventions required.
    /// </summary>
    public static bool ExecuteAction(Form form, string controlName, ControlAction action, object value = null)
    {
        Control control = FindControlByName(form, controlName);

        if (control == null)
        {
            Console.WriteLine($"[ActionMapper] '{controlName}' not found on form.");
            return false;
        }

        // Reflection gives us the TRUE runtime type — not just "Control"
        Type runtimeType = control.GetType();
        Console.WriteLine($"[ActionMapper] '{controlName}' detected as [{runtimeType.Name}] → Action: {action}");

        DispatchByReflectedType(control, runtimeType, action, value);
        return true;
    }

    // ---------------------------------------------------------------
    // STEP 4 — Dispatcher: routes to handler based on reflected type
    // ---------------------------------------------------------------

    private static void DispatchByReflectedType(Control control, Type type, ControlAction action, object value)
    {
        // IsAssignableFrom covers subclasses (e.g. custom TextBox inheriting TextBox)
        // Order matters: more-derived types must be checked before base types

        if (typeof(MaskedTextBox).IsAssignableFrom(type)) HandleMaskedTextBox((MaskedTextBox)control, action, value);
        else if (typeof(RichTextBox).IsAssignableFrom(type)) HandleRichTextBox((RichTextBox)control, action, value);
        else if (typeof(TextBox).IsAssignableFrom(type)) HandleTextBox((TextBox)control, action, value);
        else if (typeof(NumericUpDown).IsAssignableFrom(type)) HandleNumericUpDown((NumericUpDown)control, action, value);
        else if (typeof(CheckedListBox).IsAssignableFrom(type)) HandleCheckedListBox((CheckedListBox)control, action, value);
        else if (typeof(ListBox).IsAssignableFrom(type)) HandleListBox((ListBox)control, action, value);
        else if (typeof(ComboBox).IsAssignableFrom(type)) HandleComboBox((ComboBox)control, action, value);
        else if (typeof(CheckBox).IsAssignableFrom(type)) HandleCheckBox((CheckBox)control, action, value);
        else if (typeof(RadioButton).IsAssignableFrom(type)) HandleRadioButton((RadioButton)control, action, value);
        else if (typeof(Button).IsAssignableFrom(type)) HandleButton((Button)control, action, value);
        else if (typeof(DataGridView).IsAssignableFrom(type)) HandleDataGridView((DataGridView)control, action, value);
        else if (typeof(DateTimePicker).IsAssignableFrom(type)) HandleDateTimePicker((DateTimePicker)control, action, value);
        else if (typeof(TabControl).IsAssignableFrom(type)) HandleTabControl((TabControl)control, action, value);
        else if (typeof(TreeView).IsAssignableFrom(type)) HandleTreeView((TreeView)control, action, value);
        else if (typeof(ListView).IsAssignableFrom(type)) HandleListView((ListView)control, action, value);
        else if (typeof(PictureBox).IsAssignableFrom(type)) HandlePictureBox((PictureBox)control, action, value);
        else if (typeof(TrackBar).IsAssignableFrom(type)) HandleTrackBar((TrackBar)control, action, value);
        else if (typeof(ProgressBar).IsAssignableFrom(type)) HandleProgressBar((ProgressBar)control, action, value);
        else if (typeof(GroupBox).IsAssignableFrom(type)) HandleGroupBox((GroupBox)control, action, value);
        else if (typeof(Panel).IsAssignableFrom(type)) HandlePanel((Panel)control, action, value);
        else if (typeof(Label).IsAssignableFrom(type))
            Console.WriteLine($"[ActionMapper] Label '{control.Name}' is not interactive.");
        else
            HandleFallback(control, action, value);
    }



    // ---------------------------------------------------------------
    // Individual control handlers
    // ---------------------------------------------------------------

    private static void HandleTextBox(TextBox tb, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: tb.Focus(); break;
            case ControlAction.Click: tb.Focus(); tb.SelectionStart = tb.Text.Length; break;
            case ControlAction.Select: tb.Focus(); tb.SelectAll(); break;
            case ControlAction.Type:
                tb.Focus();
                string text = value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    // Smart spacing: Add space if text present and not trailing space
                    if (tb.TextLength > 0 && !tb.Text.EndsWith(" ")) tb.AppendText(" ");
                    tb.AppendText(text);
                }
                break;
        }
    }

    private static void HandleRichTextBox(RichTextBox rtb, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: rtb.Focus(); break;
            case ControlAction.Click: rtb.Focus(); rtb.SelectionStart = rtb.Text.Length; break;
            case ControlAction.Select: rtb.Focus(); rtb.SelectAll(); break;
            case ControlAction.Type:
                rtb.Focus();
                string text = value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    if (rtb.TextLength > 0 && !rtb.Text.EndsWith(" ")) rtb.AppendText(" ");
                    rtb.AppendText(text);
                }
                break;
        }
    }

    private static void HandleMaskedTextBox(MaskedTextBox mtb, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: mtb.Focus(); break;
            case ControlAction.Click: mtb.Focus(); break;
            case ControlAction.Select: mtb.Focus(); mtb.SelectAll(); break;
            case ControlAction.Type:
                mtb.Focus();
                if (value != null) mtb.Text = value.ToString(); // MaskedTextBox usually replaces
                break;
        }
    }

    private static void HandleNumericUpDown(NumericUpDown nud, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: nud.Focus(); break;
            case ControlAction.Click: nud.Focus(); break;
            case ControlAction.Select: nud.Focus(); nud.Select(0, nud.Text.Length); break;
            case ControlAction.Type:
            case ControlAction.SelectByValue:
                nud.Focus();
                if (decimal.TryParse(value?.ToString(), out decimal val)) nud.Value = val;
                break;
        }
    }

    private static void HandleComboBox(ComboBox cb, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: cb.Focus(); break;
            case ControlAction.Click: 
            case ControlAction.Dropdown:
                cb.Focus(); 
                cb.DroppedDown = true; 
                break;
            case ControlAction.Select:
                cb.Focus();
                if (cb.Items.Count > 0 && cb.SelectedIndex < 0) cb.SelectedIndex = 0;
                break;
            case ControlAction.SelectByValue:
            case ControlAction.Type:
                cb.Focus();
                string search = value?.ToString() ?? "";
                
                // Try exact match first (case-insensitive)
                int index = -1;
                for (int i = 0; i < cb.Items.Count; i++)
                {
                    string itemText = cb.Items[i]?.ToString() ?? "";
                    if (itemText.Equals(search, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }
                
                // If no exact match, try prefix match (case-insensitive)
                if (index < 0)
                {
                    index = cb.FindStringExact(search); // Exact match, case-insensitive
                }
                
                // If still no match, try prefix/contains match
                if (index < 0)
                {
                    for (int i = 0; i < cb.Items.Count; i++)
                    {
                        string itemText = cb.Items[i]?.ToString() ?? "";
                        if (itemText.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            index = i;
                            break;
                        }
                    }
                }
                
                if (index >= 0) cb.SelectedIndex = index;
                else cb.Text = search; // Fallback to typing if DropDown style allows
                break;
        }
    }

    private static void HandleListBox(ListBox lb, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: lb.Focus(); break;
            case ControlAction.Click: lb.Focus(); break;
            case ControlAction.Select:
                lb.Focus();
                if (lb.Items.Count > 0 && lb.SelectedIndex < 0) lb.SelectedIndex = 0;
                break;
             case ControlAction.SelectByValue:
                lb.Focus();
                string search = value?.ToString() ?? "";
                int index = lb.FindString(search);
                if (index >= 0) lb.SelectedIndex = index;
                break;
        }
    }

    private static void HandleCheckedListBox(CheckedListBox clb, ControlAction action, object value)
    {
        // Similar to ListBox
        switch (action)
        {
            case ControlAction.Focus: clb.Focus(); break;
            case ControlAction.Click: clb.Focus(); break;
            case ControlAction.Select:
                clb.Focus();
                if (clb.Items.Count > 0) clb.SelectedIndex = 0;
                break;
        }
    }

    private static void HandleCheckBox(CheckBox chk, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: chk.Focus(); break;
            case ControlAction.Click: chk.Focus(); chk.Checked = !chk.Checked; break;
            case ControlAction.Select: chk.Focus(); chk.Checked = true; break;
            case ControlAction.SelectByValue:
                // "true", "yes", "on", "checked" -> true
                string v = value?.ToString()?.ToLower() ?? "";
                chk.Checked = (v == "true" || v == "yes" || v == "on" || v == "checked");
                break;
        }
    }

    private static void HandleRadioButton(RadioButton rb, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: rb.Focus(); break;
            case ControlAction.Click:
            case ControlAction.Select: rb.Focus(); rb.Checked = true; break;
             // SelectByValue not usually relevant for single RadioButton, but maybe group? 
             // Logic usually handled by container. For single RB, "Select" means check it.
        }
    }

    private static void HandleButton(Button btn, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: btn.Focus(); break;
            case ControlAction.Click: btn.Focus(); btn.PerformClick(); break;
            case ControlAction.Select: btn.Focus(); break;
        }
    }

    private static void HandleDataGridView(DataGridView dgv, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: dgv.Focus(); break;
            case ControlAction.Click: dgv.Focus(); break;
            case ControlAction.Select:
                dgv.Focus();
                if (dgv.Rows.Count > 0 && dgv.Columns.Count > 0)
                {
                    dgv.ClearSelection();
                    dgv.Rows[0].Selected = true;
                    dgv.CurrentCell = dgv.Rows[0].Cells[0];
                }
                break;
        }
    }

    private static void HandleDateTimePicker(DateTimePicker dtp, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: dtp.Focus(); break;
            case ControlAction.Click: dtp.Focus(); break;
            case ControlAction.Select: dtp.Focus(); SendKeys.Send("%{DOWN}"); break;
             case ControlAction.Type:
            case ControlAction.SelectByValue:
                dtp.Focus();
                if (DateTime.TryParse(value?.ToString(), out DateTime dt)) dtp.Value = dt;
                break;
        }
    }

    private static void HandleTabControl(TabControl tc, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: tc.Focus(); break;
            case ControlAction.Click: tc.Focus(); break;
            case ControlAction.Select:
                tc.Focus();
                if (tc.TabPages.Count > 0) tc.SelectedIndex = 0;
                break;
        }
    }

    private static void HandleTreeView(TreeView tv, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: tv.Focus(); break;
            case ControlAction.Click: tv.Focus(); break;
            case ControlAction.Select:
                tv.Focus();
                if (tv.Nodes.Count > 0) tv.SelectedNode = tv.Nodes[0];
                break;
        }
    }

    private static void HandleListView(ListView lv, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: lv.Focus(); break;
            case ControlAction.Click: lv.Focus(); break;
            case ControlAction.Select:
                lv.Focus();
                if (lv.Items.Count > 0)
                {
                    lv.Items[0].Selected = true;
                    lv.Items[0].Focused = true;
                    lv.EnsureVisible(0);
                }
                break;
        }
    }

    private static void HandlePictureBox(PictureBox pb, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: pb.Focus(); break;
            case ControlAction.Click: 
                // InvokeOnClick is protected, use Reflection
                var method = typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
                method?.Invoke(pb, new object[] { EventArgs.Empty });
                break;
            case ControlAction.Select: pb.Focus(); break;
        }
    }

    private static void HandleTrackBar(TrackBar trk, ControlAction action, object value)
    {
        switch (action)
        {
            case ControlAction.Focus: trk.Focus(); break;
            case ControlAction.Click: trk.Focus(); break;
            case ControlAction.Select: trk.Focus(); trk.Value = trk.Minimum; break;
        }
    }

    private static void HandleProgressBar(ProgressBar pb, ControlAction action, object value)
    {
        pb.Focus();
        Console.WriteLine($"[ActionMapper] ProgressBar '{pb.Name}' is display-only.");
    }

    private static void HandlePanel(Panel pnl, ControlAction action, object value)
    {
        foreach (Control child in pnl.Controls)
            if (child.CanFocus) { child.Focus(); break; }
    }

    private static void HandleGroupBox(GroupBox gb, ControlAction action, object value)
    {
        foreach (Control child in gb.Controls)
            if (child.CanFocus) { child.Focus(); break; }
    }

    private static void HandleFallback(Control ctrl, ControlAction action, object value)
    {
        Console.WriteLine($"[ActionMapper] No specific handler for {ctrl.GetType().Name}. Using generic.");
        if (ctrl.CanFocus) ctrl.Focus();
        if (action == ControlAction.Click)
        {
             var method = typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
             method?.Invoke(ctrl, new object[] { EventArgs.Empty });
        }
    }

    // ---------------------------------------------------------------
    // Utility: Recursive control finder by Name
    // ---------------------------------------------------------------

    /// <summary>
    /// Recursively searches all nested controls for one matching the given name.
    /// Works across Panels, GroupBoxes, TabPages, and any other containers.
    /// </summary>
    public static Control FindControlByName(Control parent, string name)
    {
        foreach (Control child in parent.Controls)
        {
            if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                return child;

            Control found = FindControlByName(child, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Attempts to find a control based on a voice command.
    /// Example: "Scan Type Growth" -> Matches "cmbScanType" (Key: "scantype") -> Returns Control + "Growth"
    /// </summary>
    public static bool TryFindControlByVoice(Control root, string commandText, out Control match, out string valuePayload)
    {
        match = null;
        valuePayload = "";
        if (string.IsNullOrEmpty(commandText)) return false;

        string cleanupCommand = commandText.ToLower().Replace(" ", ""); // "scantypegrowth"
        
        var allControls = GetAllControls(root);
        // Sort by length of name to match specific before generic (e.g. "ScanType" before "Scan")
        allControls.Sort((a, b) => (b.Name?.Length ?? 0).CompareTo(a.Name?.Length ?? 0));

        foreach (var ctrl in allControls)
        {
            string cleanName = ctrl.Name.ToLower(); 
            // Remove common prefixes
            // Remove common prefixes
            // WARNING: Do NOT include "lbl" here. Labels are non-interactive and will steal focus from "txtBpd".
            foreach (var prefix in new[] { "txt", "cmb", "rb", "chk", "dtp", "btn", "rtb" })
            {
                if (cleanName.StartsWith(prefix))
                {
                    cleanName = cleanName.Substring(prefix.Length);
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(cleanName)) continue;

            // Check match
            // "Scan Type Growth" (voice) vs "ScanType" (control)
            // Voice command usually "Starts With" the control name key
            // But we need to check the original command text for value separation
            
            // Strategy: Check if the cleaned command "Starts With" the cleaned control name
            if (cleanupCommand.StartsWith(cleanName))
            {
                match = ctrl;
                
                // Extract Value Payload
                // We need to find where the key ends in the original string.
                // This is tricky because "Scan Type" (2 words) -> "ScanType".
                // We can just remove the Key characters from the start? 
                // Approximate logic: 
                // command: "Scan Type Growth"
                // key: "scantype" (8 chars)
                // logic: Iterate original string, skip spaces, count 8 chars? 
                
                // Simpler fallback: pass original text, let Control Handler parse? 
                // No, handlers expect "Growth" not "Scan Type Growth".
                
                // Let's try to strip the key from the command based on length of "cleanName".
                // Count non-space chars in command until we match length of cleanName.
                
                // debug
                Console.WriteLine($"[TryFind] MATCH CANDIDATE: '{ctrl.Name}' (Clean: '{cleanName}') for Cmd: '{cleanupCommand}'");

                int charCount = 0;
                int splitIndex = 0;
                for (int i = 0; i < commandText.Length; i++)
                {
                    if (commandText[i] != ' ') charCount++;
                    if (charCount == cleanName.Length)
                    {
                        splitIndex = i + 1;
                        break;
                    }
                }
                
                if (splitIndex < commandText.Length)
                {
                    valuePayload = commandText.Substring(splitIndex).Trim();
                }
                
                return true;
            }
        }
        return false;
    }

    private static System.Collections.Generic.List<Control> GetAllControls(Control parent)
    {
        var list = new System.Collections.Generic.List<Control>();
        foreach (Control child in parent.Controls)
        {
            if (!string.IsNullOrEmpty(child.Name)) list.Add(child);
            list.AddRange(GetAllControls(child));
        }
        return list;
    }

    public static void UpdateControlValue(string controlName, string textValue, string activeForm)
    {
        var frm = Application.OpenForms.Cast<Form>()
            .FirstOrDefault(f => f.Name.Equals(activeForm, StringComparison.OrdinalIgnoreCase));
        if (frm == null) return;

        Control[] found = frm.Controls.Find(controlName, true);

        if (found.Length > 0)
        {
            Control ctrl = found[0];

            // Use Invoke if we are on a background thread
            if (ctrl.InvokeRequired)
            {
                ctrl.Invoke(new Action(() => ApplyValue(ctrl, textValue)));
            }
            else
            {
                ApplyValue(ctrl, textValue);
            }
        }
    }

    private static void ApplyValue(Control ctrl, string textValue)
    {
        ctrl.Focus();

        // Pattern Matching to handle different control types
        switch (ctrl)
        {
            case ComboBox cmb: // Generalized from TwigComboBox
                // For ComboBox, try to find the item in the list
                cmb.Text = textValue; // Fallback to text
                break;

            case TextBox txt: // Generalized from WinTextBox
            case RichTextBox rtx: // Generalized from RichTextBoxControl
                // These all inherit 'Text', so we can set them simply
                ctrl.Text = textValue;
                break;

            case CheckBox chk:
                // If the value is "true", "yes", or "1", check the box
                chk.Checked = (textValue.ToLower() == "true" || textValue == "1" || textValue.ToLower() == "yes");
                break;

            default:
                // Fallback for any other control type
                ctrl.Text = textValue;
                break;
        }
    }
}


