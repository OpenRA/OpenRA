namespace OpenRA.ObserverUIEditor {
	partial class ColumnEdit {
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
			this.cbValue = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.edWidth = new System.Windows.Forms.NumericUpDown();
			this.button1 = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.edCustomText = new System.Windows.Forms.TextBox();
			this.cbAlign = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.chkNoBackground = new System.Windows.Forms.CheckBox();
			this.label5 = new System.Windows.Forms.Label();
			this.edTitle = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.edWidth)).BeginInit();
			this.SuspendLayout();
			// 
			// cbValue
			// 
			this.cbValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbValue.FormattingEnabled = true;
			this.cbValue.Items.AddRange(new object[] {
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
			this.cbValue.Location = new System.Drawing.Point(139, 55);
			this.cbValue.Name = "cbValue";
			this.cbValue.Size = new System.Drawing.Size(187, 21);
			this.cbValue.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 58);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(34, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Value";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(13, 32);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(35, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Width";
			// 
			// edWidth
			// 
			this.edWidth.Location = new System.Drawing.Point(139, 30);
			this.edWidth.Maximum = new decimal(new int[] {
            1920,
            0,
            0,
            0});
			this.edWidth.Name = "edWidth";
			this.edWidth.Size = new System.Drawing.Size(120, 20);
			this.edWidth.TabIndex = 3;
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Location = new System.Drawing.Point(310, 163);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 4;
			this.button1.Text = "Save";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(28, 85);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(62, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "Custom text";
			// 
			// edCustomText
			// 
			this.edCustomText.Location = new System.Drawing.Point(139, 82);
			this.edCustomText.Name = "edCustomText";
			this.edCustomText.Size = new System.Drawing.Size(187, 20);
			this.edCustomText.TabIndex = 6;
			// 
			// cbAlign
			// 
			this.cbAlign.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbAlign.FormattingEnabled = true;
			this.cbAlign.Items.AddRange(new object[] {
            "Left",
            "Center",
            "Right"});
			this.cbAlign.Location = new System.Drawing.Point(139, 108);
			this.cbAlign.Name = "cbAlign";
			this.cbAlign.Size = new System.Drawing.Size(121, 21);
			this.cbAlign.TabIndex = 7;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(13, 111);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(30, 13);
			this.label4.TabIndex = 8;
			this.label4.Text = "Align";
			// 
			// chkNoBackground
			// 
			this.chkNoBackground.AutoSize = true;
			this.chkNoBackground.Location = new System.Drawing.Point(139, 135);
			this.chkNoBackground.Name = "chkNoBackground";
			this.chkNoBackground.Size = new System.Drawing.Size(100, 17);
			this.chkNoBackground.TabIndex = 9;
			this.chkNoBackground.Text = "No background";
			this.chkNoBackground.UseVisualStyleBackColor = true;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(13, 9);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(27, 13);
			this.label5.TabIndex = 10;
			this.label5.Text = "Title";
			// 
			// edTitle
			// 
			this.edTitle.Location = new System.Drawing.Point(139, 4);
			this.edTitle.Name = "edTitle";
			this.edTitle.Size = new System.Drawing.Size(187, 20);
			this.edTitle.TabIndex = 11;
			// 
			// ColumnEdit
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(397, 198);
			this.Controls.Add(this.edTitle);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.chkNoBackground);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.cbAlign);
			this.Controls.Add(this.edCustomText);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.edWidth);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.cbValue);
			this.Name = "ColumnEdit";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "ColumnEdit";
			((System.ComponentModel.ISupportInitialize)(this.edWidth)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox cbValue;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown edWidth;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox edCustomText;
		private System.Windows.Forms.ComboBox cbAlign;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.CheckBox chkNoBackground;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox edTitle;
	}
}