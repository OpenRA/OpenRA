namespace OpenRA.Editor
{
	partial class ResizeDialog
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
			this.width = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.cordonLeft = new System.Windows.Forms.NumericUpDown();
			this.cordonTop = new System.Windows.Forms.NumericUpDown();
			this.cordonRight = new System.Windows.Forms.NumericUpDown();
			this.cordonBottom = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.height = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.width)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonLeft)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonTop)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonRight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonBottom)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.height)).BeginInit();
			this.SuspendLayout();
			// 
			// width
			// 
			this.width.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.width.Location = new System.Drawing.Point(161, 18);
			this.width.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.width.Name = "width";
			this.width.Size = new System.Drawing.Size(105, 20);
			this.width.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(23, 20);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(27, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Size";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(23, 46);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(86, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Cordon Left/Top";
			// 
			// cordonLeft
			// 
			this.cordonLeft.Location = new System.Drawing.Point(161, 44);
			this.cordonLeft.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.cordonLeft.Name = "cordonLeft";
			this.cordonLeft.Size = new System.Drawing.Size(105, 20);
			this.cordonLeft.TabIndex = 0;
			// 
			// cordonTop
			// 
			this.cordonTop.Location = new System.Drawing.Point(272, 44);
			this.cordonTop.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.cordonTop.Name = "cordonTop";
			this.cordonTop.Size = new System.Drawing.Size(105, 20);
			this.cordonTop.TabIndex = 0;
			// 
			// cordonRight
			// 
			this.cordonRight.Location = new System.Drawing.Point(161, 70);
			this.cordonRight.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.cordonRight.Name = "cordonRight";
			this.cordonRight.Size = new System.Drawing.Size(105, 20);
			this.cordonRight.TabIndex = 0;
			// 
			// cordonBottom
			// 
			this.cordonBottom.Location = new System.Drawing.Point(272, 70);
			this.cordonBottom.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.cordonBottom.Name = "cordonBottom";
			this.cordonBottom.Size = new System.Drawing.Size(105, 20);
			this.cordonBottom.TabIndex = 0;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(23, 72);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(107, 13);
			this.label3.TabIndex = 1;
			this.label3.Text = "Cordon Right/Bottom";
			// 
			// button1
			// 
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(302, 111);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 2;
			this.button1.Text = "Cancel";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// button2
			// 
			this.button2.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button2.Location = new System.Drawing.Point(221, 111);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 2;
			this.button2.Text = "OK";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// height
			// 
			this.height.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.height.Location = new System.Drawing.Point(272, 18);
			this.height.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.height.Name = "height";
			this.height.Size = new System.Drawing.Size(105, 20);
			this.height.TabIndex = 0;
			// 
			// ResizeDialog
			// 
			this.AcceptButton = this.button2;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button1;
			this.ClientSize = new System.Drawing.Size(409, 146);
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
			this.Name = "ResizeDialog";
			this.Text = "Resize Map";
			((System.ComponentModel.ISupportInitialize)(this.width)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonLeft)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonTop)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonRight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.cordonBottom)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.height)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		public System.Windows.Forms.NumericUpDown width;
		public System.Windows.Forms.NumericUpDown cordonLeft;
		public System.Windows.Forms.NumericUpDown cordonTop;
		public System.Windows.Forms.NumericUpDown cordonRight;
		public System.Windows.Forms.NumericUpDown cordonBottom;
		public System.Windows.Forms.NumericUpDown height;
	}
}