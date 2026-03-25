using System;
using System.Drawing;
using System.Windows.Forms;

namespace SonocareWinForms
{
    public class FinalImpressionControl : UserControl
    {
        public Panel pnlMain;
        public Label lblHeading;
        public TextBox txtHeading;
        public Label lblFinding;
        public RadioButton rbNormal;
        public RadioButton rbAbnormal;
        public Label lblTemplate;
        public ComboBox cmbTemplate;
        public Label lblSummary;
        public RichTextBox rtbSummary;

        // Measurement Fields
        public Label lblMeasurements;
        public MeasurementFieldControl fieldBPD;
        public MeasurementFieldControl fieldHC;
        public MeasurementFieldControl fieldAC;
        public MeasurementFieldControl fieldFL;

        public FinalImpressionControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.pnlMain = new Panel();
            this.lblHeading = new Label();
            this.txtHeading = new TextBox();
            this.lblFinding = new Label();
            this.rbNormal = new RadioButton();
            this.rbAbnormal = new RadioButton();
            this.lblTemplate = new Label();
            this.cmbTemplate = new ComboBox();
            this.lblSummary = new Label();
            this.rtbSummary = new RichTextBox();

            // Initialize measurement fields
            this.lblMeasurements = new Label();
            this.fieldBPD = new MeasurementFieldControl();
            this.fieldHC = new MeasurementFieldControl();
            this.fieldAC = new MeasurementFieldControl();
            this.fieldFL = new MeasurementFieldControl();
            
            this.pnlMain.SuspendLayout();
            this.SuspendLayout();
            
            // Panel
            this.pnlMain.Dock = DockStyle.Fill;
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Padding = new Padding(10);
            
            int y = 20;
            int xLabel = 20;
            int xControl = 120;
            
            // 1. Text Box (Heading)
            this.lblHeading.Text = "Heading:";
            this.lblHeading.Location = new Point(xLabel, y);
            this.lblHeading.AutoSize = true;
            
            this.txtHeading.Location = new Point(xControl, y);
            this.txtHeading.Size = new Size(300, 20);
            this.txtHeading.Name = "txtHeading";
            
            y += 40;
            
            // 2. Radio Buttons (Finding)
            this.lblFinding.Text = "Finding:";
            this.lblFinding.Location = new Point(xLabel, y);
            this.lblFinding.AutoSize = true;
            
            this.rbNormal.Text = "Normal";
            this.rbNormal.Location = new Point(xControl, y);
            this.rbNormal.AutoSize = true;
            this.rbNormal.Name = "rbNormal";
            
            this.rbAbnormal.Text = "Abnormal";
            this.rbAbnormal.Location = new Point(xControl + 80, y); // Offset
            this.rbAbnormal.AutoSize = true;
            this.rbAbnormal.Name = "rbAbnormal";
            
            y += 40;
            
            // 3. Drop Down (Template)
            this.lblTemplate.Text = "Template:";
            this.lblTemplate.Location = new Point(xLabel, y);
            this.lblTemplate.AutoSize = true;
            
            this.cmbTemplate.Location = new Point(xControl, y);
            this.cmbTemplate.Size = new Size(300, 21);
            this.cmbTemplate.Name = "cmbTemplate";
            this.cmbTemplate.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbTemplate.Items.AddRange(new object[] { "Template A", "Template B", "Template C" });
            
            y += 40;

            // Measurements Section
            this.lblMeasurements.Text = "Measurements:";
            this.lblMeasurements.Location = new Point(xLabel, y);
            this.lblMeasurements.AutoSize = true;
            this.lblMeasurements.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            y += 30;

            // BPD Field
            this.fieldBPD.Name = "fieldBPD";
            this.fieldBPD.Location = new Point(xLabel, y);
            this.fieldBPD.SetField("BPD", "mm", new string[] { "Within normal", "Below average", "Above average" }, "Biparietal Diameter");
            this.fieldBPD.SetThresholds(50, 100, true); // Normal range: 50-100mm, auto-evaluate enabled
            this.fieldBPD.SetTextBoxName("fieldBPD_value");
            this.fieldBPD.Value = "75"; // Set default value for testing
            y += 38;

            // HC Field
            this.fieldHC.Name = "fieldHC";
            this.fieldHC.Location = new Point(xLabel, y);
            this.fieldHC.SetField("HC", "mm", new string[] { "Within normal", "Below average", "Above average" }, "Head Circumference");
            this.fieldHC.SetThresholds(200, 350, true); // Normal range: 200-350mm, auto-evaluate enabled
            this.fieldHC.SetTextBoxName("fieldHC_value");
            
            y += 38;

            // AC Field
            this.fieldAC.Name = "fieldAC";
            this.fieldAC.Location = new Point(xLabel, y);
            this.fieldAC.SetField("AC", "mm", new string[] { "Within normal", "Below average", "Above average" }, "Abdominal Circumference");
            this.fieldAC.SetThresholds(180, 340, true); // Normal range: 180-340mm, auto-evaluate enabled
            this.fieldAC.SetTextBoxName("fieldAC_value");
            
            y += 38;

            // FL Field
            this.fieldFL.Name = "fieldFL";
            this.fieldFL.Location = new Point(xLabel, y);
            this.fieldFL.SetField("FL", "mm", new string[] { "Within normal", "Below average", "Above average" }, "Femur Length");
            this.fieldFL.SetThresholds(40, 80, true); // Normal range: 40-80mm, auto-evaluate enabled
            this.fieldFL.SetTextBoxName("fieldFL_value");
            
            y += 45;
            
            // 4. Notes Box (RichTextBox) -> Renamed to Summary
            this.lblSummary.Text = "Summary:";
            this.lblSummary.Location = new Point(xLabel, y);
            this.lblSummary.AutoSize = true;
            
            this.rtbSummary.Location = new Point(xControl, y);
            this.rtbSummary.Size = new Size(300, 100);
            this.rtbSummary.Name = "rtbSummary";
            
            // Add controls to Panel
            this.pnlMain.Controls.Add(this.lblHeading);
            this.pnlMain.Controls.Add(this.txtHeading);
            this.pnlMain.Controls.Add(this.lblFinding);
            this.pnlMain.Controls.Add(this.rbNormal);
            this.pnlMain.Controls.Add(this.rbAbnormal);
            this.pnlMain.Controls.Add(this.lblTemplate);
            this.pnlMain.Controls.Add(this.cmbTemplate);
            this.pnlMain.Controls.Add(this.lblMeasurements);
            this.pnlMain.Controls.Add(this.fieldBPD);
            this.pnlMain.Controls.Add(this.fieldHC);
            this.pnlMain.Controls.Add(this.fieldAC);
            this.pnlMain.Controls.Add(this.fieldFL);
            this.pnlMain.Controls.Add(this.lblSummary);
            this.pnlMain.Controls.Add(this.rtbSummary);
            
            // Add Panel to UserControl
            this.Controls.Add(this.pnlMain);
            this.Name = "FinalImpressionControl";
            this.Size = new Size(650, 550);
            this.TabStop = false; // Don't focus the container, focus the internal controls
            
            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
