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
			this.button2 = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.CordonBottom = new System.Windows.Forms.NumericUpDown();
			this.CordonTop = new System.Windows.Forms.NumericUpDown();
			this.CordonRight = new System.Windows.Forms.NumericUpDown();
			this.CordonLeft = new System.Windows.Forms.NumericUpDown();
			this.MapHeight = new System.Windows.Forms.NumericUpDown();
			this.MapWidth = new System.Windows.Forms.NumericUpDown();
			this.label4 = new System.Windows.Forms.Label();
			this.TheaterBox = new System.Windows.Forms.ComboBox();
			((System.ComponentModel.ISupportInitialize)(this.CordonBottom)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonTop)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonRight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonLeft)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MapHeight)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MapWidth)).BeginInit();
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
			this.CordonBottom.Location = new System.Drawing.Point(280, 75);
			this.CordonBottom.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.CordonBottom.Name = "cordonBottom";
			this.CordonBottom.Size = new System.Drawing.Size(105, 20);
			this.CordonBottom.TabIndex = 5;
			this.CordonBottom.Value = new decimal(new int[] {
			112,
			0,
			0,
			0});
			this.CordonBottom.Enter += new System.EventHandler(this.SelectText);
			//
			// cordonTop
			//
			this.CordonTop.Location = new System.Drawing.Point(280, 49);
			this.CordonTop.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.CordonTop.Name = "cordonTop";
			this.CordonTop.Size = new System.Drawing.Size(105, 20);
			this.CordonTop.TabIndex = 3;
			this.CordonTop.Value = new decimal(new int[] {
			16,
			0,
			0,
			0});
			this.CordonTop.Enter += new System.EventHandler(this.SelectText);
			//
			// cordonRight
			//
			this.CordonRight.Location = new System.Drawing.Point(169, 75);
			this.CordonRight.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.CordonRight.Name = "cordonRight";
			this.CordonRight.Size = new System.Drawing.Size(105, 20);
			this.CordonRight.TabIndex = 4;
			this.CordonRight.Value = new decimal(new int[] {
			112,
			0,
			0,
			0});
			this.CordonRight.Enter += new System.EventHandler(this.SelectText);
			//
			// cordonLeft
			//
			this.CordonLeft.Location = new System.Drawing.Point(169, 49);
			this.CordonLeft.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.CordonLeft.Name = "cordonLeft";
			this.CordonLeft.Size = new System.Drawing.Size(105, 20);
			this.CordonLeft.TabIndex = 2;
			this.CordonLeft.Value = new decimal(new int[] {
			16,
			0,
			0,
			0});
			this.CordonLeft.Enter += new System.EventHandler(this.SelectText);
			//
			// height
			//
			this.MapHeight.Increment = new decimal(new int[] {
			8,
			0,
			0,
			0});
			this.MapHeight.Location = new System.Drawing.Point(280, 23);
			this.MapHeight.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.MapHeight.Name = "height";
			this.MapHeight.Size = new System.Drawing.Size(105, 20);
			this.MapHeight.TabIndex = 1;
			this.MapHeight.Value = new decimal(new int[] {
			128,
			0,
			0,
			0});
			this.MapHeight.Enter += new System.EventHandler(this.SelectText);
			//
			// width
			//
			this.MapWidth.Increment = new decimal(new int[] {
			8,
			0,
			0,
			0});
			this.MapWidth.Location = new System.Drawing.Point(169, 23);
			this.MapWidth.Maximum = new decimal(new int[] {
			2048,
			0,
			0,
			0});
			this.MapWidth.Name = "width";
			this.MapWidth.Size = new System.Drawing.Size(105, 20);
			this.MapWidth.TabIndex = 0;
			this.MapWidth.Value = new decimal(new int[] {
			128,
			0,
			0,
			0});
			this.MapWidth.Enter += new System.EventHandler(this.SelectText);
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
			this.TheaterBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.TheaterBox.FormattingEnabled = true;
			this.TheaterBox.Location = new System.Drawing.Point(169, 121);
			this.TheaterBox.Name = "theater";
			this.TheaterBox.Size = new System.Drawing.Size(216, 21);
			this.TheaterBox.TabIndex = 6;
			//
			// NewMapDialog
			//
			this.AcceptButton = this.button2;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button1;
			this.ClientSize = new System.Drawing.Size(418, 210);
			this.Controls.Add(this.TheaterBox);
			this.Controls.Add(this.label4);
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
			this.Name = "NewMapDialog";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "New Map";
			((System.ComponentModel.ISupportInitialize)(this.CordonBottom)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonTop)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonRight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.CordonLeft)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MapHeight)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MapWidth)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.NumericUpDown CordonBottom;
		public System.Windows.Forms.NumericUpDown CordonTop;
		public System.Windows.Forms.NumericUpDown CordonRight;
		public System.Windows.Forms.NumericUpDown CordonLeft;
		public System.Windows.Forms.NumericUpDown MapHeight;
		public System.Windows.Forms.NumericUpDown MapWidth;
		private System.Windows.Forms.Label label4;
		public System.Windows.Forms.ComboBox TheaterBox;
	}
}