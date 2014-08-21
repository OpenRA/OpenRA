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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.miniMapBox = new System.Windows.Forms.PictureBox();
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
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.toolStripMenuItemNew = new System.Windows.Forms.ToolStripButton();
			this.toolStripMenuItemOpen = new System.Windows.Forms.ToolStripButton();
			this.toolStripMenuItemSave = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripMenuItemProperties = new System.Windows.Forms.ToolStripButton();
			this.toolStripMenuItemResize = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripMenuItemShowActorNames = new System.Windows.Forms.ToolStripButton();
			this.toolStripMenuItemShowGrid = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator12 = new System.Windows.Forms.ToolStripSeparator();
			this.zoomIntoolStripButton = new System.Windows.Forms.ToolStripButton();
			this.zoomOutToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.panToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.showRulerToolStripItem = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripMenuItemFixOpenAreas = new System.Windows.Forms.ToolStripButton();
			this.toolStripMenuItemSetupDefaultPlayers = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
			this.eraserToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.toolStripMenuItemCopySelection = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.quickhelpToolStripButton = new System.Windows.Forms.ToolStripButton();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.miniMapExport = new System.Windows.Forms.ToolStripMenuItem();
			this.miniMapToPng = new System.Windows.Forms.ToolStripMenuItem();
			this.fullMapRenderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.resizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
			this.showActorNamesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showGridToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showRulerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.fixOpenAreasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setupDefaultPlayersMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.copySelectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripComboBox1 = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openRAWebsiteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openRAResourcesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.wikiDocumentationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.discussionForumsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sourceCodeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.issueTrackerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.developerBountiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabelFiller = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabelMousePosition = new System.Windows.Forms.ToolStripStatusLabel();
			this.bottomToolStripPanel = new System.Windows.Forms.ToolStripPanel();
			this.topToolStripPanel = new System.Windows.Forms.ToolStripPanel();
			this.rightToolStripPanel = new System.Windows.Forms.ToolStripPanel();
			this.leftToolStripPanel = new System.Windows.Forms.ToolStripPanel();
			this.contentPanel = new System.Windows.Forms.ToolStripContentPanel();
			this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
			this.cashToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.miniMapBox)).BeginInit();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.panel1.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.splitContainer3.Panel1.SuspendLayout();
			this.splitContainer3.Panel2.SuspendLayout();
			this.splitContainer3.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
			this.toolStripContainer1.ContentPanel.SuspendLayout();
			this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer1.SuspendLayout();
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
			this.splitContainer1.Size = new System.Drawing.Size(985, 695);
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
			this.splitContainer2.Panel1.Controls.Add(this.miniMapBox);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.tabControl1);
			this.splitContainer2.Size = new System.Drawing.Size(198, 695);
			this.splitContainer2.SplitterDistance = 153;
			this.splitContainer2.TabIndex = 1;
			// 
			// pmMiniMap
			// 
			this.miniMapBox.BackColor = System.Drawing.Color.Black;
			this.miniMapBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.miniMapBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.miniMapBox.Location = new System.Drawing.Point(0, 0);
			this.miniMapBox.Name = "pmMiniMap";
			this.miniMapBox.Size = new System.Drawing.Size(198, 153);
			this.miniMapBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.miniMapBox.TabIndex = 1;
			this.miniMapBox.TabStop = false;
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
			this.tabControl1.Size = new System.Drawing.Size(198, 538);
			this.tabControl1.TabIndex = 0;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.tilePalette);
			this.tabPage1.Location = new System.Drawing.Point(4, 20);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(190, 514);
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
			this.tilePalette.Size = new System.Drawing.Size(184, 508);
			this.tilePalette.TabIndex = 1;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.panel1);
			this.tabPage2.Controls.Add(this.actorOwnerChooser);
			this.tabPage2.Location = new System.Drawing.Point(4, 20);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(190, 514);
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
			this.panel1.Size = new System.Drawing.Size(184, 487);
			this.panel1.TabIndex = 4;
			// 
			// actorPalette
			// 
			this.actorPalette.AutoScroll = true;
			this.actorPalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.actorPalette.Dock = System.Windows.Forms.DockStyle.Fill;
			this.actorPalette.Location = new System.Drawing.Point(0, 0);
			this.actorPalette.Name = "actorPalette";
			this.actorPalette.Size = new System.Drawing.Size(184, 487);
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
			this.actorOwnerChooser.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.DrawPlayerListItem);
			this.actorOwnerChooser.SelectionChangeCommitted += new System.EventHandler(this.OnSelectedPlayerChanged);
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.resourcePalette);
			this.tabPage3.Location = new System.Drawing.Point(4, 20);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Size = new System.Drawing.Size(190, 514);
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
			this.resourcePalette.Size = new System.Drawing.Size(190, 514);
			this.resourcePalette.TabIndex = 3;
			// 
			// surface1
			// 
			this.surface1.BackColor = System.Drawing.Color.Black;
			this.surface1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.surface1.Location = new System.Drawing.Point(0, 0);
			this.surface1.Name = "surface1";
			this.surface1.Size = new System.Drawing.Size(783, 695);
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
			this.splitContainer3.Panel1.Controls.Add(this.toolStrip1);
			// 
			// splitContainer3.Panel2
			// 
			this.splitContainer3.Panel2.Controls.Add(this.splitContainer1);
			this.splitContainer3.Size = new System.Drawing.Size(985, 724);
			this.splitContainer3.SplitterDistance = 25;
			this.splitContainer3.TabIndex = 6;
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripMenuItemNew,
			this.toolStripMenuItemOpen,
			this.toolStripMenuItemSave,
			this.toolStripSeparator,
			this.toolStripMenuItemProperties,
			this.toolStripMenuItemResize,
			this.toolStripSeparator8,
			this.toolStripMenuItemShowActorNames,
			this.toolStripMenuItemShowGrid,
			this.toolStripSeparator12,
			this.zoomIntoolStripButton,
			this.zoomOutToolStripButton,
			this.panToolStripButton,
			this.showRulerToolStripItem,
			this.toolStripSeparator10,
			this.toolStripMenuItemFixOpenAreas,
			this.toolStripMenuItemSetupDefaultPlayers,
			this.toolStripSeparator11,
			this.eraserToolStripButton,
			this.toolStripMenuItemCopySelection,
			this.toolStripSeparator7,
			this.quickhelpToolStripButton});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.toolStrip1.Size = new System.Drawing.Size(985, 25);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// toolStripMenuItemNew
			// 
			this.toolStripMenuItemNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMenuItemNew.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemNew.Image")));
			this.toolStripMenuItemNew.ImageTransparentColor = System.Drawing.Color.Fuchsia;
			this.toolStripMenuItemNew.Name = "toolStripMenuItemNew";
			this.toolStripMenuItemNew.Size = new System.Drawing.Size(23, 22);
			this.toolStripMenuItemNew.Text = "&New...";
			this.toolStripMenuItemNew.ToolTipText = "Create a new blank map.";
			this.toolStripMenuItemNew.Click += new System.EventHandler(this.ToolStripMenuItemNewClick);
			// 
			// toolStripMenuItemOpen
			// 
			this.toolStripMenuItemOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMenuItemOpen.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemOpen.Image")));
			this.toolStripMenuItemOpen.Name = "toolStripMenuItemOpen";
			this.toolStripMenuItemOpen.Size = new System.Drawing.Size(23, 22);
			this.toolStripMenuItemOpen.Text = "&Open...";
			this.toolStripMenuItemOpen.ToolTipText = "Open an existing map.";
			this.toolStripMenuItemOpen.Click += new System.EventHandler(this.ToolStripMenuItemOpenClick);
			// 
			// toolStripMenuItemSave
			// 
			this.toolStripMenuItemSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMenuItemSave.Enabled = false;
			this.toolStripMenuItemSave.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemSave.Image")));
			this.toolStripMenuItemSave.Name = "toolStripMenuItemSave";
			this.toolStripMenuItemSave.Size = new System.Drawing.Size(23, 22);
			this.toolStripMenuItemSave.Text = "&Save";
			this.toolStripMenuItemSave.ToolTipText = "Quicksave current map.";
			this.toolStripMenuItemSave.Click += new System.EventHandler(this.ToolStripMenuItemSaveClick);
			// 
			// toolStripSeparator
			// 
			this.toolStripSeparator.Name = "toolStripSeparator";
			this.toolStripSeparator.Size = new System.Drawing.Size(6, 25);
			// 
			// toolStripMenuItemProperties
			// 
			this.toolStripMenuItemProperties.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMenuItemProperties.Enabled = false;
			this.toolStripMenuItemProperties.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemProperties.Image")));
			this.toolStripMenuItemProperties.Name = "toolStripMenuItemProperties";
			this.toolStripMenuItemProperties.Size = new System.Drawing.Size(23, 22);
			this.toolStripMenuItemProperties.Text = "&Properties...";
			this.toolStripMenuItemProperties.ToolTipText = "Edit Metadata";
			this.toolStripMenuItemProperties.Click += new System.EventHandler(this.ToolStripMenuItemPropertiesClick);
			// 
			// toolStripMenuItemResize
			// 
			this.toolStripMenuItemResize.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMenuItemResize.Enabled = false;
			this.toolStripMenuItemResize.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemResize.Image")));
			this.toolStripMenuItemResize.Name = "toolStripMenuItemResize";
			this.toolStripMenuItemResize.Size = new System.Drawing.Size(23, 22);
			this.toolStripMenuItemResize.Text = "&Resize...";
			this.toolStripMenuItemResize.ToolTipText = "Change the map borders and dimensions.";
			this.toolStripMenuItemResize.Click += new System.EventHandler(this.ToolStripMenuItemResizeClick);
			// 
			// toolStripSeparator8
			// 
			this.toolStripSeparator8.Name = "toolStripSeparator8";
			this.toolStripSeparator8.Size = new System.Drawing.Size(6, 25);
			// 
			// toolStripMenuItemShowActorNames
			// 
			this.toolStripMenuItemShowActorNames.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMenuItemShowActorNames.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemShowActorNames.Image")));
			this.toolStripMenuItemShowActorNames.Name = "toolStripMenuItemShowActorNames";
			this.toolStripMenuItemShowActorNames.Size = new System.Drawing.Size(23, 22);
			this.toolStripMenuItemShowActorNames.Text = "Show Actor &Names";
			this.toolStripMenuItemShowActorNames.ToolTipText = "If the actor has a custom name, display it.";
			this.toolStripMenuItemShowActorNames.Click += new System.EventHandler(this.ToolStripMenuItemShowActorNamesClick);
			// 
			// toolStripMenuItemShowGrid
			// 
			this.toolStripMenuItemShowGrid.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMenuItemShowGrid.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemShowGrid.Image")));
			this.toolStripMenuItemShowGrid.Name = "toolStripMenuItemShowGrid";
			this.toolStripMenuItemShowGrid.Size = new System.Drawing.Size(23, 22);
			this.toolStripMenuItemShowGrid.Text = "Show &Grid";
			this.toolStripMenuItemShowGrid.ToolTipText = "Enable a grid overlay for better orientation.";
			this.toolStripMenuItemShowGrid.Click += new System.EventHandler(this.ToolStripMenuItemShowGridClick);
			// 
			// toolStripSeparator12
			// 
			this.toolStripSeparator12.Name = "toolStripSeparator12";
			this.toolStripSeparator12.Size = new System.Drawing.Size(6, 25);
			// 
			// zoomIntoolStripButton
			// 
			this.zoomIntoolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.zoomIntoolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("zoomIntoolStripButton.Image")));
			this.zoomIntoolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.zoomIntoolStripButton.Name = "zoomIntoolStripButton";
			this.zoomIntoolStripButton.Size = new System.Drawing.Size(23, 22);
			this.zoomIntoolStripButton.Text = "Zoom in";
			this.zoomIntoolStripButton.Click += new System.EventHandler(this.ZoomInToolStripButtonClick);
			// 
			// zoomOutToolStripButton
			// 
			this.zoomOutToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.zoomOutToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("zoomOutToolStripButton.Image")));
			this.zoomOutToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.zoomOutToolStripButton.Name = "zoomOutToolStripButton";
			this.zoomOutToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.zoomOutToolStripButton.Text = "Zoom out";
			this.zoomOutToolStripButton.Click += new System.EventHandler(this.ZoomOutToolStripButtonClick);
			// 
			// panToolStripButton
			// 
			this.panToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.panToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("panToolStripButton.Image")));
			this.panToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.panToolStripButton.Name = "panToolStripButton";
			this.panToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.panToolStripButton.Text = "Pan view";
			this.panToolStripButton.Click += new System.EventHandler(this.PanToolStripButtonClick);
			//
			// showRulerToolStripItem
			// 
			this.showRulerToolStripItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.showRulerToolStripItem.Image = ((System.Drawing.Image)(resources.GetObject("showRulerToolStripItem.Image")));
			this.showRulerToolStripItem.Name = "showRulerToolStripItem";
			this.showRulerToolStripItem.Size = new System.Drawing.Size(23, 22);
			this.showRulerToolStripItem.Text = "Show Ruler";
			this.showRulerToolStripItem.Click += new System.EventHandler(this.ShowRulerToolStripItemClick);
			// 
			// toolStripSeparator10
			// 
			this.toolStripSeparator10.Name = "toolStripSeparator10";
			this.toolStripSeparator10.Size = new System.Drawing.Size(6, 25);
			// 
			// toolStripMenuItemFixOpenAreas
			// 
			this.toolStripMenuItemFixOpenAreas.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMenuItemFixOpenAreas.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemFixOpenAreas.Image")));
			this.toolStripMenuItemFixOpenAreas.Name = "toolStripMenuItemFixOpenAreas";
			this.toolStripMenuItemFixOpenAreas.Size = new System.Drawing.Size(23, 22);
			this.toolStripMenuItemFixOpenAreas.Text = "&Fix Open Areas";
			this.toolStripMenuItemFixOpenAreas.ToolTipText = "Add some randomness into clear tiles.";
			this.toolStripMenuItemFixOpenAreas.Click += new System.EventHandler(this.ToolStripMenuItemFixOpenAreasClick);
			// 
			// toolStripMenuItemSetupDefaultPlayers
			// 
			this.toolStripMenuItemSetupDefaultPlayers.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMenuItemSetupDefaultPlayers.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemSetupDefaultPlayers.Image")));
			this.toolStripMenuItemSetupDefaultPlayers.Name = "toolStripMenuItemSetupDefaultPlayers";
			this.toolStripMenuItemSetupDefaultPlayers.Size = new System.Drawing.Size(23, 22);
			this.toolStripMenuItemSetupDefaultPlayers.Text = "&Setup Default Players";
			this.toolStripMenuItemSetupDefaultPlayers.ToolTipText = "Setup the players for each spawnpoint placed.";
			this.toolStripMenuItemSetupDefaultPlayers.Click += new System.EventHandler(this.ToolStripMenuItemSetupDefaultPlayersClick);
			// 
			// toolStripSeparator11
			// 
			this.toolStripSeparator11.Name = "toolStripSeparator11";
			this.toolStripSeparator11.Size = new System.Drawing.Size(6, 25);
			// 
			// eraserToolStripButton
			// 
			this.eraserToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.eraserToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("eraserToolStripButton.Image")));
			this.eraserToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.eraserToolStripButton.Name = "eraserToolStripButton";
			this.eraserToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.eraserToolStripButton.Text = "Erase actors and resources.";
			this.eraserToolStripButton.Click += new System.EventHandler(this.EraserToolStripButtonClick);
			// 
			// toolStripMenuItemCopySelection
			// 
			this.toolStripMenuItemCopySelection.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMenuItemCopySelection.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItemCopySelection.Image")));
			this.toolStripMenuItemCopySelection.Name = "toolStripMenuItemCopySelection";
			this.toolStripMenuItemCopySelection.Size = new System.Drawing.Size(23, 22);
			this.toolStripMenuItemCopySelection.Text = "Copy Selection";
			this.toolStripMenuItemCopySelection.ToolTipText = "Copy the current selection and paste it again on left-click.";
			this.toolStripMenuItemCopySelection.Click += new System.EventHandler(this.ToolStripMenuItemCopySelectionClick);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(6, 25);
			// 
			// QuickhelpToolStripButton
			// 
			this.quickhelpToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.quickhelpToolStripButton.Image = ((System.Drawing.Image)(resources.GetObject("QuickhelpToolStripButton.Image")));
			this.quickhelpToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.quickhelpToolStripButton.Name = "QuickhelpToolStripButton";
			this.quickhelpToolStripButton.Size = new System.Drawing.Size(23, 22);
			this.quickhelpToolStripButton.Text = "Help";
			this.quickhelpToolStripButton.ToolTipText = "Display the mapping tutorial in the OpenRA wiki.";
			this.quickhelpToolStripButton.Click += new System.EventHandler(this.HelpToolStripButton_Click);
			// 
			// menuStrip1
			// 
			this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.fileToolStripMenuItem,
			this.mapToolStripMenuItem,
			this.toolStripComboBox1,
			this.toolStripLabel1,
			this.helpToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
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
			this.miniMapExport,
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
			this.newToolStripMenuItem.ToolTipText = "Create a new blank map.";
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
			this.openToolStripMenuItem.ToolTipText = "Open an existing map.";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenClicked);
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Enabled = false;
			this.saveToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripMenuItem.Image")));
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.ToolTipText = "Quicksave current map.";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.SaveClicked);
			// 
			// saveAsToolStripMenuItem
			// 
			this.saveAsToolStripMenuItem.Enabled = false;
			this.saveAsToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveAsToolStripMenuItem.Image")));
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.saveAsToolStripMenuItem.Text = "Save &As...";
			this.saveAsToolStripMenuItem.ToolTipText = "Save the map while choosing a filename.";
			this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsClicked);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(120, 6);
			// 
			// mnuExport
			// 
			this.miniMapExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.miniMapToPng,
			this.fullMapRenderToolStripMenuItem});
			this.miniMapExport.Image = ((System.Drawing.Image)(resources.GetObject("mnuExport.Image")));
			this.miniMapExport.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.miniMapExport.Name = "mnuExport";
			this.miniMapExport.Size = new System.Drawing.Size(123, 22);
			this.miniMapExport.Text = "&Export";
			// 
			// mnuMinimapToPNG
			// 
			this.miniMapToPng.Enabled = false;
			this.miniMapToPng.Image = ((System.Drawing.Image)(resources.GetObject("mnuMinimapToPNG.Image")));
			this.miniMapToPng.Name = "mnuMinimapToPNG";
			this.miniMapToPng.Size = new System.Drawing.Size(163, 22);
			this.miniMapToPng.Text = "Minimap to PNG";
			this.miniMapToPng.ToolTipText = "Save the map radar display as an image.";
			this.miniMapToPng.Click += new System.EventHandler(this.ExportMinimap);
			// 
			// fullMapRenderToolStripMenuItem
			// 
			this.fullMapRenderToolStripMenuItem.Enabled = false;
			this.fullMapRenderToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("fullMapRenderToolStripMenuItem.Image")));
			this.fullMapRenderToolStripMenuItem.Name = "fullMapRenderToolStripMenuItem";
			this.fullMapRenderToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
			this.fullMapRenderToolStripMenuItem.Text = "Full Map Render";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(120, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("exitToolStripMenuItem.Image")));
			this.exitToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.ToolTipText = "Quit the map editor.";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.CloseClicked);
			// 
			// mapToolStripMenuItem
			// 
			this.mapToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.propertiesToolStripMenuItem,
			this.resizeToolStripMenuItem,
			this.toolStripSeparator9,
			this.showActorNamesToolStripMenuItem,
			this.showGridToolStripMenuItem,
			this.showRulerToolStripMenuItem,
			this.toolStripSeparator5,
			this.fixOpenAreasToolStripMenuItem,
			this.setupDefaultPlayersMenuItem,
			this.toolStripSeparator4,
			this.copySelectionToolStripMenuItem});
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
			this.propertiesToolStripMenuItem.ToolTipText = "Edit Metadata";
			this.propertiesToolStripMenuItem.Click += new System.EventHandler(this.PropertiesClicked);
			// 
			// resizeToolStripMenuItem
			// 
			this.resizeToolStripMenuItem.Enabled = false;
			this.resizeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("resizeToolStripMenuItem.Image")));
			this.resizeToolStripMenuItem.Name = "resizeToolStripMenuItem";
			this.resizeToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.resizeToolStripMenuItem.Text = "&Resize...";
			this.resizeToolStripMenuItem.ToolTipText = "Change the map borders and dimensions.";
			this.resizeToolStripMenuItem.Click += new System.EventHandler(this.ResizeClicked);
			// 
			// toolStripSeparator9
			// 
			this.toolStripSeparator9.Name = "toolStripSeparator9";
			this.toolStripSeparator9.Size = new System.Drawing.Size(182, 6);
			// 
			// showActorNamesToolStripMenuItem
			// 
			this.showActorNamesToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("showActorNamesToolStripMenuItem.Image")));
			this.showActorNamesToolStripMenuItem.Name = "showActorNamesToolStripMenuItem";
			this.showActorNamesToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.showActorNamesToolStripMenuItem.Text = "Show &Actor Names";
			this.showActorNamesToolStripMenuItem.ToolTipText = "If the actor has a custom name, display it.";
			this.showActorNamesToolStripMenuItem.Click += new System.EventHandler(this.ShowActorNamesClicked);
			// 
			// showGridToolStripMenuItem
			// 
			this.showGridToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("showGridToolStripMenuItem.Image")));
			this.showGridToolStripMenuItem.Name = "showGridToolStripMenuItem";
			this.showGridToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.showGridToolStripMenuItem.Text = "Show &Grid";
			this.showGridToolStripMenuItem.ToolTipText = "Enable a grid overlay for better orientation.";
			this.showGridToolStripMenuItem.Click += new System.EventHandler(this.ShowGridClicked);
			// 
			// showRulerToolStripMenuItem
			// 
			this.showRulerToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("showRulerToolStripMenuItem.Image")));
			this.showRulerToolStripMenuItem.Name = "showRulerToolStripMenuItem";
			this.showRulerToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.showRulerToolStripMenuItem.Text = "Show Ruler";
			this.showRulerToolStripMenuItem.Click += new System.EventHandler(this.ShowRulerToolStripMenuItemClick);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(182, 6);
			// 
			// fixOpenAreasToolStripMenuItem
			// 
			this.fixOpenAreasToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("fixOpenAreasToolStripMenuItem.Image")));
			this.fixOpenAreasToolStripMenuItem.Name = "fixOpenAreasToolStripMenuItem";
			this.fixOpenAreasToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.fixOpenAreasToolStripMenuItem.Text = "&Fix Open Areas";
			this.fixOpenAreasToolStripMenuItem.ToolTipText = "Add some randomness into clear tiles.";
			this.fixOpenAreasToolStripMenuItem.Click += new System.EventHandler(this.FixOpenAreas);
			// 
			// setupDefaultPlayersMenuItem
			// 
			this.setupDefaultPlayersMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("setupDefaultPlayersMenuItem.Image")));
			this.setupDefaultPlayersMenuItem.Name = "setupDefaultPlayersMenuItem";
			this.setupDefaultPlayersMenuItem.Size = new System.Drawing.Size(185, 22);
			this.setupDefaultPlayersMenuItem.Text = "&Setup Default Players";
			this.setupDefaultPlayersMenuItem.ToolTipText = "Setup the players for each spawnpoint placed.";
			this.setupDefaultPlayersMenuItem.Click += new System.EventHandler(this.SetupDefaultPlayers);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(182, 6);
			// 
			// copySelectionToolStripMenuItem
			// 
			this.copySelectionToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("copySelectionToolStripMenuItem.Image")));
			this.copySelectionToolStripMenuItem.Name = "copySelectionToolStripMenuItem";
			this.copySelectionToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
			this.copySelectionToolStripMenuItem.Text = "Copy Selection";
			this.copySelectionToolStripMenuItem.ToolTipText = "Copy the current selection and paste it again on left-click.";
			this.copySelectionToolStripMenuItem.Click += new System.EventHandler(this.CopySelectionToolStripMenuItemClick);
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
			this.toolStripLabel1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripLabel1.Image")));
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(87, 20);
			this.toolStripLabel1.Text = "Active Mod:";
			this.toolStripLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.toolStripLabel1.TextDirection = System.Windows.Forms.ToolStripTextDirection.Horizontal;
			this.toolStripLabel1.ToolTipText = "Choose the OpenRA mod whose tilesets and actors shall be used.";
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.openRAWebsiteToolStripMenuItem,
			this.openRAResourcesToolStripMenuItem,
			this.wikiDocumentationToolStripMenuItem,
			this.discussionForumsToolStripMenuItem,
			this.sourceCodeToolStripMenuItem,
			this.issueTrackerToolStripMenuItem,
			this.developerBountiesToolStripMenuItem,
			this.toolStripSeparator6,
			this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 23);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// openRAWebsiteToolStripMenuItem
			// 
			this.openRAWebsiteToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openRAWebsiteToolStripMenuItem.Image")));
			this.openRAWebsiteToolStripMenuItem.Name = "openRAWebsiteToolStripMenuItem";
			this.openRAWebsiteToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.openRAWebsiteToolStripMenuItem.Text = "OpenRA &Website";
			this.openRAWebsiteToolStripMenuItem.ToolTipText = "Visit the OpenRA homepage.";
			this.openRAWebsiteToolStripMenuItem.Click += new System.EventHandler(this.OpenRAWebsiteToolStripMenuItemClick);
			// 
			// openRAResourcesToolStripMenuItem
			// 
			this.openRAResourcesToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openRAResourcesToolStripMenuItem.Image")));
			this.openRAResourcesToolStripMenuItem.Name = "openRAResourcesToolStripMenuItem";
			this.openRAResourcesToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.openRAResourcesToolStripMenuItem.Text = "OpenRA &Resources";
			this.openRAResourcesToolStripMenuItem.ToolTipText = "Share your maps and replays by uploading on this file exchange community.";
			this.openRAResourcesToolStripMenuItem.Click += new System.EventHandler(this.OpenRAResourcesToolStripMenuItemClick);
			// 
			// wikiDocumentationToolStripMenuItem
			// 
			this.wikiDocumentationToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("wikiDocumentationToolStripMenuItem.Image")));
			this.wikiDocumentationToolStripMenuItem.Name = "wikiDocumentationToolStripMenuItem";
			this.wikiDocumentationToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.wikiDocumentationToolStripMenuItem.Text = "Wiki &Documentation";
			this.wikiDocumentationToolStripMenuItem.ToolTipText = "Read and contribute to the developer documentation.";
			this.wikiDocumentationToolStripMenuItem.Click += new System.EventHandler(this.WikiDocumentationToolStripMenuItemClick);
			// 
			// discussionForumsToolStripMenuItem
			// 
			this.discussionForumsToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("discussionForumsToolStripMenuItem.Image")));
			this.discussionForumsToolStripMenuItem.Name = "discussionForumsToolStripMenuItem";
			this.discussionForumsToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.discussionForumsToolStripMenuItem.Text = "Discussion &Forums";
			this.discussionForumsToolStripMenuItem.ToolTipText = "Discuss OpenRA related matters in a bulletin board forum.";
			this.discussionForumsToolStripMenuItem.Click += new System.EventHandler(this.DiscussionForumsToolStripMenuItemClick);
			// 
			// sourceCodeToolStripMenuItem
			// 
			this.sourceCodeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("sourceCodeToolStripMenuItem.Image")));
			this.sourceCodeToolStripMenuItem.Name = "sourceCodeToolStripMenuItem";
			this.sourceCodeToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.sourceCodeToolStripMenuItem.Text = "Source &Code";
			this.sourceCodeToolStripMenuItem.ToolTipText = "Browse and download the source code. Fix what annoys you. Patches are welcome.";
			this.sourceCodeToolStripMenuItem.Click += new System.EventHandler(this.SourceCodeToolStripMenuItemClick);
			// 
			// issueTrackerToolStripMenuItem
			// 
			this.issueTrackerToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("issueTrackerToolStripMenuItem.Image")));
			this.issueTrackerToolStripMenuItem.Name = "issueTrackerToolStripMenuItem";
			this.issueTrackerToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.issueTrackerToolStripMenuItem.Text = "Issue &Tracker";
			this.issueTrackerToolStripMenuItem.ToolTipText = "Report problems and request features.";
			this.issueTrackerToolStripMenuItem.Click += new System.EventHandler(this.IssueTrackerToolStripMenuItemClick);
			// 
			// developerBountiesToolStripMenuItem
			// 
			this.developerBountiesToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("developerBountiesToolStripMenuItem.Image")));
			this.developerBountiesToolStripMenuItem.Name = "developerBountiesToolStripMenuItem";
			this.developerBountiesToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.developerBountiesToolStripMenuItem.Text = "Developer &Bounties";
			this.developerBountiesToolStripMenuItem.ToolTipText = "Hire a developer to get OpenRA modified to your wishes.";
			this.developerBountiesToolStripMenuItem.Click += new System.EventHandler(this.DeveloperBountiesToolStripMenuItemClick);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(180, 6);
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("aboutToolStripMenuItem.Image")));
			this.aboutToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			this.aboutToolStripMenuItem.Text = "&About";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItemClick);
			// 
			// statusStrip1
			// 
			this.statusStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.cashToolStripStatusLabel,
			this.toolStripStatusLabelFiller,
			this.toolStripStatusLabelMousePosition});
			this.statusStrip1.Location = new System.Drawing.Point(0, 0);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(985, 22);
			this.statusStrip1.TabIndex = 7;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// toolStripStatusLabelFiller
			// 
			this.toolStripStatusLabelFiller.Name = "toolStripStatusLabelFiller";
			this.toolStripStatusLabelFiller.Size = new System.Drawing.Size(872, 17);
			this.toolStripStatusLabelFiller.Spring = true;
			// 
			// toolStripStatusLabelMousePosition
			// 
			this.toolStripStatusLabelMousePosition.Image = ((System.Drawing.Image)(resources.GetObject("toolStripStatusLabelMousePosition.Image")));
			this.toolStripStatusLabelMousePosition.Name = "toolStripStatusLabelMousePosition";
			this.toolStripStatusLabelMousePosition.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.toolStripStatusLabelMousePosition.Size = new System.Drawing.Size(38, 17);
			this.toolStripStatusLabelMousePosition.Text = "0,0";
			// 
			// BottomToolStripPanel
			// 
			this.bottomToolStripPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.bottomToolStripPanel.Location = new System.Drawing.Point(0, 25);
			this.bottomToolStripPanel.Name = "BottomToolStripPanel";
			this.bottomToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.bottomToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.bottomToolStripPanel.Size = new System.Drawing.Size(985, 0);
			// 
			// TopToolStripPanel
			// 
			this.topToolStripPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.topToolStripPanel.Location = new System.Drawing.Point(0, 0);
			this.topToolStripPanel.Name = "TopToolStripPanel";
			this.topToolStripPanel.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.topToolStripPanel.RowMargin = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.topToolStripPanel.Size = new System.Drawing.Size(985, 0);
			// 
			// RightToolStripPanel
			// 
			this.rightToolStripPanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.rightToolStripPanel.Location = new System.Drawing.Point(985, 0);
			this.rightToolStripPanel.Name = "RightToolStripPanel";
			this.rightToolStripPanel.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.rightToolStripPanel.RowMargin = new System.Windows.Forms.Padding(0, 3, 0, 0);
			this.rightToolStripPanel.Size = new System.Drawing.Size(0, 25);
			// 
			// LeftToolStripPanel
			// 
			this.leftToolStripPanel.Dock = System.Windows.Forms.DockStyle.Left;
			this.leftToolStripPanel.Location = new System.Drawing.Point(0, 0);
			this.leftToolStripPanel.Name = "LeftToolStripPanel";
			this.leftToolStripPanel.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.leftToolStripPanel.RowMargin = new System.Windows.Forms.Padding(0, 3, 0, 0);
			this.leftToolStripPanel.Size = new System.Drawing.Size(0, 25);
			// 
			// ContentPanel
			// 
			this.contentPanel.Size = new System.Drawing.Size(985, 25);
			// 
			// toolStripContainer1
			// 
			// 
			// toolStripContainer1.BottomToolStripPanel
			// 
			this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.statusStrip1);
			// 
			// toolStripContainer1.ContentPanel
			// 
			this.toolStripContainer1.ContentPanel.AutoScroll = true;
			this.toolStripContainer1.ContentPanel.Controls.Add(this.splitContainer3);
			this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(985, 724);
			this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
			this.toolStripContainer1.Name = "toolStripContainer1";
			this.toolStripContainer1.Size = new System.Drawing.Size(985, 773);
			this.toolStripContainer1.TabIndex = 8;
			this.toolStripContainer1.Text = "toolStripContainer1";
			// 
			// toolStripContainer1.TopToolStripPanel
			// 
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.menuStrip1);
			// 
			// cashToolStripStatusLabel
			// 
			this.cashToolStripStatusLabel.Image = ((System.Drawing.Image)(resources.GetObject("cashToolStripStatusLabel.Image")));
			this.cashToolStripStatusLabel.Name = "cashToolStripStatusLabel";
			this.cashToolStripStatusLabel.Size = new System.Drawing.Size(29, 17);
			this.cashToolStripStatusLabel.Text = "0";
			this.cashToolStripStatusLabel.ToolTipText = "Net worth of the maps resources in cash";
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(985, 773);
			this.Controls.Add(this.toolStripContainer1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "OpenRA Editor";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyUp);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			this.splitContainer2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.miniMapBox)).EndInit();
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.tabPage3.ResumeLayout(false);
			this.splitContainer3.Panel1.ResumeLayout(false);
			this.splitContainer3.Panel1.PerformLayout();
			this.splitContainer3.Panel2.ResumeLayout(false);
			this.splitContainer3.ResumeLayout(false);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
			this.toolStripContainer1.ContentPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.PerformLayout();
			this.toolStripContainer1.ResumeLayout(false);
			this.toolStripContainer1.PerformLayout();
			this.ResumeLayout(false);
		}
		private System.Windows.Forms.ToolStripStatusLabel cashToolStripStatusLabel;
		private System.Windows.Forms.ToolStripButton panToolStripButton;
		private System.Windows.Forms.ToolStripButton zoomOutToolStripButton;
		private System.Windows.Forms.ToolStripButton zoomIntoolStripButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator12;
		private System.Windows.Forms.ToolStripButton showRulerToolStripItem;
		private System.Windows.Forms.ToolStripMenuItem showRulerToolStripMenuItem;
		private System.Windows.Forms.ToolStripButton eraserToolStripButton;

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
		private System.Windows.Forms.PictureBox miniMapBox;
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
		private System.Windows.Forms.ToolStripMenuItem miniMapExport;
		private System.Windows.Forms.ToolStripMenuItem miniMapToPng;
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
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem copySelectionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openRAWebsiteToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem issueTrackerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem developerBountiesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem discussionForumsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem wikiDocumentationToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openRAResourcesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripMenuItem sourceCodeToolStripMenuItem;
		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripButton quickhelpToolStripButton;
		private System.Windows.Forms.ToolStripPanel bottomToolStripPanel;
		private System.Windows.Forms.ToolStripPanel topToolStripPanel;
		private System.Windows.Forms.ToolStripPanel rightToolStripPanel;
		private System.Windows.Forms.ToolStripPanel leftToolStripPanel;
		private System.Windows.Forms.ToolStripContentPanel contentPanel;
		private System.Windows.Forms.ToolStripButton toolStripMenuItemNew;
		private System.Windows.Forms.ToolStripButton toolStripMenuItemOpen;
		private System.Windows.Forms.ToolStripButton toolStripMenuItemSave;
		private System.Windows.Forms.ToolStripButton toolStripMenuItemProperties;
		private System.Windows.Forms.ToolStripButton toolStripMenuItemResize;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
		private System.Windows.Forms.ToolStripButton toolStripMenuItemShowActorNames;
		private System.Windows.Forms.ToolStripButton toolStripMenuItemShowGrid;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
		private System.Windows.Forms.ToolStripButton toolStripMenuItemFixOpenAreas;
		private System.Windows.Forms.ToolStripButton toolStripMenuItemSetupDefaultPlayers;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
		private System.Windows.Forms.ToolStripButton toolStripMenuItemCopySelection;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
		private System.Windows.Forms.ToolStripMenuItem fullMapRenderToolStripMenuItem;
	}
}
