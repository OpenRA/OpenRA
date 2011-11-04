#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Editor
{
	partial class Form1
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.pmMiniMap = new System.Windows.Forms.PictureBox();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.tilePalette = new System.Windows.Forms.FlowLayoutPanel();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.panel1 = new System.Windows.Forms.Panel();
			this.actorPalette = new System.Windows.Forms.FlowLayoutPanel();
			this.actorOwnerChooser = new System.Windows.Forms.ComboBox();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.resourcePalette = new System.Windows.Forms.FlowLayoutPanel();
			this.surface1 = new OpenRA.Editor.Surface();
			this.tt = new System.Windows.Forms.ToolTip(this.components);
			this.splitContainer3 = new System.Windows.Forms.SplitContainer();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.cCRedAlertMapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.bitmapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuExport = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuMinimapToPNG = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.resizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showActorNamesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showGridToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fixOpenAreasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setupDefaultPlayersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripComboBox1 = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabelFiller = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabelMousePosition = new System.Windows.Forms.ToolStripStatusLabel();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pmMiniMap)).BeginInit();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.panel1.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.splitContainer3.Panel1.SuspendLayout();
			this.splitContainer3.Panel2.SuspendLayout();
			this.splitContainer3.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.surface1);
			this.splitContainer1.Size = new System.Drawing.Size(985, 744);
			this.splitContainer1.SplitterDistance = 198;
			this.splitContainer1.TabIndex = 0;
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.pmMiniMap);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.tabControl1);
			this.splitContainer2.Size = new System.Drawing.Size(198, 744);
			this.splitContainer2.SplitterDistance = 164;
			this.splitContainer2.TabIndex = 1;
			// 
			// pmMiniMap
			// 
			this.pmMiniMap.BackColor = System.Drawing.Color.Black;
			this.pmMiniMap.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pmMiniMap.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pmMiniMap.Location = new System.Drawing.Point(0, 0);
			this.pmMiniMap.Name = "pmMiniMap";
			this.pmMiniMap.Size = new System.Drawing.Size(198, 164);
			this.pmMiniMap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pmMiniMap.TabIndex = 1;
			this.pmMiniMap.TabStop = false;
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.tabControl1.Multiline = true;
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.Padding = new System.Drawing.Point(6, 0);
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(198, 576);
			this.tabControl1.TabIndex = 0;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.tilePalette);
			this.tabPage1.Location = new System.Drawing.Point(4, 20);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(190, 552);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Templates";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// tilePalette
			// 
			this.tilePalette.AutoScroll = true;
			this.tilePalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tilePalette.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tilePalette.Location = new System.Drawing.Point(3, 3);
			this.tilePalette.Name = "tilePalette";
			this.tilePalette.Size = new System.Drawing.Size(184, 546);
			this.tilePalette.TabIndex = 1;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.panel1);
			this.tabPage2.Controls.Add(this.actorOwnerChooser);
			this.tabPage2.Location = new System.Drawing.Point(4, 20);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(190, 552);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Actors";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.actorPalette);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(3, 24);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(184, 525);
			this.panel1.TabIndex = 4;
			// 
			// actorPalette
			// 
			this.actorPalette.AutoScroll = true;
			this.actorPalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.actorPalette.Dock = System.Windows.Forms.DockStyle.Fill;
			this.actorPalette.Location = new System.Drawing.Point(0, 0);
			this.actorPalette.Name = "actorPalette";
			this.actorPalette.Size = new System.Drawing.Size(184, 525);
			this.actorPalette.TabIndex = 3;
			// 
			// actorOwnerChooser
			// 
			this.actorOwnerChooser.Dock = System.Windows.Forms.DockStyle.Top;
			this.actorOwnerChooser.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.actorOwnerChooser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.actorOwnerChooser.FormattingEnabled = true;
			this.actorOwnerChooser.Location = new System.Drawing.Point(3, 3);
			this.actorOwnerChooser.Name = "actorOwnerChooser";
			this.actorOwnerChooser.Size = new System.Drawing.Size(184, 21);
			this.actorOwnerChooser.TabIndex = 3;
			this.actorOwnerChooser.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.onDrawPlayerItem);
			this.actorOwnerChooser.SelectionChangeCommitted += new System.EventHandler(this.onSelectOwner);
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.resourcePalette);
			this.tabPage3.Location = new System.Drawing.Point(4, 20);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Size = new System.Drawing.Size(190, 552);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Resources";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// resourcePalette
			// 
			this.resourcePalette.AutoScroll = true;
			this.resourcePalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.resourcePalette.Dock = System.Windows.Forms.DockStyle.Fill;
			this.resourcePalette.Location = new System.Drawing.Point(0, 0);
			this.resourcePalette.Name = "resourcePalette";
			this.resourcePalette.Size = new System.Drawing.Size(190, 552);
			this.resourcePalette.TabIndex = 3;
			// 
			// surface1
			// 
			this.surface1.BackColor = System.Drawing.Color.Black;
			this.surface1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.surface1.Location = new System.Drawing.Point(0, 0);
			this.surface1.Name = "surface1";
			this.surface1.Size = new System.Drawing.Size(783, 744);
			this.surface1.TabIndex = 5;
			this.surface1.Text = "surface1";
			// 
			// tt
			// 
			this.tt.ShowAlways = true;
			// 
			// splitContainer3
			// 
			this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer3.IsSplitterFixed = true;
			this.splitContainer3.Location = new System.Drawing.Point(0, 0);
			this.splitContainer3.Name = "splitContainer3";
			this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer3.Panel1
			// 
			this.splitContainer3.Panel1.Controls.Add(this.menuStrip1);
			// 
			// splitContainer3.Panel2
			// 
			this.splitContainer3.Panel2.Controls.Add(this.splitContainer1);
			this.splitContainer3.Size = new System.Drawing.Size(985, 773);
			this.splitContainer3.SplitterDistance = 25;
			this.splitContainer3.TabIndex = 6;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.mapToolStripMenuItem,
            this.toolStripComboBox1,
            this.toolStripLabel1});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(985, 27);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.toolStripSeparator1,
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripSeparator2,
            this.toolStripMenuItem1,
            this.mnuExport,
            this.toolStripSeparator3,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 23);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// newToolStripMenuItem
			// 
			this.newToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("newToolStripMenuItem.Image")));
			this.newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
			this.newToolStripMenuItem.Name = "newToolStripMenuItem";
			this.newToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.newToolStripMenuItem.Text = "&New...";
			this.newToolStripMenuItem.Click += new System.EventHandler(this.NewClicked);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(120, 6);
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripMenuItem.Image")));
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.openToolStripMenuItem.Text = "&Open...";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenClicked);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Enabled = false;
			this.saveToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripMenuItem.Image")));
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.SaveClicked);
			// 
			// saveAsToolStripMenuItem
			// 
			this.saveAsToolStripMenuItem.Enabled = false;
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.saveAsToolStripMenuItem.Text = "Save &As...";
			this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsClicked);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(120, 6);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cCRedAlertMapToolStripMenuItem,
            this.bitmapToolStripMenuItem});
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(123, 22);
			this.toolStripMenuItem1.Text = "&Import";
			// 
			// cCRedAlertMapToolStripMenuItem
			// 
			this.cCRedAlertMapToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("cCRedAlertMapToolStripMenuItem.Image")));
			this.cCRedAlertMapToolStripMenuItem.Name = "cCRedAlertMapToolStripMenuItem";
			this.cCRedAlertMapToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.cCRedAlertMapToolStripMenuItem.Text = "&C&&C / Red Alert Map...";
			this.cCRedAlertMapToolStripMenuItem.Click += new System.EventHandler(this.ImportLegacyMapClicked);
			// 
			// bitmapToolStripMenuItem
			// 
			this.bitmapToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("bitmapToolStripMenuItem.Image")));
			this.bitmapToolStripMenuItem.Name = "bitmapToolStripMenuItem";
			this.bitmapToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
			this.bitmapToolStripMenuItem.Text = "&Bitmap...";
			this.bitmapToolStripMenuItem.Visible = false;
			// 
			// mnuExport
			// 
			this.mnuExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuMinimapToPNG});
			this.mnuExport.Name = "mnuExport";
			this.mnuExport.Size = new System.Drawing.Size(123, 22);
			this.mnuExport.Text = "&Export";
			// 
			// mnuMinimapToPNG
			// 
			this.mnuMinimapToPNG.Enabled = false;
			this.mnuMinimapToPNG.Image = ((System.Drawing.Image)(resources.GetObject("mnuMinimapToPNG.Image")));
			this.mnuMinimapToPNG.Name = "mnuMinimapToPNG";
			this.mnuMinimapToPNG.Size = new System.Drawing.Size(163, 22);
			this.mnuMinimapToPNG.Text = "Minimap to PNG";
			this.mnuMinimapToPNG.Click += new System.EventHandler(this.ExportMinimap);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(120, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.CloseClicked);
			// 
			// mapToolStripMenuItem
			// 
			this.mapToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.propertiesToolStripMenuItem,
            this.resizeToolStripMenuItem,
            this.showActorNamesToolStripMenuItem,
            this.showGridToolStripMenuItem,
            this.fixOpenAreasToolStripMenuItem,
            this.setupDefaultPlayersMenuItem});
			this.mapToolStripMenuItem.Name = "mapToolStripMenuItem";
			this.mapToolStripMenuItem.Size = new System.Drawing.Size(43, 23);
			this.mapToolStripMenuItem.Text = "&Map";
			// 
			// propertiesToolStripMenuItem
			// 
			this.propertiesToolStripMenuItem.Enabled = false;
			this.propertiesToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("propertiesToolStripMenuItem.Image")));
			this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
			this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.propertiesToolStripMenuItem.Text = "&Properties...";
			this.propertiesToolStripMenuItem.Click += new System.EventHandler(this.PropertiesClicked);
			// 
			// resizeToolStripMenuItem
			// 
			this.resizeToolStripMenuItem.Enabled = false;
			this.resizeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("resizeToolStripMenuItem.Image")));
			this.resizeToolStripMenuItem.Name = "resizeToolStripMenuItem";
			this.resizeToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.resizeToolStripMenuItem.Text = "&Resize...";
			this.resizeToolStripMenuItem.Click += new System.EventHandler(this.ResizeClicked);
			// 
			// showActorNamesToolStripMenuItem
			// 
			this.showActorNamesToolStripMenuItem.Name = "showActorNamesToolStripMenuItem";
			this.showActorNamesToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.showActorNamesToolStripMenuItem.Text = "Show Actor &Names";
			this.showActorNamesToolStripMenuItem.Click += new System.EventHandler(this.ShowActorNamesClicked);
			// 
			// showGridToolStripMenuItem
			// 
			this.showGridToolStripMenuItem.Name = "showGridToolStripMenuItem";
			this.showGridToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.showGridToolStripMenuItem.Text = "Show &Grid";
			this.showGridToolStripMenuItem.Click += new System.EventHandler(this.ShowGridClicked);
			// 
			// fixOpenAreasToolStripMenuItem
			// 
			this.fixOpenAreasToolStripMenuItem.Name = "fixOpenAreasToolStripMenuItem";
			this.fixOpenAreasToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.fixOpenAreasToolStripMenuItem.Text = "&Fix Open Areas";
			this.fixOpenAreasToolStripMenuItem.Click += new System.EventHandler(this.FixOpenAreas);
			// 
			// setupDefaultPlayersMenuItem
			// 
			this.setupDefaultPlayersMenuItem.Name = "setupDefaultPlayersMenuItem";
			this.setupDefaultPlayersMenuItem.Size = new System.Drawing.Size(185, 22);
			this.setupDefaultPlayersMenuItem.Text = "&Setup Default Players";
			this.setupDefaultPlayersMenuItem.Click += new System.EventHandler(this.SetupDefaultPlayers);
			// 
			// toolStripComboBox1
			// 
			this.toolStripComboBox1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.toolStripComboBox1.Name = "toolStripComboBox1";
			this.toolStripComboBox1.Size = new System.Drawing.Size(121, 23);
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(71, 20);
			this.toolStripLabel1.Text = "Active Mod:";
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabelFiller,
            this.toolStripStatusLabelMousePosition});
			this.statusStrip1.Location = new System.Drawing.Point(0, 751);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(985, 22);
			this.statusStrip1.TabIndex = 7;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripStatusLabelFiller
			// 
			this.toolStripStatusLabelFiller.Name = "toolStripStatusLabelFiller";
			this.toolStripStatusLabelFiller.Size = new System.Drawing.Size(948, 17);
			this.toolStripStatusLabelFiller.Spring = true;
			// 
			// toolStripStatusLabelMousePosition
			// 
			this.toolStripStatusLabelMousePosition.Name = "toolStripStatusLabelMousePosition";
			this.toolStripStatusLabelMousePosition.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.toolStripStatusLabelMousePosition.Size = new System.Drawing.Size(22, 17);
			this.toolStripStatusLabelMousePosition.Text = "0,0";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(985, 773);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.splitContainer3);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "OpenRA Editor";
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyUp);
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			this.splitContainer2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pmMiniMap)).EndInit();
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.splitContainer3.Panel1.ResumeLayout(false);
			this.splitContainer3.Panel1.PerformLayout();
			this.splitContainer3.Panel2.ResumeLayout(false);
			this.splitContainer3.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.ToolTip tt;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.FlowLayoutPanel tilePalette;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.FlowLayoutPanel resourcePalette;
		private Surface surface1;
		private System.Windows.Forms.PictureBox pmMiniMap;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.SplitContainer splitContainer3;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelMousePosition;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelFiller;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem cCRedAlertMapToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem bitmapToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem mnuExport;
		private System.Windows.Forms.ToolStripMenuItem mnuMinimapToPNG;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem mapToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem propertiesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem resizeToolStripMenuItem;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
		private System.Windows.Forms.ToolStripComboBox toolStripComboBox1;
		private System.Windows.Forms.ToolStripMenuItem showActorNamesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem showGridToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem fixOpenAreasToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setupDefaultPlayersMenuItem;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.FlowLayoutPanel actorPalette;
		private System.Windows.Forms.ComboBox actorOwnerChooser;

	}
}

