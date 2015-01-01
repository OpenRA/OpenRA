namespace OpenRA.TilesetBuilder
{
	partial class FormBuilder
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormBuilder));
			this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
			this.panel1 = new System.Windows.Forms.Panel();
			this.ImageList = new System.Windows.Forms.ImageList(this.components);
			this.terrainTypes = new System.Windows.Forms.ToolStrip();
			this.toolStripLabel3 = new System.Windows.Forms.ToolStripLabel();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripLabel4 = new System.Windows.Forms.ToolStripLabel();
			this.txtTilesetName = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripLabel5 = new System.Windows.Forms.ToolStripLabel();
			this.txtID = new System.Windows.Forms.ToolStripTextBox();
			this.lblExt = new System.Windows.Forms.ToolStripLabel();
			this.txtExt = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripLabel6 = new System.Windows.Forms.ToolStripLabel();
			this.txtPal = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.toolStripButton15 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton14 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton16 = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
			this.surface1 = new OpenRA.TilesetBuilder.Surface();
			this.toolStripContainer1.ContentPanel.SuspendLayout();
			this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.terrainTypes.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStripContainer1
			// 
			// 
			// toolStripContainer1.ContentPanel
			// 
			this.toolStripContainer1.ContentPanel.Controls.Add(this.panel1);
			this.toolStripContainer1.ContentPanel.Controls.Add(this.terrainTypes);
			this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(908, 571);
			this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
			this.toolStripContainer1.Name = "toolStripContainer1";
			this.toolStripContainer1.Size = new System.Drawing.Size(908, 596);
			this.toolStripContainer1.TabIndex = 0;
			this.toolStripContainer1.Text = "toolStripContainer1";
			// 
			// toolStripContainer1.TopToolStripPanel
			// 
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
			this.toolStripContainer1.TopToolStripPanel.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			// 
			// panel1
			// 
			this.panel1.AutoScroll = true;
			this.panel1.BackColor = System.Drawing.Color.Black;
			this.panel1.Controls.Add(this.surface1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(0, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(788, 571);
			this.panel1.TabIndex = 3;
			// 
			// imageList
			// 
			this.ImageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
			this.ImageList.TransparentColor = System.Drawing.Color.Transparent;
			this.ImageList.Images.SetKeyName(0, "clear.png");
			this.ImageList.Images.SetKeyName(1, "water.png");
			this.ImageList.Images.SetKeyName(2, "road.png");
			this.ImageList.Images.SetKeyName(3, "rock.png");
			this.ImageList.Images.SetKeyName(4, "river.png");
			this.ImageList.Images.SetKeyName(5, "rough.png");
			this.ImageList.Images.SetKeyName(6, "wall.png");
			this.ImageList.Images.SetKeyName(7, "beach.png");
			this.ImageList.Images.SetKeyName(8, "tree.png");
			this.ImageList.Images.SetKeyName(9, "tiberium.png");
			// 
			// tsTerrainTypes
			// 
			this.terrainTypes.AutoSize = false;
			this.terrainTypes.Dock = System.Windows.Forms.DockStyle.Right;
			this.terrainTypes.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripLabel3,
			this.toolStripSeparator3,
			this.toolStripLabel4,
			this.txtTilesetName,
			this.toolStripLabel5,
			this.txtID,
			this.lblExt,
			this.txtExt,
			this.toolStripLabel6,
			this.txtPal,
			this.toolStripSeparator5,
			this.toolStripLabel2,
			this.toolStripSeparator4,
			this.toolStripButton1});
			this.terrainTypes.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
			this.terrainTypes.Location = new System.Drawing.Point(788, 0);
			this.terrainTypes.Name = "tsTerrainTypes";
			this.terrainTypes.Size = new System.Drawing.Size(120, 571);
			this.terrainTypes.TabIndex = 1;
			this.terrainTypes.Text = "toolStrip3";
			// 
			// toolStripLabel3
			// 
			this.toolStripLabel3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
			this.toolStripLabel3.Name = "toolStripLabel3";
			this.toolStripLabel3.Size = new System.Drawing.Size(118, 13);
			this.toolStripLabel3.Text = "Tileset setup";
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(118, 6);
			// 
			// toolStripLabel4
			// 
			this.toolStripLabel4.Name = "toolStripLabel4";
			this.toolStripLabel4.Size = new System.Drawing.Size(118, 13);
			this.toolStripLabel4.Text = "Tileset name:";
			// 
			// txtTilesetName
			// 
			this.txtTilesetName.MaxLength = 32;
			this.txtTilesetName.Name = "txtTilesetName";
			this.txtTilesetName.Size = new System.Drawing.Size(116, 21);
			this.txtTilesetName.Text = "Temperat";
			this.txtTilesetName.TextChanged += new System.EventHandler(this.TilesetNameChanged);
			// 
			// toolStripLabel5
			// 
			this.toolStripLabel5.Name = "toolStripLabel5";
			this.toolStripLabel5.Size = new System.Drawing.Size(118, 13);
			this.toolStripLabel5.Text = "Tileset ID:";
			// 
			// txtID
			// 
			this.txtID.Name = "txtID";
			this.txtID.ReadOnly = true;
			this.txtID.Size = new System.Drawing.Size(116, 21);
			this.txtID.Text = "TEMPERAT";
			// 
			// lblExt
			// 
			this.lblExt.Name = "lblExt";
			this.lblExt.Size = new System.Drawing.Size(118, 13);
			this.lblExt.Text = "Extensions:";
			// 
			// txtExt
			// 
			this.txtExt.Name = "txtExt";
			this.txtExt.ReadOnly = true;
			this.txtExt.Size = new System.Drawing.Size(116, 21);
			this.txtExt.Text = ".tem,.shp";
			// 
			// toolStripLabel6
			// 
			this.toolStripLabel6.Name = "toolStripLabel6";
			this.toolStripLabel6.Size = new System.Drawing.Size(118, 13);
			this.toolStripLabel6.Text = "Palette:";
			// 
			// txtPal
			// 
			this.txtPal.Name = "txtPal";
			this.txtPal.ReadOnly = true;
			this.txtPal.Size = new System.Drawing.Size(116, 21);
			this.txtPal.Text = "temperat.pal";
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(118, 6);
			// 
			// toolStripLabel2
			// 
			this.toolStripLabel2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
			this.toolStripLabel2.Name = "toolStripLabel2";
			this.toolStripLabel2.Size = new System.Drawing.Size(118, 13);
			this.toolStripLabel2.Text = "Terrain type";
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(118, 6);
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.Checked = true;
			this.toolStripButton1.CheckState = System.Windows.Forms.CheckState.Checked;
			this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(118, 20);
			this.toolStripButton1.Text = "Template Tool";
			this.toolStripButton1.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.toolStripButton1.Click += new System.EventHandler(this.TerrainTypeSelectorClicked);
			// 
			// toolStrip1
			// 
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripButton15,
			this.toolStripButton2,
			this.toolStripButton14,
			this.toolStripButton16,
			this.toolStripSeparator1,
			this.toolStripButton3});
			this.toolStrip1.Location = new System.Drawing.Point(3, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(275, 25);
			this.toolStrip1.TabIndex = 0;
			// 
			// toolStripButton15
			// 
			this.toolStripButton15.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton15.Image")));
			this.toolStripButton15.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton15.Name = "toolStripButton15";
			this.toolStripButton15.Size = new System.Drawing.Size(48, 22);
			this.toolStripButton15.Text = "New";
			this.toolStripButton15.ToolTipText = "Create new tileset";
			this.toolStripButton15.Click += new System.EventHandler(this.NewTilesetButton);
			// 
			// toolStripButton2
			// 
			this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
			this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton2.Name = "toolStripButton2";
			this.toolStripButton2.Size = new System.Drawing.Size(51, 22);
			this.toolStripButton2.Text = "Save";
			this.toolStripButton2.ToolTipText = "Save Template definitions to file (*.tsx)";
			this.toolStripButton2.Click += new System.EventHandler(this.SaveClicked);
			// 
			// toolStripButton14
			// 
			this.toolStripButton14.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton14.Image")));
			this.toolStripButton14.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton14.Name = "toolStripButton14";
			this.toolStripButton14.Size = new System.Drawing.Size(59, 22);
			this.toolStripButton14.Text = "Export";
			this.toolStripButton14.ToolTipText = "Export defined Templates to files";
			this.toolStripButton14.Click += new System.EventHandler(this.ExportClicked);
			// 
			// toolStripButton16
			// 
			this.toolStripButton16.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton14.Image")));
			this.toolStripButton16.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton16.Name = "toolStripButton16";
			this.toolStripButton16.Size = new System.Drawing.Size(65, 22);
			this.toolStripButton16.Text = "Dump";
			this.toolStripButton16.ToolTipText = "Dump Template ID to tile number mapping to console";
			this.toolStripButton16.Click += new System.EventHandler(this.Export2Clicked);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// toolStripButton3
			// 
			this.toolStripButton3.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton3.Image")));
			this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton3.Name = "toolStripButton3";
			this.toolStripButton3.Size = new System.Drawing.Size(70, 22);
			this.toolStripButton3.Text = "Overlays";
			this.toolStripButton3.ToolTipText = "Show/hide terrain type overlays";
			this.toolStripButton3.CheckOnClick = true;
			this.toolStripButton3.Click += new System.EventHandler(this.ShowOverlaysClicked);
			// 
			// surface1
			// 
			this.surface1.BackColor = System.Drawing.Color.Black;
			this.surface1.ImagesList = this.ImageList;
			this.surface1.Location = new System.Drawing.Point(0, 0);
			this.surface1.Name = "surface1";
			this.surface1.ShowTerrainTypes = this.toolStripButton3.Checked;
			this.surface1.Size = new System.Drawing.Size(653, 454);
			this.surface1.TabIndex = 2;
			this.surface1.Text = "surface1";
			// 
			// frmBuilder
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(908, 596);
			this.Controls.Add(this.toolStripContainer1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "frmBuilder";
			this.Text = "Tileset Builder 2";
			this.toolStripContainer1.ContentPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.PerformLayout();
			this.toolStripContainer1.ResumeLayout(false);
			this.toolStripContainer1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.terrainTypes.ResumeLayout(false);
			this.terrainTypes.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
		}

		#endregion

		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton toolStripButton2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton toolStripButton3;
		private System.Windows.Forms.ToolStripButton toolStripButton14;
		private System.Windows.Forms.ToolStripButton toolStripButton15;
		private System.Windows.Forms.ToolStripButton toolStripButton16;
		public System.Windows.Forms.ImageList ImageList;
		private System.Windows.Forms.ToolStrip terrainTypes;
		private System.Windows.Forms.Panel panel1;
		private Surface surface1;
		private System.Windows.Forms.ToolStripLabel toolStripLabel2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.ToolStripLabel toolStripLabel3;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripLabel toolStripLabel4;
		private System.Windows.Forms.ToolStripTextBox txtTilesetName;
		private System.Windows.Forms.ToolStripLabel toolStripLabel5;
		private System.Windows.Forms.ToolStripTextBox txtID;
		private System.Windows.Forms.ToolStripLabel lblExt;
		private System.Windows.Forms.ToolStripTextBox txtExt;
		private System.Windows.Forms.ToolStripLabel toolStripLabel6;
		private System.Windows.Forms.ToolStripTextBox txtPal;
	}
}