namespace OpenRA.TilesetBuilder
{
	partial class FormNew
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
			this.numSize = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.txtPal = new System.Windows.Forms.TextBox();
			this.btnPalBrowse = new System.Windows.Forms.Button();
			this.chkUsePalFromImage = new System.Windows.Forms.CheckBox();
			this.label3 = new System.Windows.Forms.Label();
			this.imgImage = new System.Windows.Forms.PictureBox();
			this.btnImgBrowse = new System.Windows.Forms.Button();
			this.txtImage = new System.Windows.Forms.TextBox();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.numSize)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.imgImage)).BeginInit();
			this.SuspendLayout();
			// 
			// numSize
			// 
			this.numSize.Location = new System.Drawing.Point(66, 6);
			this.numSize.Maximum = new decimal(new int[] {
			264,
			0,
			0,
			0});
			this.numSize.Minimum = new decimal(new int[] {
			24,
			0,
			0,
			0});
			this.numSize.Name = "numSize";
			this.numSize.Size = new System.Drawing.Size(49, 20);
			this.numSize.TabIndex = 0;
			this.numSize.Value = new decimal(new int[] {
			24,
			0,
			0,
			0});
			this.numSize.ValueChanged += new System.EventHandler(this.NumSizeValueChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Tile size:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 34);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(43, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Palette:";
			// 
			// txtPal
			// 
			this.txtPal.Location = new System.Drawing.Point(15, 52);
			this.txtPal.Name = "txtPal";
			this.txtPal.ReadOnly = true;
			this.txtPal.Size = new System.Drawing.Size(267, 20);
			this.txtPal.TabIndex = 3;
			// 
			// btnPalBrowse
			// 
			this.btnPalBrowse.Enabled = false;
			this.btnPalBrowse.Location = new System.Drawing.Point(288, 50);
			this.btnPalBrowse.Name = "btnPalBrowse";
			this.btnPalBrowse.Size = new System.Drawing.Size(26, 23);
			this.btnPalBrowse.TabIndex = 4;
			this.btnPalBrowse.Text = "...";
			this.btnPalBrowse.UseVisualStyleBackColor = true;
			this.btnPalBrowse.Click += new System.EventHandler(this.PaletteBrowseClick);
			// 
			// chkUsePalFromImage
			// 
			this.chkUsePalFromImage.AutoSize = true;
			this.chkUsePalFromImage.Checked = true;
			this.chkUsePalFromImage.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkUsePalFromImage.Location = new System.Drawing.Point(66, 34);
			this.chkUsePalFromImage.Name = "chkUsePalFromImage";
			this.chkUsePalFromImage.Size = new System.Drawing.Size(134, 17);
			this.chkUsePalFromImage.TabIndex = 5;
			this.chkUsePalFromImage.Text = "Use palette from image";
			this.chkUsePalFromImage.UseVisualStyleBackColor = true;
			this.chkUsePalFromImage.CheckedChanged += new System.EventHandler(this.UsePaletteFromImageCheckedChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 75);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(39, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Image:";
			// 
			// imgImage
			// 
			this.imgImage.BackColor = System.Drawing.Color.Black;
			this.imgImage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.imgImage.Location = new System.Drawing.Point(15, 120);
			this.imgImage.Name = "imgImage";
			this.imgImage.Size = new System.Drawing.Size(299, 219);
			this.imgImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.imgImage.TabIndex = 7;
			this.imgImage.TabStop = false;
			// 
			// btnImgBrowse
			// 
			this.btnImgBrowse.Location = new System.Drawing.Point(288, 91);
			this.btnImgBrowse.Name = "btnImgBrowse";
			this.btnImgBrowse.Size = new System.Drawing.Size(26, 23);
			this.btnImgBrowse.TabIndex = 9;
			this.btnImgBrowse.Text = "...";
			this.btnImgBrowse.UseVisualStyleBackColor = true;
			this.btnImgBrowse.Click += new System.EventHandler(this.ImageBrowseClick);
			// 
			// txtImage
			// 
			this.txtImage.Location = new System.Drawing.Point(15, 91);
			this.txtImage.Name = "txtImage";
			this.txtImage.ReadOnly = true;
			this.txtImage.Size = new System.Drawing.Size(267, 20);
			this.txtImage.TabIndex = 8;
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(239, 345);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 10;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.CancelButtonClick);
			// 
			// btnOk
			// 
			this.btnOk.Location = new System.Drawing.Point(158, 345);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 11;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.OkButtonClick);
			// 
			// frmNew
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(329, 378);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnImgBrowse);
			this.Controls.Add(this.txtImage);
			this.Controls.Add(this.imgImage);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.chkUsePalFromImage);
			this.Controls.Add(this.btnPalBrowse);
			this.Controls.Add(this.txtPal);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.numSize);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmNew";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "New Tileset";
			((System.ComponentModel.ISupportInitialize)(this.numSize)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.imgImage)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.NumericUpDown numSize;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtPal;
		private System.Windows.Forms.Button btnPalBrowse;
		private System.Windows.Forms.CheckBox chkUsePalFromImage;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.PictureBox imgImage;
		private System.Windows.Forms.Button btnImgBrowse;
		private System.Windows.Forms.TextBox txtImage;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
	}
}