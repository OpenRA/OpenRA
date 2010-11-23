namespace OpenRA.Editor
{
	partial class NewMapDialog
	{
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
			this.button2 = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.cordonBottom = new System.Windows.Forms.NumericUpDown();
			this.cordonTop = new System.Windows.Forms.NumericUpDown();
			this.cordonRight = new System.Windows.Forms.NumericUpDown();
			this.cordonLeft = new System.Windows.Forms.NumericUpDown();
			this.height = new System.Windows.Forms.NumericUpDown();
			this.width = new System.Windows.Forms.NumericUpDown();
			this.label4 = new System.Windows.Forms.Label();
			this.theater = new System.Windows.Forms.ComboBox();
			((System.ComponentModel.ISupportInitialize)(this.cordonBottom)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonTop)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonRight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonLeft)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.height)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.width)).BeginInit();
			this.SuspendLayout();
			// 
			// button2
			// 
			this.button2.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button2.Location = new System.Drawing.Point(229, 160);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 7;
			this.button2.Text = "OK";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// button1
			// 
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(310, 160);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 8;
			this.button1.Text = "Cancel";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(31, 77);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(107, 13);
			this.label3.TabIndex = 9;
			this.label3.Text = "Cordon Right/Bottom";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(31, 51);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(86, 13);
			this.label2.TabIndex = 11;
			this.label2.Text = "Cordon Left/Top";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(31, 25);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(27, 13);
			this.label1.TabIndex = 10;
			this.label1.Text = "Size";
			// 
			// cordonBottom
			// 
			this.cordonBottom.Location = new System.Drawing.Point(280, 75);
			this.cordonBottom.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.cordonBottom.Name = "cordonBottom";
			this.cordonBottom.Size = new System.Drawing.Size(105, 20);
			this.cordonBottom.TabIndex = 5;
			this.cordonBottom.Value = new decimal(new int[] {
            112,
            0,
            0,
            0});
			this.cordonBottom.Enter += new System.EventHandler(this.SelectText);
			// 
			// cordonTop
			// 
			this.cordonTop.Location = new System.Drawing.Point(280, 49);
			this.cordonTop.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.cordonTop.Name = "cordonTop";
			this.cordonTop.Size = new System.Drawing.Size(105, 20);
			this.cordonTop.TabIndex = 3;
			this.cordonTop.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
			this.cordonTop.Enter += new System.EventHandler(this.SelectText);
			// 
			// cordonRight
			// 
			this.cordonRight.Location = new System.Drawing.Point(169, 75);
			this.cordonRight.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.cordonRight.Name = "cordonRight";
			this.cordonRight.Size = new System.Drawing.Size(105, 20);
			this.cordonRight.TabIndex = 4;
			this.cordonRight.Value = new decimal(new int[] {
            112,
            0,
            0,
            0});
			this.cordonRight.Enter += new System.EventHandler(this.SelectText);
			// 
			// cordonLeft
			// 
			this.cordonLeft.Location = new System.Drawing.Point(169, 49);
			this.cordonLeft.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.cordonLeft.Name = "cordonLeft";
			this.cordonLeft.Size = new System.Drawing.Size(105, 20);
			this.cordonLeft.TabIndex = 2;
			this.cordonLeft.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
			this.cordonLeft.Enter += new System.EventHandler(this.SelectText);
			// 
			// height
			// 
			this.height.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.height.Location = new System.Drawing.Point(280, 23);
			this.height.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.height.Name = "height";
			this.height.Size = new System.Drawing.Size(105, 20);
			this.height.TabIndex = 1;
			this.height.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
			this.height.Enter += new System.EventHandler(this.SelectText);
			// 
			// width
			// 
			this.width.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.width.Location = new System.Drawing.Point(169, 23);
			this.width.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.width.Name = "width";
			this.width.Size = new System.Drawing.Size(105, 20);
			this.width.TabIndex = 0;
			this.width.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
			this.width.Enter += new System.EventHandler(this.SelectText);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(31, 124);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(44, 13);
			this.label4.TabIndex = 14;
			this.label4.Text = "Tileset";
			// 
			// theater
			// 
			this.theater.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.theater.FormattingEnabled = true;
			this.theater.Location = new System.Drawing.Point(169, 121);
			this.theater.Name = "theater";
			this.theater.Size = new System.Drawing.Size(216, 21);
			this.theater.TabIndex = 6;
			// 
			// NewMapDialog
			// 
			this.AcceptButton = this.button2;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button1;
			this.ClientSize = new System.Drawing.Size(418, 210);
			this.Controls.Add(this.theater);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.cordonBottom);
			this.Controls.Add(this.cordonTop);
			this.Controls.Add(this.cordonRight);
			this.Controls.Add(this.cordonLeft);
			this.Controls.Add(this.height);
			this.Controls.Add(this.width);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "NewMapDialog";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "New Map";
			((System.ComponentModel.ISupportInitialize)(this.cordonBottom)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonTop)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonRight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonLeft)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.height)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.width)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.NumericUpDown cordonBottom;
		public System.Windows.Forms.NumericUpDown cordonTop;
		public System.Windows.Forms.NumericUpDown cordonRight;
		public System.Windows.Forms.NumericUpDown cordonLeft;
		public System.Windows.Forms.NumericUpDown height;
		public System.Windows.Forms.NumericUpDown width;
		private System.Windows.Forms.Label label4;
		public System.Windows.Forms.ComboBox theater;
	}
}