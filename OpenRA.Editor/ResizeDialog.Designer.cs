#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
				components.Dispose();

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.MapWidth = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.CordonLeft = new System.Windows.Forms.NumericUpDown();
			this.CordonTop = new System.Windows.Forms.NumericUpDown();
			this.CordonRight = new System.Windows.Forms.NumericUpDown();
			this.CordonBottom = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.MapHeight = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.MapWidth)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonLeft)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonTop)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonRight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonBottom)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MapHeight)).BeginInit();
			this.SuspendLayout();
			//
			// width
			//
			this.MapWidth.Increment = new decimal(new int[] {
			8,
			0,
			0,
			0});
			this.MapWidth.Location = new System.Drawing.Point(161, 18);
			this.MapWidth.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.MapWidth.Name = "width";
			this.MapWidth.Size = new System.Drawing.Size(105, 20);
			this.MapWidth.TabIndex = 0;
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
			this.CordonLeft.Location = new System.Drawing.Point(161, 44);
			this.CordonLeft.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.CordonLeft.Name = "cordonLeft";
			this.CordonLeft.Size = new System.Drawing.Size(105, 20);
			this.CordonLeft.TabIndex = 0;
			//
			// cordonTop
			//
			this.CordonTop.Location = new System.Drawing.Point(272, 44);
			this.CordonTop.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.CordonTop.Name = "cordonTop";
			this.CordonTop.Size = new System.Drawing.Size(105, 20);
			this.CordonTop.TabIndex = 0;
			//
			// cordonRight
			//
			this.CordonRight.Location = new System.Drawing.Point(161, 70);
			this.CordonRight.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.CordonRight.Name = "cordonRight";
			this.CordonRight.Size = new System.Drawing.Size(105, 20);
			this.CordonRight.TabIndex = 0;
			//
			// cordonBottom
			//
			this.CordonBottom.Location = new System.Drawing.Point(272, 70);
			this.CordonBottom.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.CordonBottom.Name = "cordonBottom";
			this.CordonBottom.Size = new System.Drawing.Size(105, 20);
			this.CordonBottom.TabIndex = 0;
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
			this.MapHeight.Increment = new decimal(new int[] {
			8,
			0,
			0,
			0});
			this.MapHeight.Location = new System.Drawing.Point(272, 18);
			this.MapHeight.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.MapHeight.Name = "height";
			this.MapHeight.Size = new System.Drawing.Size(105, 20);
			this.MapHeight.TabIndex = 0;
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
			this.Controls.Add(this.CordonBottom);
			this.Controls.Add(this.CordonTop);
			this.Controls.Add(this.CordonRight);
			this.Controls.Add(this.CordonLeft);
			this.Controls.Add(this.MapHeight);
			this.Controls.Add(this.MapWidth);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "ResizeDialog";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Resize Map";
			((System.ComponentModel.ISupportInitialize)(this.MapWidth)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonLeft)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonTop)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonRight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonBottom)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MapHeight)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		public System.Windows.Forms.NumericUpDown MapWidth;
		public System.Windows.Forms.NumericUpDown CordonLeft;
		public System.Windows.Forms.NumericUpDown CordonTop;
		public System.Windows.Forms.NumericUpDown CordonRight;
		public System.Windows.Forms.NumericUpDown CordonBottom;
		public System.Windows.Forms.NumericUpDown MapHeight;
	}
}