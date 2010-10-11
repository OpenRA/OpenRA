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
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.pmMiniMap = new System.Windows.Forms.PictureBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tilePalette = new System.Windows.Forms.FlowLayoutPanel();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.actorPalette = new System.Windows.Forms.FlowLayoutPanel();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.resourcePalette = new System.Windows.Forms.FlowLayoutPanel();
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
            this.exotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mapToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.spawnpointsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.layersFloaterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tt = new System.Windows.Forms.ToolTip(this.components);
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.surface1 = new OpenRA.Editor.Surface();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
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
            this.tabPage3.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.splitContainer1);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(985, 727);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Padding = new System.Windows.Forms.Padding(0, 0, 0, 22);
            this.toolStripContainer1.Size = new System.Drawing.Size(985, 773);
            this.toolStripContainer1.TabIndex = 1;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.menuStrip1);
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
            this.splitContainer1.Size = new System.Drawing.Size(985, 727);
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
            this.splitContainer2.Size = new System.Drawing.Size(198, 727);
            this.splitContainer2.SplitterDistance = 160;
            this.splitContainer2.TabIndex = 1;
            // 
            // pmMiniMap
            // 
            this.pmMiniMap.BackColor = System.Drawing.Color.Black;
            this.pmMiniMap.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pmMiniMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pmMiniMap.Location = new System.Drawing.Point(0, 0);
            this.pmMiniMap.Name = "pmMiniMap";
            this.pmMiniMap.Size = new System.Drawing.Size(198, 160);
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
            this.tabControl1.Size = new System.Drawing.Size(198, 563);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.tilePalette);
            this.tabPage1.Location = new System.Drawing.Point(4, 20);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(190, 539);
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
            this.tilePalette.Size = new System.Drawing.Size(184, 533);
            this.tilePalette.TabIndex = 1;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.actorPalette);
            this.tabPage2.Location = new System.Drawing.Point(4, 20);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(190, 539);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Actors";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // actorPalette
            // 
            this.actorPalette.AutoScroll = true;
            this.actorPalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.actorPalette.Dock = System.Windows.Forms.DockStyle.Fill;
            this.actorPalette.Location = new System.Drawing.Point(3, 3);
            this.actorPalette.Name = "actorPalette";
            this.actorPalette.Size = new System.Drawing.Size(184, 533);
            this.actorPalette.TabIndex = 2;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.resourcePalette);
            this.tabPage3.Location = new System.Drawing.Point(4, 20);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(190, 539);
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
            this.resourcePalette.Size = new System.Drawing.Size(190, 539);
            this.resourcePalette.TabIndex = 3;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.mapToolStripMenuItem,
            this.toolsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(985, 24);
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
            this.exotToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("newToolStripMenuItem.Image")));
            this.newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.newToolStripMenuItem.Text = "&New...";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.NewClicked);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(122, 6);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripMenuItem.Image")));
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenClicked);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripMenuItem.Image")));
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.SaveClicked);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsClicked);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(122, 6);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cCRedAlertMapToolStripMenuItem,
            this.bitmapToolStripMenuItem});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(125, 22);
            this.toolStripMenuItem1.Text = "&Import";
            // 
            // cCRedAlertMapToolStripMenuItem
            // 
            this.cCRedAlertMapToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("cCRedAlertMapToolStripMenuItem.Image")));
            this.cCRedAlertMapToolStripMenuItem.Name = "cCRedAlertMapToolStripMenuItem";
            this.cCRedAlertMapToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.cCRedAlertMapToolStripMenuItem.Text = "&C&&C / Red Alert Map...";
            this.cCRedAlertMapToolStripMenuItem.Click += new System.EventHandler(this.ImportLegacyMapClicked);
            // 
            // bitmapToolStripMenuItem
            // 
            this.bitmapToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("bitmapToolStripMenuItem.Image")));
            this.bitmapToolStripMenuItem.Name = "bitmapToolStripMenuItem";
            this.bitmapToolStripMenuItem.Size = new System.Drawing.Size(185, 22);
            this.bitmapToolStripMenuItem.Text = "&Bitmap...";
            this.bitmapToolStripMenuItem.Visible = false;
            // 
            // mnuExport
            // 
            this.mnuExport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuMinimapToPNG});
            this.mnuExport.Name = "mnuExport";
            this.mnuExport.Size = new System.Drawing.Size(152, 22);
            this.mnuExport.Text = "&Export";
            // 
            // mnuMinimapToPNG
            // 
            this.mnuMinimapToPNG.Image = ((System.Drawing.Image)(resources.GetObject("mnuMinimapToPNG.Image")));
            this.mnuMinimapToPNG.Name = "mnuMinimapToPNG";
            this.mnuMinimapToPNG.Size = new System.Drawing.Size(152, 22);
            this.mnuMinimapToPNG.Text = "Minimap to PNG";
            this.mnuMinimapToPNG.Click += new System.EventHandler(this.mnuMinimapToPNG_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(122, 6);
            // 
            // exotToolStripMenuItem
            // 
            this.exotToolStripMenuItem.Name = "exotToolStripMenuItem";
            this.exotToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.exotToolStripMenuItem.Text = "E&xit";
            this.exotToolStripMenuItem.Click += new System.EventHandler(this.CloseClicked);
            // 
            // mapToolStripMenuItem
            // 
            this.mapToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.propertiesToolStripMenuItem,
            this.resizeToolStripMenuItem,
            this.toolStripSeparator4,
            this.spawnpointsToolStripMenuItem});
            this.mapToolStripMenuItem.Name = "mapToolStripMenuItem";
            this.mapToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.mapToolStripMenuItem.Text = "&Map";
            // 
            // propertiesToolStripMenuItem
            // 
            this.propertiesToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("propertiesToolStripMenuItem.Image")));
            this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
            this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.propertiesToolStripMenuItem.Text = "&Properties...";
            this.propertiesToolStripMenuItem.Click += new System.EventHandler(this.PropertiesClicked);
            // 
            // resizeToolStripMenuItem
            // 
            this.resizeToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("resizeToolStripMenuItem.Image")));
            this.resizeToolStripMenuItem.Name = "resizeToolStripMenuItem";
            this.resizeToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.resizeToolStripMenuItem.Text = "&Resize...";
            this.resizeToolStripMenuItem.Click += new System.EventHandler(this.ResizeClicked);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(132, 6);
            // 
            // spawnpointsToolStripMenuItem
            // 
            this.spawnpointsToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("spawnpointsToolStripMenuItem.Image")));
            this.spawnpointsToolStripMenuItem.Name = "spawnpointsToolStripMenuItem";
            this.spawnpointsToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.spawnpointsToolStripMenuItem.Text = "&Spawnpoints";
            this.spawnpointsToolStripMenuItem.Click += new System.EventHandler(this.SpawnPointsClicked);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.layersFloaterToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            this.toolsToolStripMenuItem.Visible = false;
            // 
            // layersFloaterToolStripMenuItem
            // 
            this.layersFloaterToolStripMenuItem.Name = "layersFloaterToolStripMenuItem";
            this.layersFloaterToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.layersFloaterToolStripMenuItem.Text = "Layers floater";
            this.layersFloaterToolStripMenuItem.Click += new System.EventHandler(this.layersFloaterToolStripMenuItem_Click);
            // 
            // tt
            // 
            this.tt.ShowAlways = true;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 751);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(985, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // surface1
            // 
            this.surface1.BackColor = System.Drawing.Color.Black;
            this.surface1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.surface1.Location = new System.Drawing.Point(0, 0);
            this.surface1.Name = "surface1";
            this.surface1.Size = new System.Drawing.Size(783, 727);
            this.surface1.TabIndex = 5;
            this.surface1.Text = "surface1";
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "*.png";
            this.saveFileDialog.Filter = "PNG Image (*.png)|";
            this.saveFileDialog.Title = "Export minimap to PNG";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(985, 773);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStripContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "OpenRA Editor";
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyUp);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
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
            this.tabPage3.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.ToolTip tt;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.FlowLayoutPanel tilePalette;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.FlowLayoutPanel actorPalette;
		private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.FlowLayoutPanel resourcePalette;
		private Surface surface1;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem exotToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem cCRedAlertMapToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem bitmapToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem mapToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem propertiesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem resizeToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem spawnpointsToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem layersFloaterToolStripMenuItem;
        private System.Windows.Forms.PictureBox pmMiniMap;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ToolStripMenuItem mnuExport;
        private System.Windows.Forms.ToolStripMenuItem mnuMinimapToPNG;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;

	}
}

