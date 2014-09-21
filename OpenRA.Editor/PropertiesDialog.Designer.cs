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
	partial class PropertiesDialog
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
			this.label1 = new System.Windows.Forms.Label();
			this.TitleBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.DescBox = new System.Windows.Forms.TextBox();
			this.SelectableCheckBox = new System.Windows.Forms.CheckBox();
			this.label3 = new System.Windows.Forms.Label();
			this.AuthorBox = new System.Windows.Forms.TextBox();
			this.ShellmapCheckBox = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			//
			// button2
			//
			this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button2.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button2.Location = new System.Drawing.Point(196, 193);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 14;
			this.button2.Text = "OK";
			this.button2.UseVisualStyleBackColor = true;
			//
			// button1
			//
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(277, 193);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 15;
			this.button1.Text = "Cancel";
			this.button1.UseVisualStyleBackColor = true;
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 50);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(27, 13);
			this.label1.TabIndex = 16;
			this.label1.Text = "Title";
			//
			// title
			//
			this.TitleBox.Location = new System.Drawing.Point(66, 47);
			this.TitleBox.Name = "title";
			this.TitleBox.Size = new System.Drawing.Size(286, 20);
			this.TitleBox.TabIndex = 17;
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 76);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(32, 13);
			this.label2.TabIndex = 16;
			this.label2.Text = "Desc";
			//
			// desc
			//
			this.DescBox.Location = new System.Drawing.Point(66, 73);
			this.DescBox.Name = "desc";
			this.DescBox.Size = new System.Drawing.Size(286, 20);
			this.DescBox.TabIndex = 17;
			//
			// selectable
			//
			this.SelectableCheckBox.AutoSize = true;
			this.SelectableCheckBox.Location = new System.Drawing.Point(118, 138);
			this.SelectableCheckBox.Name = "selectable";
			this.SelectableCheckBox.Size = new System.Drawing.Size(130, 17);
			this.SelectableCheckBox.TabIndex = 18;
			this.SelectableCheckBox.Text = "Show in Map Chooser";
			this.SelectableCheckBox.UseVisualStyleBackColor = true;
			//
			// label3
			//
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 102);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(38, 13);
			this.label3.TabIndex = 16;
			this.label3.Text = "Author";
			//
			// author
			//
			this.AuthorBox.Location = new System.Drawing.Point(66, 99);
			this.AuthorBox.Name = "author";
			this.AuthorBox.Size = new System.Drawing.Size(286, 20);
			this.AuthorBox.TabIndex = 17;
			//
			// checkBox1
			//
			this.ShellmapCheckBox.AutoSize = true;
			this.ShellmapCheckBox.Location = new System.Drawing.Point(118, 161);
			this.ShellmapCheckBox.Name = "checkBox1";
			this.ShellmapCheckBox.Size = new System.Drawing.Size(105, 17);
			this.ShellmapCheckBox.TabIndex = 18;
			this.ShellmapCheckBox.Text = "Use as Shellmap";
			this.ShellmapCheckBox.UseVisualStyleBackColor = true;
			//
			// PropertiesDialog
			//
			this.AcceptButton = this.button2;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button1;
			this.ClientSize = new System.Drawing.Size(370, 228);
			this.Controls.Add(this.ShellmapCheckBox);
			this.Controls.Add(this.SelectableCheckBox);
			this.Controls.Add(this.AuthorBox);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.DescBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.TitleBox);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "PropertiesDialog";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Map Properties";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.TextBox TitleBox;
		private System.Windows.Forms.Label label2;
		public System.Windows.Forms.TextBox DescBox;
		public System.Windows.Forms.CheckBox SelectableCheckBox;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.TextBox AuthorBox;
		public System.Windows.Forms.CheckBox ShellmapCheckBox;
	}
}
