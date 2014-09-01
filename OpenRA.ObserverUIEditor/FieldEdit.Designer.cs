namespace OpenRA.ObserverUIEditor {
	partial class frmFieldEdit {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.button1 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.cbValue = new System.Windows.Forms.ComboBox();
			this.grpPositioning = new System.Windows.Forms.GroupBox();
			this.edHeight = new System.Windows.Forms.NumericUpDown();
			this.label8 = new System.Windows.Forms.Label();
			this.edY = new System.Windows.Forms.NumericUpDown();
			this.label7 = new System.Windows.Forms.Label();
			this.edX = new System.Windows.Forms.NumericUpDown();
			this.label6 = new System.Windows.Forms.Label();
			this.edWidth = new System.Windows.Forms.NumericUpDown();
			this.label2 = new System.Windows.Forms.Label();
			this.grpLabelSettings = new System.Windows.Forms.GroupBox();
			this.label4 = new System.Windows.Forms.Label();
			this.cbAlign = new System.Windows.Forms.ComboBox();
			this.edCustomText = new System.Windows.Forms.TextBox();
			this.lblCustomText = new System.Windows.Forms.Label();
			this.grpImageSettings = new System.Windows.Forms.GroupBox();
			this.label9 = new System.Windows.Forms.Label();
			this.cmbImage = new System.Windows.Forms.ComboBox();
			this.grpPositioning.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.edHeight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.edY)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.edX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.edWidth)).BeginInit();
			this.grpLabelSettings.SuspendLayout();
			this.grpImageSettings.SuspendLayout();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Location = new System.Drawing.Point(197, 234);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 16;
			this.button1.Text = "Save";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(31, 13);
			this.label1.TabIndex = 13;
			this.label1.Text = "Type";
			// 
			// cbValue
			// 
			this.cbValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbValue.FormattingEnabled = true;
			this.cbValue.Items.AddRange(new object[] {
            "Image@FLAG",
            "Image",
            "Label@PLAYER",
            "Label@CONTROL",
            "Label@KILLS_COST",
            "Label@DEATHS_COST",
            "Label@UNITS_KILLED",
            "Label@UNITS_DEAD",
            "Label@BUILDINGS_KILLED",
            "Label@BUILDINGS_DEAD",
            "Label@ACTIONS_MIN",
            "Label@ACTIONS_MIN_TXT",
            "Label@CASH",
            "Label@INCOME",
            "Label@SPENT",
            "Label@EARNED_MIN",
            "Label@ENERGY",
            "Label@HARVESTERS",
            "Label@ARMYVALUE",
            "Label@STATICDEFVALUE",
            "Label@BUILDINGVALUE",
            "Label"});
			this.cbValue.Location = new System.Drawing.Point(72, 6);
			this.cbValue.Name = "cbValue";
			this.cbValue.Size = new System.Drawing.Size(187, 21);
			this.cbValue.TabIndex = 12;
			this.cbValue.SelectedIndexChanged += new System.EventHandler(this.cbValue_SelectedIndexChanged);
			// 
			// grpPositioning
			// 
			this.grpPositioning.Controls.Add(this.edHeight);
			this.grpPositioning.Controls.Add(this.label8);
			this.grpPositioning.Controls.Add(this.edY);
			this.grpPositioning.Controls.Add(this.label7);
			this.grpPositioning.Controls.Add(this.edX);
			this.grpPositioning.Controls.Add(this.label6);
			this.grpPositioning.Controls.Add(this.edWidth);
			this.grpPositioning.Controls.Add(this.label2);
			this.grpPositioning.Location = new System.Drawing.Point(12, 33);
			this.grpPositioning.Name = "grpPositioning";
			this.grpPositioning.Size = new System.Drawing.Size(258, 89);
			this.grpPositioning.TabIndex = 28;
			this.grpPositioning.TabStop = false;
			this.grpPositioning.Text = "Positioning";
			// 
			// edHeight
			// 
			this.edHeight.ForeColor = System.Drawing.SystemColors.WindowFrame;
			this.edHeight.Location = new System.Drawing.Point(179, 54);
			this.edHeight.Maximum = new decimal(new int[] {
            1920,
            0,
            0,
            0});
			this.edHeight.Name = "edHeight";
			this.edHeight.Size = new System.Drawing.Size(62, 20);
			this.edHeight.TabIndex = 35;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label8.Location = new System.Drawing.Point(135, 56);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(38, 13);
			this.label8.TabIndex = 34;
			this.label8.Text = "Height";
			// 
			// edY
			// 
			this.edY.ForeColor = System.Drawing.SystemColors.WindowFrame;
			this.edY.Location = new System.Drawing.Point(179, 28);
			this.edY.Maximum = new decimal(new int[] {
            1920,
            0,
            0,
            0});
			this.edY.Name = "edY";
			this.edY.Size = new System.Drawing.Size(62, 20);
			this.edY.TabIndex = 33;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.label7.Location = new System.Drawing.Point(159, 30);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(14, 13);
			this.label7.TabIndex = 32;
			this.label7.Text = "Y";
			// 
			// edX
			// 
			this.edX.Location = new System.Drawing.Point(60, 28);
			this.edX.Maximum = new decimal(new int[] {
            1920,
            0,
            0,
            0});
			this.edX.Name = "edX";
			this.edX.Size = new System.Drawing.Size(62, 20);
			this.edX.TabIndex = 31;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(40, 30);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(14, 13);
			this.label6.TabIndex = 30;
			this.label6.Text = "X";
			// 
			// edWidth
			// 
			this.edWidth.Location = new System.Drawing.Point(60, 54);
			this.edWidth.Maximum = new decimal(new int[] {
            1920,
            0,
            0,
            0});
			this.edWidth.Name = "edWidth";
			this.edWidth.Size = new System.Drawing.Size(62, 20);
			this.edWidth.TabIndex = 29;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(19, 56);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(35, 13);
			this.label2.TabIndex = 28;
			this.label2.Text = "Width";
			// 
			// grpLabelSettings
			// 
			this.grpLabelSettings.Controls.Add(this.label4);
			this.grpLabelSettings.Controls.Add(this.cbAlign);
			this.grpLabelSettings.Controls.Add(this.edCustomText);
			this.grpLabelSettings.Controls.Add(this.lblCustomText);
			this.grpLabelSettings.Location = new System.Drawing.Point(12, 132);
			this.grpLabelSettings.Name = "grpLabelSettings";
			this.grpLabelSettings.Size = new System.Drawing.Size(258, 90);
			this.grpLabelSettings.TabIndex = 31;
			this.grpLabelSettings.TabStop = false;
			this.grpLabelSettings.Text = "Label settings";
			this.grpLabelSettings.Visible = false;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(46, 58);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(30, 13);
			this.label4.TabIndex = 25;
			this.label4.Text = "Align";
			// 
			// cbAlign
			// 
			this.cbAlign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbAlign.FormattingEnabled = true;
			this.cbAlign.Items.AddRange(new object[] {
            "Left",
            "Center",
            "Right"});
			this.cbAlign.Location = new System.Drawing.Point(82, 55);
			this.cbAlign.Name = "cbAlign";
			this.cbAlign.Size = new System.Drawing.Size(148, 21);
			this.cbAlign.TabIndex = 24;
			this.cbAlign.SelectedIndexChanged += new System.EventHandler(this.cbAlign_SelectedIndexChanged);
			// 
			// edCustomText
			// 
			this.edCustomText.Location = new System.Drawing.Point(82, 29);
			this.edCustomText.Name = "edCustomText";
			this.edCustomText.Size = new System.Drawing.Size(148, 20);
			this.edCustomText.TabIndex = 23;
			// 
			// lblCustomText
			// 
			this.lblCustomText.AutoSize = true;
			this.lblCustomText.Location = new System.Drawing.Point(14, 32);
			this.lblCustomText.Name = "lblCustomText";
			this.lblCustomText.Size = new System.Drawing.Size(62, 13);
			this.lblCustomText.TabIndex = 22;
			this.lblCustomText.Text = "Custom text";
			// 
			// grpImageSettings
			// 
			this.grpImageSettings.Controls.Add(this.label9);
			this.grpImageSettings.Controls.Add(this.cmbImage);
			this.grpImageSettings.Location = new System.Drawing.Point(12, 132);
			this.grpImageSettings.Name = "grpImageSettings";
			this.grpImageSettings.Size = new System.Drawing.Size(258, 51);
			this.grpImageSettings.TabIndex = 32;
			this.grpImageSettings.TabStop = false;
			this.grpImageSettings.Text = "Image settings";
			this.grpImageSettings.Visible = false;
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(14, 22);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(36, 13);
			this.label9.TabIndex = 32;
			this.label9.Text = "Image";
			// 
			// cmbImage
			// 
			this.cmbImage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbImage.FormattingEnabled = true;
			this.cmbImage.Items.AddRange(new object[] {
            "Image@FLAG",
            "Label@PLAYER",
            "Label@CONTROL",
            "Label@KILLS_COST",
            "Label@DEATHS_COST",
            "Label@UNITS_KILLED",
            "Label@UNITS_DEAD",
            "Label@BUILDINGS_KILLED",
            "Label@BUILDINGS_DEAD",
            "Label@ACTIONS_MIN",
            "Label@ACTIONS_MIN_TXT",
            "Label@CASH",
            "Label@INCOME",
            "Label@SPENT",
            "Label@EARNED_MIN",
            "Label@ENERGY",
            "Label@HARVESTERS",
            "Label@ARMYVALUE",
            "Label@STATICDEFVALUE",
            "Label@BUILDINGVALUE",
            "Label"});
			this.cmbImage.Location = new System.Drawing.Point(60, 19);
			this.cmbImage.Name = "cmbImage";
			this.cmbImage.Size = new System.Drawing.Size(187, 21);
			this.cmbImage.TabIndex = 31;
			// 
			// frmFieldEdit
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 269);
			this.Controls.Add(this.grpImageSettings);
			this.Controls.Add(this.grpLabelSettings);
			this.Controls.Add(this.grpPositioning);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.cbValue);
			this.Name = "frmFieldEdit";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Field Edit";
			this.grpPositioning.ResumeLayout(false);
			this.grpPositioning.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.edHeight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.edY)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.edX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.edWidth)).EndInit();
			this.grpLabelSettings.ResumeLayout(false);
			this.grpLabelSettings.PerformLayout();
			this.grpImageSettings.ResumeLayout(false);
			this.grpImageSettings.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox cbValue;
		private System.Windows.Forms.GroupBox grpPositioning;
		private System.Windows.Forms.NumericUpDown edY;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.NumericUpDown edX;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.NumericUpDown edWidth;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown edHeight;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.GroupBox grpLabelSettings;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox cbAlign;
		private System.Windows.Forms.TextBox edCustomText;
		private System.Windows.Forms.Label lblCustomText;
		private System.Windows.Forms.GroupBox grpImageSettings;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.ComboBox cmbImage;
	}
}