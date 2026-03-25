using System;
using System.Drawing;
using System.Windows.Forms;

namespace SonocareWinForms
{
    public class MeasurementFieldControl : UserControl
    {
        // Controls
        private Label lblFieldName;
        private TextBox txtValue;
        private CheckBox chkAbnormal;
        private Label lblUnit;
        private ComboBox cmbOptions;
        private Label lblNote;

        // Public accessors for voice control
        public TextBox ValueTextBox => txtValue;
        public CheckBox AbnormalCheckBox => chkAbnormal;
        public ComboBox OptionsComboBox => cmbOptions;

        // Threshold properties for auto-evaluation
        public double NormalMin { get; set; } = 0;
        public double NormalMax { get; set; } = double.MaxValue;
        public bool AutoEvaluate { get; set; } = false;

        // Properties
        public string FieldName
        {
            get => lblFieldName.Text;
            set => lblFieldName.Text = value;
        }

        public string Value
        {
            get => txtValue.Text;
            set => txtValue.Text = value;
        }

        public bool IsAbnormal
        {
            get => chkAbnormal.Checked;
            set => chkAbnormal.Checked = value;
        }

        public string Unit
        {
            get => lblUnit.Text;
            set => lblUnit.Text = value;
        }

        public string[] Options
        {
            set
            {
                cmbOptions.Items.Clear();
                if (value != null && value.Length > 0)
                {
                    cmbOptions.Items.AddRange(value);
                    cmbOptions.Visible = true;
                }
                else
                {
                    cmbOptions.Visible = false;
                }
            }
        }

        public string SelectedOption
        {
            get => cmbOptions.SelectedItem?.ToString() ?? string.Empty;
            set => cmbOptions.SelectedItem = value;
        }

        public string Note
        {
            get => lblNote.Text;
            set
            {
                lblNote.Text = value;
                lblNote.Visible = !string.IsNullOrEmpty(value);
            }
        }

        // Events
        public event EventHandler ValueChanged;
        public event EventHandler AbnormalityChanged;
        public event EventHandler OptionChanged;

        public MeasurementFieldControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.lblFieldName = new Label();
            this.txtValue = new TextBox();
            this.chkAbnormal = new CheckBox();
            this.lblUnit = new Label();
            this.cmbOptions = new ComboBox();
            this.lblNote = new Label();

            this.SuspendLayout();

            // Label - Field Name
            this.lblFieldName.Text = "Field:";
            this.lblFieldName.Location = new Point(5, 8);
            this.lblFieldName.Size = new Size(80, 20);
            this.lblFieldName.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.lblFieldName.TextAlign = ContentAlignment.MiddleLeft;

            // TextBox - Value
            this.txtValue.Location = new Point(90, 5);
            this.txtValue.Size = new Size(100, 23);
            this.txtValue.Name = "txtValue";
            this.txtValue.Font = new Font("Segoe UI", 9F);
            this.txtValue.TabStop = true;
            this.txtValue.TextChanged += TxtValue_TextChanged;

            // Label - Unit
            this.lblUnit.Text = "unit";
            this.lblUnit.Location = new Point(195, 8);
            this.lblUnit.Size = new Size(40, 20);
            this.lblUnit.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            this.lblUnit.TextAlign = ContentAlignment.MiddleLeft;
            this.lblUnit.ForeColor = Color.Gray;

            // CheckBox - Abnormal
            this.chkAbnormal.Text = "Abnormal";
            this.chkAbnormal.Location = new Point(240, 5);
            this.chkAbnormal.Size = new Size(85, 23);
            this.chkAbnormal.Name = "chkAbnormal";
            this.chkAbnormal.Font = new Font("Segoe UI", 8.5F);
            this.chkAbnormal.CheckedChanged += (s, e) => AbnormalityChanged?.Invoke(this, e);

            // ComboBox - Options
            this.cmbOptions.Location = new Point(330, 5);
            this.cmbOptions.Size = new Size(120, 23);
            this.cmbOptions.Name = "cmbOptions";
            this.cmbOptions.Font = new Font("Segoe UI", 9F);
            this.cmbOptions.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbOptions.Visible = false;
            this.cmbOptions.SelectedIndexChanged += (s, e) => OptionChanged?.Invoke(this, e);

            // Label - Note
            this.lblNote.Text = "";
            this.lblNote.Location = new Point(455, 8);
            this.lblNote.Size = new Size(150, 20);
            this.lblNote.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            this.lblNote.ForeColor = Color.DarkBlue;
            this.lblNote.TextAlign = ContentAlignment.MiddleLeft;
            this.lblNote.Visible = false;

            // Add controls to UserControl
            this.Controls.Add(this.lblFieldName);
            this.Controls.Add(this.txtValue);
            this.Controls.Add(this.lblUnit);
            this.Controls.Add(this.chkAbnormal);
            this.Controls.Add(this.cmbOptions);
            this.Controls.Add(this.lblNote);

            // UserControl properties
            this.Name = "MeasurementFieldControl";
            this.Size = new Size(610, 33);
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = Color.WhiteSmoke;
            this.TabStop = false; // Don't focus the container, focus the internal controls

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // Helper method to set all properties at once
        public void SetField(string fieldName, string unit, string[] options = null, string note = null)
        {
            this.FieldName = fieldName;
            this.Unit = unit;
            if (options != null)
                this.Options = options;
            if (note != null)
                this.Note = note;
        }
        
        // Helper to configure thresholds and enable auto-evaluation
        public void SetThresholds(double normalMin, double normalMax, bool autoEvaluate = true)
        {
            this.NormalMin = normalMin;
            this.NormalMax = normalMax;
            this.AutoEvaluate = autoEvaluate;
        }
        
        // Helper to set the internal textbox name for voice control
        public void SetTextBoxName(string name)
        {
            this.txtValue.Name = name;
        }
        
        // Helper to set the internal checkbox name for voice control
        public void SetCheckBoxName(string name)
        {
            this.chkAbnormal.Name = name;
        }
        
        // Helper to set the internal combobox name for voice control
        public void SetComboBoxName(string name)
        {
            this.cmbOptions.Name = name;
        }

        // Event handler for value changes
        private void TxtValue_TextChanged(object sender, EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
            
            if (AutoEvaluate)
            {
                EvaluateValue();
            }
        }

        // Auto-evaluation logic based on thresholds
        private void EvaluateValue()
        {
            string valueText = txtValue.Text.Trim();
            
            // Try to parse the value
            if (double.TryParse(valueText, out double numericValue))
            {
                bool isAbnormal = false;
                string status = "";
                
                // Determine if abnormal and which status
                if (numericValue < NormalMin)
                {
                    isAbnormal = true;
                    status = "Below average";
                }
                else if (numericValue > NormalMax)
                {
                    isAbnormal = true;
                    status = "Above average";
                }
                else
                {
                    isAbnormal = false;
                    status = "Within normal";
                }
                
                // Update checkbox
                chkAbnormal.Checked = isAbnormal;
                
                // Update dropdown if the option exists
                if (cmbOptions.Visible && !string.IsNullOrEmpty(status))
                {
                    for (int i = 0; i < cmbOptions.Items.Count; i++)
                    {
                        if (cmbOptions.Items[i].ToString().Equals(status, StringComparison.OrdinalIgnoreCase))
                        {
                            cmbOptions.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                // Clear selections if value is not valid
                chkAbnormal.Checked = false;
                cmbOptions.SelectedIndex = -1;
            }
        }
    }
}
