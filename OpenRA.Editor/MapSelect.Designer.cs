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
	partial class MapSelect
	{
		// TODO:
		private System.ComponentModel.IContainer components = null;

		// TODO:
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}

			base.Dispose(disposing);
		}

		// TODO:
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapSelect));
			this.MapList = new System.Windows.Forms.ListView();
			this.ColumnMapName = new System.Windows.Forms.ColumnHeader("(none)");
			this.MapIconsList = new System.Windows.Forms.ImageList(this.components);
			this.ButtonCancel = new System.Windows.Forms.Button();
			this.ButtonOkay = new System.Windows.Forms.Button();
			this.NewLabel = new System.Windows.Forms.Label();
			this.NewText = new System.Windows.Forms.TextBox();
			this.MiniMapBox = new System.Windows.Forms.PictureBox();
			this.BottomPanel = new System.Windows.Forms.Panel();
			this.PathOutText = new System.Windows.Forms.TextBox();
			this.PathOutLabel = new System.Windows.Forms.Label();
			this.PathLabel = new System.Windows.Forms.Label();
			this.SplitContainer1 = new System.Windows.Forms.SplitContainer();
			this.MapListLabel = new System.Windows.Forms.Label();
			this.DescTxt = new System.Windows.Forms.TextBox();
			this.DescLabel = new System.Windows.Forms.Label();
			this.TheaterText = new System.Windows.Forms.TextBox();
			this.TheaterLabel = new System.Windows.Forms.Label();
			this.AuthorText = new System.Windows.Forms.TextBox();
			this.AuthorLabel = new System.Windows.Forms.Label();
			this.TitleText = new System.Windows.Forms.TextBox();
			this.MapNameLabel = new System.Windows.Forms.Label();
			this.MiniMapLabel = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)this.MiniMapBox).BeginInit();
			this.BottomPanel.SuspendLayout();
			this.SplitContainer1.Panel1.SuspendLayout();
			this.SplitContainer1.Panel2.SuspendLayout();
			this.SplitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
			this.SuspendLayout();

			this.MapList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.MapList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {	this.ColumnMapName });
			this.MapList.FullRowSelect = true;
			this.MapList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.MapList.LargeImageList = this.MapIconsList;
			this.MapList.Location = new System.Drawing.Point(15, 25);
			this.MapList.MultiSelect = false;
			this.MapList.Name = "MapList";
			this.MapList.Size = new System.Drawing.Size(273, 294);
			this.MapList.SmallImageList = this.MapIconsList;
			this.MapList.StateImageList = this.MapIconsList;
			this.MapList.TabIndex = 0;
			this.MapList.UseCompatibleStateImageBehavior = false;
			this.MapList.View = System.Windows.Forms.View.Details;
			this.MapList.SelectedIndexChanged += new System.EventHandler(this.MapList_SelectedIndexChanged);

			this.ColumnMapName.Text = "Map name";
			this.ColumnMapName.Width = 240;

			this.MapIconsList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
			this.MapIconsList.ImageSize = new System.Drawing.Size(24, 24);
			this.MapIconsList.TransparentColor = System.Drawing.Color.Transparent;

			this.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.ButtonCancel.Location = new System.Drawing.Point(407, 35);
			this.ButtonCancel.Name = "btnCancel";
			this.ButtonCancel.Size = new System.Drawing.Size(75, 23);
			this.ButtonCancel.TabIndex = 3;
			this.ButtonCancel.Text = "Cancel";
			this.ButtonCancel.UseVisualStyleBackColor = true;

			this.ButtonOkay.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.ButtonOkay.Location = new System.Drawing.Point(326, 35);
			this.ButtonOkay.Name = "btnOk";
			this.ButtonOkay.Size = new System.Drawing.Size(75, 23);
			this.ButtonOkay.TabIndex = 2;
			this.ButtonOkay.Text = "Open";
			this.ButtonOkay.UseVisualStyleBackColor = true;

			this.NewLabel.AutoSize = true;
			this.NewLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, (byte)204);
			this.NewLabel.Location = new System.Drawing.Point(12, 40);
			this.NewLabel.Name = "lblNew";
			this.NewLabel.Size = new System.Drawing.Size(69, 13);
			this.NewLabel.TabIndex = 3;
			this.NewLabel.Text = "Map name:";

			this.NewText.BackColor = System.Drawing.SystemColors.Window;
			this.NewText.Location = new System.Drawing.Point(88, 37);
			this.NewText.Name = "txtNew";
			this.NewText.ReadOnly = true;
			this.NewText.Size = new System.Drawing.Size(232, 20);
			this.NewText.TabIndex = 1;

			this.MiniMapBox.BackColor = System.Drawing.Color.Black;
			this.MiniMapBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.MiniMapBox.Location = new System.Drawing.Point(32, 25);
			this.MiniMapBox.Name = "pbMinimap";
			this.MiniMapBox.Size = new System.Drawing.Size(124, 124);
			this.MiniMapBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.MiniMapBox.TabIndex = 5;
			this.MiniMapBox.TabStop = false;

			this.BottomPanel.Controls.Add(this.pictureBox1);
			this.BottomPanel.Controls.Add(this.PathOutText);
			this.BottomPanel.Controls.Add(this.PathOutLabel);
			this.BottomPanel.Controls.Add(this.PathLabel);
			this.BottomPanel.Controls.Add(this.ButtonCancel);
			this.BottomPanel.Controls.Add(this.ButtonOkay);
			this.BottomPanel.Controls.Add(this.NewText);
			this.BottomPanel.Controls.Add(this.NewLabel);
			this.BottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.BottomPanel.Location = new System.Drawing.Point(0, 332);
			this.BottomPanel.MaximumSize = new System.Drawing.Size(0, 70);
			this.BottomPanel.Name = "pnlBottom";
			this.BottomPanel.Size = new System.Drawing.Size(494, 70);
			this.BottomPanel.TabIndex = 6;

			this.PathOutText.BackColor = System.Drawing.SystemColors.Window;
			this.PathOutText.Location = new System.Drawing.Point(55, 10);
			this.PathOutText.Name = "txtPathOut";
			this.PathOutText.ReadOnly = true;
			this.PathOutText.Size = new System.Drawing.Size(265, 20);
			this.PathOutText.TabIndex = 0;
			this.PathOutText.TextChanged += new System.EventHandler(this.PathOutTextChanged);

			this.PathOutLabel.AutoSize = true;
			this.PathOutLabel.Location = new System.Drawing.Point(55, 13);
			this.PathOutLabel.Name = "lblPathOut";
			this.PathOutLabel.Size = new System.Drawing.Size(0, 13);
			this.PathOutLabel.TabIndex = 6;

			this.PathLabel.AutoSize = true;
			this.PathLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, (byte)204);
			this.PathLabel.Location = new System.Drawing.Point(12, 13);
			this.PathLabel.Name = "lblPath";
			this.PathLabel.Size = new System.Drawing.Size(37, 13);
			this.PathLabel.TabIndex = 5;
			this.PathLabel.Text = "Path:";

			this.SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SplitContainer1.Location = new System.Drawing.Point(0, 0);
			this.SplitContainer1.Name = "splitContainer1";

			this.SplitContainer1.Panel1.Controls.Add(this.MapListLabel);
			this.SplitContainer1.Panel1.Controls.Add(this.MapList);

			this.SplitContainer1.Panel2.Controls.Add(this.DescTxt);
			this.SplitContainer1.Panel2.Controls.Add(this.DescLabel);
			this.SplitContainer1.Panel2.Controls.Add(this.TheaterText);
			this.SplitContainer1.Panel2.Controls.Add(this.TheaterLabel);
			this.SplitContainer1.Panel2.Controls.Add(this.AuthorText);
			this.SplitContainer1.Panel2.Controls.Add(this.AuthorLabel);
			this.SplitContainer1.Panel2.Controls.Add(this.TitleText);
			this.SplitContainer1.Panel2.Controls.Add(this.MapNameLabel);
			this.SplitContainer1.Panel2.Controls.Add(this.MiniMapLabel);
			this.SplitContainer1.Panel2.Controls.Add(this.MiniMapBox);
			this.SplitContainer1.Size = new System.Drawing.Size(494, 332);
			this.SplitContainer1.SplitterDistance = 300;
			this.SplitContainer1.TabIndex = 7;

			this.MapListLabel.AutoSize = true;
			this.MapListLabel.Location = new System.Drawing.Point(12, 9);
			this.MapListLabel.Name = "lblMapList";
			this.MapListLabel.Size = new System.Drawing.Size(81, 13);
			this.MapListLabel.TabIndex = 1;
			this.MapListLabel.Text = "Available maps:";

			this.DescTxt.BackColor = System.Drawing.SystemColors.ButtonFace;
			this.DescTxt.Location = new System.Drawing.Point(16, 289);
			this.DescTxt.Name = "txtDesc";
			this.DescTxt.ReadOnly = true;
			this.DescTxt.Size = new System.Drawing.Size(162, 20);
			this.DescTxt.TabIndex = 14;

			this.DescLabel.AutoSize = true;
			this.DescLabel.Location = new System.Drawing.Point(13, 273);
			this.DescLabel.Name = "lblDesc";
			this.DescLabel.Size = new System.Drawing.Size(63, 13);
			this.DescLabel.TabIndex = 13;
			this.DescLabel.Text = "Description:";

			this.TheaterText.BackColor = System.Drawing.SystemColors.ButtonFace;
			this.TheaterText.Location = new System.Drawing.Point(16, 252);
			this.TheaterText.Name = "txtTheater";
			this.TheaterText.ReadOnly = true;
			this.TheaterText.Size = new System.Drawing.Size(162, 20);
			this.TheaterText.TabIndex = 12;

			this.TheaterLabel.AutoSize = true;
			this.TheaterLabel.Location = new System.Drawing.Point(13, 236);
			this.TheaterLabel.Name = "lblTheater";
			this.TheaterLabel.Size = new System.Drawing.Size(47, 13);
			this.TheaterLabel.TabIndex = 11;
			this.TheaterLabel.Text = "Tileset:";

			this.AuthorText.BackColor = System.Drawing.SystemColors.ButtonFace;
			this.AuthorText.Location = new System.Drawing.Point(16, 214);
			this.AuthorText.Name = "txtAuthor";
			this.AuthorText.ReadOnly = true;
			this.AuthorText.Size = new System.Drawing.Size(162, 20);
			this.AuthorText.TabIndex = 10;

			this.AuthorLabel.AutoSize = true;
			this.AuthorLabel.Location = new System.Drawing.Point(13, 198);
			this.AuthorLabel.Name = "lblAuthor";
			this.AuthorLabel.Size = new System.Drawing.Size(41, 13);
			this.AuthorLabel.TabIndex = 9;
			this.AuthorLabel.Text = "Author:";

			this.TitleText.BackColor = System.Drawing.SystemColors.ButtonFace;
			this.TitleText.Location = new System.Drawing.Point(16, 177);
			this.TitleText.Name = "txtTitle";
			this.TitleText.ReadOnly = true;
			this.TitleText.Size = new System.Drawing.Size(162, 20);
			this.TitleText.TabIndex = 8;

			this.MapNameLabel.AutoSize = true;
			this.MapNameLabel.Location = new System.Drawing.Point(13, 161);
			this.MapNameLabel.Name = "lblMapName";
			this.MapNameLabel.Size = new System.Drawing.Size(30, 13);
			this.MapNameLabel.TabIndex = 7;
			this.MapNameLabel.Text = "Title:";

			this.MiniMapLabel.AutoSize = true;
			this.MiniMapLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, (byte)204);
			this.MiniMapLabel.Location = new System.Drawing.Point(29, 9);
			this.MiniMapLabel.Name = "lblMinimap";
			this.MiniMapLabel.Size = new System.Drawing.Size(71, 13);
			this.MiniMapLabel.TabIndex = 6;
			this.MiniMapLabel.Text = "Map preview:";

			this.pictureBox1.Image = (System.Drawing.Image)resources.GetObject("pictureBox1.Image");
			this.pictureBox1.Location = new System.Drawing.Point(336, -9);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(54, 35);
			this.pictureBox1.TabIndex = 7;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.Visible = false;

			this.AcceptButton = this.ButtonOkay;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.ButtonCancel;
			this.ClientSize = new System.Drawing.Size(494, 402);
			this.Controls.Add(this.SplitContainer1);
			this.Controls.Add(this.BottomPanel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MapSelect";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Select map";
			this.Load += new System.EventHandler(this.MapSelect_Load);
			((System.ComponentModel.ISupportInitialize)this.MiniMapBox).EndInit();
			this.BottomPanel.ResumeLayout(false);
			this.BottomPanel.PerformLayout();
			this.SplitContainer1.Panel1.ResumeLayout(false);
			this.SplitContainer1.Panel1.PerformLayout();
			this.SplitContainer1.Panel2.ResumeLayout(false);
			this.SplitContainer1.Panel2.PerformLayout();
			this.SplitContainer1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
			this.ResumeLayout(false);
		}

		public System.Windows.Forms.ListView MapList;
		public System.Windows.Forms.Button ButtonCancel;
		public System.Windows.Forms.Button ButtonOkay;
		public System.Windows.Forms.Label NewLabel;
		public System.Windows.Forms.TextBox NewText;
		public System.Windows.Forms.ColumnHeader ColumnMapName;
		public System.Windows.Forms.ImageList MapIconsList;
		public System.Windows.Forms.PictureBox MiniMapBox;
		public System.Windows.Forms.Panel BottomPanel;
		public System.Windows.Forms.SplitContainer SplitContainer1;
		public System.Windows.Forms.Label MiniMapLabel;
		public System.Windows.Forms.TextBox TheaterText;
		public System.Windows.Forms.Label TheaterLabel;
		public System.Windows.Forms.TextBox AuthorText;
		public System.Windows.Forms.Label AuthorLabel;
		public System.Windows.Forms.TextBox TitleText;
		public System.Windows.Forms.Label MapNameLabel;
		public System.Windows.Forms.TextBox DescTxt;
		public System.Windows.Forms.Label DescLabel;
		public System.Windows.Forms.Label MapListLabel;
		public System.Windows.Forms.Label PathOutLabel;
		public System.Windows.Forms.Label PathLabel;
		public System.Windows.Forms.TextBox PathOutText;
		private System.Windows.Forms.PictureBox pictureBox1;
	}
}