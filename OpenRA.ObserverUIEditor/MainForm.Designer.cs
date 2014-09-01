namespace OpenRA.ObserverUIEditor {
	partial class MainForm {
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
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.selectModToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.miModCnc = new System.Windows.Forms.ToolStripMenuItem();
			this.miModRA = new System.Windows.Forms.ToolStripMenuItem();
			this.miModD2k = new System.Windows.Forms.ToolStripMenuItem();
			this.miModTS = new System.Windows.Forms.ToolStripMenuItem();
			this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.grpGeneral = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.cbDefaultView = new System.Windows.Forms.ComboBox();
			this.grpTableEdit = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.cbBackground = new System.Windows.Forms.ComboBox();
			this.edTitle = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.picExample = new System.Windows.Forms.PictureBox();
			this.btnDown = new System.Windows.Forms.Button();
			this.btnUp = new System.Windows.Forms.Button();
			this.btnEditColumn = new System.Windows.Forms.Button();
			this.btnDeleteColumn = new System.Windows.Forms.Button();
			this.edRowSpacing = new System.Windows.Forms.NumericUpDown();
			this.edColumnSpacing = new System.Windows.Forms.NumericUpDown();
			this.edY = new System.Windows.Forms.NumericUpDown();
			this.edX = new System.Windows.Forms.NumericUpDown();
			this.edRowHeight = new System.Windows.Forms.NumericUpDown();
			this.label2 = new System.Windows.Forms.Label();
			this.lblRowSpacing = new System.Windows.Forms.Label();
			this.lblRowHeight = new System.Windows.Forms.Label();
			this.lblY = new System.Windows.Forms.Label();
			this.lblX = new System.Windows.Forms.Label();
			this.lstColumns = new System.Windows.Forms.ListBox();
			this.btnAddColumn = new System.Windows.Forms.Button();
			this.cbConfigureView = new System.Windows.Forms.ComboBox();
			this.dlgColor = new System.Windows.Forms.ColorDialog();
			this.grpBarsSettings = new System.Windows.Forms.GroupBox();
			this.edSpacing = new System.Windows.Forms.NumericUpDown();
			this.lblSpacing = new System.Windows.Forms.Label();
			this.edThickness = new System.Windows.Forms.NumericUpDown();
			this.lblThickness = new System.Windows.Forms.Label();
			this.lblEvenColor = new System.Windows.Forms.Label();
			this.lblOddColor = new System.Windows.Forms.Label();
			this.shapeContainer1 = new Microsoft.VisualBasic.PowerPacks.ShapeContainer();
			this.btnEvenColor = new Microsoft.VisualBasic.PowerPacks.RectangleShape();
			this.btnOddColor = new Microsoft.VisualBasic.PowerPacks.RectangleShape();
			this.label4 = new System.Windows.Forms.Label();
			this.menuStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.grpGeneral.SuspendLayout();
			this.grpTableEdit.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.picExample)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.edRowSpacing)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.edColumnSpacing)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.edY)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.edX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.edRowHeight)).BeginInit();
			this.grpBarsSettings.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.edSpacing)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.edThickness)).BeginInit();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(963, 24);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectModToolStripMenuItem,
            this.reloadToolStripMenuItem,
            this.saveToolStripMenuItem1,
            this.toolStripMenuItem1,
            this.exitToolStripMenuItem});
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.saveToolStripMenuItem.Text = "&File";
			// 
			// selectModToolStripMenuItem
			// 
			this.selectModToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miModCnc,
            this.miModRA,
            this.miModD2k,
            this.miModTS});
			this.selectModToolStripMenuItem.Name = "selectModToolStripMenuItem";
			this.selectModToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
			this.selectModToolStripMenuItem.Text = "Mod";
			// 
			// miModCnc
			// 
			this.miModCnc.Name = "miModCnc";
			this.miModCnc.Size = new System.Drawing.Size(100, 22);
			this.miModCnc.Text = "C&&C";
			this.miModCnc.Click += new System.EventHandler(this.miModCnc_Click);
			// 
			// miModRA
			// 
			this.miModRA.Name = "miModRA";
			this.miModRA.Size = new System.Drawing.Size(100, 22);
			this.miModRA.Text = "RA";
			this.miModRA.Click += new System.EventHandler(this.miModRA_Click);
			// 
			// miModD2k
			// 
			this.miModD2k.Name = "miModD2k";
			this.miModD2k.Size = new System.Drawing.Size(100, 22);
			this.miModD2k.Text = "D2k";
			this.miModD2k.Click += new System.EventHandler(this.miModD2k_Click);
			// 
			// miModTS
			// 
			this.miModTS.Name = "miModTS";
			this.miModTS.Size = new System.Drawing.Size(100, 22);
			this.miModTS.Text = "TS";
			this.miModTS.Click += new System.EventHandler(this.miModTS_Click);
			// 
			// reloadToolStripMenuItem
			// 
			this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
			this.reloadToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
			this.reloadToolStripMenuItem.Text = "Reload";
			this.reloadToolStripMenuItem.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
			// 
			// saveToolStripMenuItem1
			// 
			this.saveToolStripMenuItem1.Name = "saveToolStripMenuItem1";
			this.saveToolStripMenuItem1.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.saveToolStripMenuItem1.Size = new System.Drawing.Size(138, 22);
			this.saveToolStripMenuItem1.Text = "&Save";
			this.saveToolStripMenuItem1.Click += new System.EventHandler(this.saveToolStripMenuItem1_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(135, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 24);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.grpGeneral);
			this.splitContainer1.Panel1.Padding = new System.Windows.Forms.Padding(3, 3, 3, 0);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.label4);
			this.splitContainer1.Panel2.Controls.Add(this.grpTableEdit);
			this.splitContainer1.Panel2.Controls.Add(this.cbConfigureView);
			this.splitContainer1.Size = new System.Drawing.Size(963, 477);
			this.splitContainer1.SplitterDistance = 82;
			this.splitContainer1.TabIndex = 2;
			// 
			// grpGeneral
			// 
			this.grpGeneral.Controls.Add(this.label1);
			this.grpGeneral.Controls.Add(this.cbDefaultView);
			this.grpGeneral.Dock = System.Windows.Forms.DockStyle.Top;
			this.grpGeneral.Location = new System.Drawing.Point(3, 3);
			this.grpGeneral.Name = "grpGeneral";
			this.grpGeneral.Size = new System.Drawing.Size(957, 77);
			this.grpGeneral.TabIndex = 0;
			this.grpGeneral.TabStop = false;
			this.grpGeneral.Text = "General";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 21);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(66, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Default view";
			// 
			// cbDefaultView
			// 
			this.cbDefaultView.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbDefaultView.FormattingEnabled = true;
			this.cbDefaultView.Location = new System.Drawing.Point(9, 41);
			this.cbDefaultView.Name = "cbDefaultView";
			this.cbDefaultView.Size = new System.Drawing.Size(199, 21);
			this.cbDefaultView.TabIndex = 1;
			this.cbDefaultView.SelectedIndexChanged += new System.EventHandler(this.cbDefaultView_SelectedIndexChanged);
			// 
			// grpTableEdit
			// 
			this.grpTableEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.grpTableEdit.Controls.Add(this.grpBarsSettings);
			this.grpTableEdit.Controls.Add(this.label3);
			this.grpTableEdit.Controls.Add(this.cbBackground);
			this.grpTableEdit.Controls.Add(this.edTitle);
			this.grpTableEdit.Controls.Add(this.label5);
			this.grpTableEdit.Controls.Add(this.picExample);
			this.grpTableEdit.Controls.Add(this.btnDown);
			this.grpTableEdit.Controls.Add(this.btnUp);
			this.grpTableEdit.Controls.Add(this.btnEditColumn);
			this.grpTableEdit.Controls.Add(this.btnDeleteColumn);
			this.grpTableEdit.Controls.Add(this.edRowSpacing);
			this.grpTableEdit.Controls.Add(this.edColumnSpacing);
			this.grpTableEdit.Controls.Add(this.edY);
			this.grpTableEdit.Controls.Add(this.edX);
			this.grpTableEdit.Controls.Add(this.edRowHeight);
			this.grpTableEdit.Controls.Add(this.label2);
			this.grpTableEdit.Controls.Add(this.lblRowSpacing);
			this.grpTableEdit.Controls.Add(this.lblRowHeight);
			this.grpTableEdit.Controls.Add(this.lblY);
			this.grpTableEdit.Controls.Add(this.lblX);
			this.grpTableEdit.Controls.Add(this.lstColumns);
			this.grpTableEdit.Controls.Add(this.btnAddColumn);
			this.grpTableEdit.Location = new System.Drawing.Point(3, 42);
			this.grpTableEdit.Name = "grpTableEdit";
			this.grpTableEdit.Size = new System.Drawing.Size(957, 346);
			this.grpTableEdit.TabIndex = 1;
			this.grpTableEdit.TabStop = false;
			this.grpTableEdit.Text = "Table columns";
			this.grpTableEdit.Visible = false;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(239, 28);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(65, 13);
			this.label3.TabIndex = 20;
			this.label3.Text = "Background";
			// 
			// cbBackground
			// 
			this.cbBackground.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbBackground.FormattingEnabled = true;
			this.cbBackground.Location = new System.Drawing.Point(324, 25);
			this.cbBackground.Name = "cbBackground";
			this.cbBackground.Size = new System.Drawing.Size(205, 21);
			this.cbBackground.TabIndex = 19;
			this.cbBackground.SelectedIndexChanged += new System.EventHandler(this.cbBackground_SelectedIndexChanged);
			// 
			// edTitle
			// 
			this.edTitle.Location = new System.Drawing.Point(51, 26);
			this.edTitle.Name = "edTitle";
			this.edTitle.Size = new System.Drawing.Size(136, 20);
			this.edTitle.TabIndex = 18;
			this.edTitle.TextChanged += new System.EventHandler(this.edTitle_TextChanged);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(13, 29);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(27, 13);
			this.label5.TabIndex = 17;
			this.label5.Text = "Title";
			// 
			// picExample
			// 
			this.picExample.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.picExample.BackColor = System.Drawing.Color.DarkGray;
			this.picExample.InitialImage = null;
			this.picExample.Location = new System.Drawing.Point(12, 247);
			this.picExample.Name = "picExample";
			this.picExample.Size = new System.Drawing.Size(933, 89);
			this.picExample.TabIndex = 16;
			this.picExample.TabStop = false;
			this.picExample.Paint += new System.Windows.Forms.PaintEventHandler(this.picExample_Paint);
			// 
			// btnDown
			// 
			this.btnDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDown.Location = new System.Drawing.Point(535, 213);
			this.btnDown.Name = "btnDown";
			this.btnDown.Size = new System.Drawing.Size(88, 23);
			this.btnDown.TabIndex = 15;
			this.btnDown.Text = "Down";
			this.btnDown.UseVisualStyleBackColor = true;
			this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
			// 
			// btnUp
			// 
			this.btnUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnUp.Location = new System.Drawing.Point(535, 184);
			this.btnUp.Name = "btnUp";
			this.btnUp.Size = new System.Drawing.Size(88, 23);
			this.btnUp.TabIndex = 14;
			this.btnUp.Text = "Up";
			this.btnUp.UseVisualStyleBackColor = true;
			this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
			// 
			// btnEditColumn
			// 
			this.btnEditColumn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnEditColumn.Location = new System.Drawing.Point(535, 155);
			this.btnEditColumn.Name = "btnEditColumn";
			this.btnEditColumn.Size = new System.Drawing.Size(88, 23);
			this.btnEditColumn.TabIndex = 13;
			this.btnEditColumn.Text = "Edit column";
			this.btnEditColumn.UseVisualStyleBackColor = true;
			this.btnEditColumn.Click += new System.EventHandler(this.btnEditColumn_Click);
			// 
			// btnDeleteColumn
			// 
			this.btnDeleteColumn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDeleteColumn.Location = new System.Drawing.Point(535, 126);
			this.btnDeleteColumn.Name = "btnDeleteColumn";
			this.btnDeleteColumn.Size = new System.Drawing.Size(88, 23);
			this.btnDeleteColumn.TabIndex = 12;
			this.btnDeleteColumn.Text = "Del column";
			this.btnDeleteColumn.UseVisualStyleBackColor = true;
			this.btnDeleteColumn.Click += new System.EventHandler(this.btnDeleteColumn_Click);
			// 
			// edRowSpacing
			// 
			this.edRowSpacing.Location = new System.Drawing.Point(415, 74);
			this.edRowSpacing.Name = "edRowSpacing";
			this.edRowSpacing.Size = new System.Drawing.Size(53, 20);
			this.edRowSpacing.TabIndex = 11;
			this.edRowSpacing.ValueChanged += new System.EventHandler(this.edRowSpacing_ValueChanged);
			// 
			// edColumnSpacing
			// 
			this.edColumnSpacing.Location = new System.Drawing.Point(415, 50);
			this.edColumnSpacing.Name = "edColumnSpacing";
			this.edColumnSpacing.Size = new System.Drawing.Size(53, 20);
			this.edColumnSpacing.TabIndex = 10;
			this.edColumnSpacing.ValueChanged += new System.EventHandler(this.edColumnSpacing_ValueChanged);
			// 
			// edY
			// 
			this.edY.Location = new System.Drawing.Point(51, 74);
			this.edY.Maximum = new decimal(new int[] {
            1920,
            0,
            0,
            0});
			this.edY.Name = "edY";
			this.edY.Size = new System.Drawing.Size(53, 20);
			this.edY.TabIndex = 9;
			this.edY.ValueChanged += new System.EventHandler(this.edY_ValueChanged);
			// 
			// edX
			// 
			this.edX.Location = new System.Drawing.Point(51, 50);
			this.edX.Maximum = new decimal(new int[] {
            1920,
            0,
            0,
            0});
			this.edX.Name = "edX";
			this.edX.Size = new System.Drawing.Size(53, 20);
			this.edX.TabIndex = 8;
			this.edX.ValueChanged += new System.EventHandler(this.edX_ValueChanged);
			// 
			// edRowHeight
			// 
			this.edRowHeight.Location = new System.Drawing.Point(238, 50);
			this.edRowHeight.Name = "edRowHeight";
			this.edRowHeight.Size = new System.Drawing.Size(53, 20);
			this.edRowHeight.TabIndex = 7;
			this.edRowHeight.ValueChanged += new System.EventHandler(this.edRowHeight_ValueChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(325, 52);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(84, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "Column Spacing";
			// 
			// lblRowSpacing
			// 
			this.lblRowSpacing.AutoSize = true;
			this.lblRowSpacing.Location = new System.Drawing.Point(325, 76);
			this.lblRowSpacing.Name = "lblRowSpacing";
			this.lblRowSpacing.Size = new System.Drawing.Size(71, 13);
			this.lblRowSpacing.TabIndex = 5;
			this.lblRowSpacing.Text = "Row Spacing";
			// 
			// lblRowHeight
			// 
			this.lblRowHeight.AutoSize = true;
			this.lblRowHeight.Location = new System.Drawing.Point(172, 52);
			this.lblRowHeight.Name = "lblRowHeight";
			this.lblRowHeight.Size = new System.Drawing.Size(60, 13);
			this.lblRowHeight.TabIndex = 4;
			this.lblRowHeight.Text = "RowHeight";
			// 
			// lblY
			// 
			this.lblY.AutoSize = true;
			this.lblY.Location = new System.Drawing.Point(13, 76);
			this.lblY.Name = "lblY";
			this.lblY.Size = new System.Drawing.Size(14, 13);
			this.lblY.TabIndex = 3;
			this.lblY.Text = "Y";
			// 
			// lblX
			// 
			this.lblX.AutoSize = true;
			this.lblX.Location = new System.Drawing.Point(13, 52);
			this.lblX.Name = "lblX";
			this.lblX.Size = new System.Drawing.Size(14, 13);
			this.lblX.TabIndex = 2;
			this.lblX.Text = "X";
			// 
			// lstColumns
			// 
			this.lstColumns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lstColumns.FormattingEnabled = true;
			this.lstColumns.Location = new System.Drawing.Point(12, 102);
			this.lstColumns.Name = "lstColumns";
			this.lstColumns.Size = new System.Drawing.Size(517, 134);
			this.lstColumns.TabIndex = 1;
			this.lstColumns.DoubleClick += new System.EventHandler(this.lstColumns_DoubleClick);
			// 
			// btnAddColumn
			// 
			this.btnAddColumn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAddColumn.Location = new System.Drawing.Point(535, 97);
			this.btnAddColumn.Name = "btnAddColumn";
			this.btnAddColumn.Size = new System.Drawing.Size(88, 23);
			this.btnAddColumn.TabIndex = 0;
			this.btnAddColumn.Text = "Add column";
			this.btnAddColumn.UseVisualStyleBackColor = true;
			this.btnAddColumn.Click += new System.EventHandler(this.btnAddColumn_Click);
			// 
			// cbConfigureView
			// 
			this.cbConfigureView.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbConfigureView.FormattingEnabled = true;
			this.cbConfigureView.Location = new System.Drawing.Point(86, 12);
			this.cbConfigureView.Name = "cbConfigureView";
			this.cbConfigureView.Size = new System.Drawing.Size(173, 21);
			this.cbConfigureView.TabIndex = 0;
			this.cbConfigureView.SelectedIndexChanged += new System.EventHandler(this.cbConfigureView_SelectedIndexChanged);
			// 
			// dlgColor
			// 
			this.dlgColor.FullOpen = true;
			// 
			// grpBarsSettings
			// 
			this.grpBarsSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.grpBarsSettings.Controls.Add(this.edSpacing);
			this.grpBarsSettings.Controls.Add(this.lblSpacing);
			this.grpBarsSettings.Controls.Add(this.edThickness);
			this.grpBarsSettings.Controls.Add(this.lblThickness);
			this.grpBarsSettings.Controls.Add(this.lblEvenColor);
			this.grpBarsSettings.Controls.Add(this.lblOddColor);
			this.grpBarsSettings.Controls.Add(this.shapeContainer1);
			this.grpBarsSettings.Location = new System.Drawing.Point(744, 25);
			this.grpBarsSettings.Name = "grpBarsSettings";
			this.grpBarsSettings.Size = new System.Drawing.Size(201, 150);
			this.grpBarsSettings.TabIndex = 21;
			this.grpBarsSettings.TabStop = false;
			this.grpBarsSettings.Text = "Bars Team settings";
			// 
			// edSpacing
			// 
			this.edSpacing.Location = new System.Drawing.Point(117, 110);
			this.edSpacing.Name = "edSpacing";
			this.edSpacing.Size = new System.Drawing.Size(60, 20);
			this.edSpacing.TabIndex = 6;
			// 
			// lblSpacing
			// 
			this.lblSpacing.AutoSize = true;
			this.lblSpacing.Location = new System.Drawing.Point(12, 112);
			this.lblSpacing.Name = "lblSpacing";
			this.lblSpacing.Size = new System.Drawing.Size(46, 13);
			this.lblSpacing.TabIndex = 5;
			this.lblSpacing.Text = "Spacing";
			// 
			// edThickness
			// 
			this.edThickness.Location = new System.Drawing.Point(117, 81);
			this.edThickness.Name = "edThickness";
			this.edThickness.Size = new System.Drawing.Size(60, 20);
			this.edThickness.TabIndex = 4;
			// 
			// lblThickness
			// 
			this.lblThickness.AutoSize = true;
			this.lblThickness.Location = new System.Drawing.Point(12, 83);
			this.lblThickness.Name = "lblThickness";
			this.lblThickness.Size = new System.Drawing.Size(56, 13);
			this.lblThickness.TabIndex = 3;
			this.lblThickness.Text = "Thickness";
			// 
			// lblEvenColor
			// 
			this.lblEvenColor.AutoSize = true;
			this.lblEvenColor.Location = new System.Drawing.Point(12, 51);
			this.lblEvenColor.Name = "lblEvenColor";
			this.lblEvenColor.Size = new System.Drawing.Size(58, 13);
			this.lblEvenColor.TabIndex = 1;
			this.lblEvenColor.Text = "Even color";
			// 
			// lblOddColor
			// 
			this.lblOddColor.AutoSize = true;
			this.lblOddColor.Location = new System.Drawing.Point(12, 28);
			this.lblOddColor.Name = "lblOddColor";
			this.lblOddColor.Size = new System.Drawing.Size(53, 13);
			this.lblOddColor.TabIndex = 0;
			this.lblOddColor.Text = "Odd color";
			// 
			// shapeContainer1
			// 
			this.shapeContainer1.Location = new System.Drawing.Point(3, 16);
			this.shapeContainer1.Margin = new System.Windows.Forms.Padding(0);
			this.shapeContainer1.Name = "shapeContainer1";
			this.shapeContainer1.Shapes.AddRange(new Microsoft.VisualBasic.PowerPacks.Shape[] {
            this.btnEvenColor,
            this.btnOddColor});
			this.shapeContainer1.Size = new System.Drawing.Size(195, 131);
			this.shapeContainer1.TabIndex = 2;
			this.shapeContainer1.TabStop = false;
			// 
			// btnEvenColor
			// 
			this.btnEvenColor.FillStyle = Microsoft.VisualBasic.PowerPacks.FillStyle.Solid;
			this.btnEvenColor.Location = new System.Drawing.Point(114, 34);
			this.btnEvenColor.Name = "btnEvenColor";
			this.btnEvenColor.Size = new System.Drawing.Size(25, 15);
			// 
			// btnOddColor
			// 
			this.btnOddColor.FillStyle = Microsoft.VisualBasic.PowerPacks.FillStyle.Solid;
			this.btnOddColor.Location = new System.Drawing.Point(114, 11);
			this.btnOddColor.Name = "btnOddColor";
			this.btnOddColor.Size = new System.Drawing.Size(25, 15);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(9, 15);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(52, 13);
			this.label4.TabIndex = 2;
			this.label4.Text = "Configure";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(963, 501);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "MainForm";
			this.Text = "Observer UI editor";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
			this.Load += new System.EventHandler(this.Form1_Load);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.grpGeneral.ResumeLayout(false);
			this.grpGeneral.PerformLayout();
			this.grpTableEdit.ResumeLayout(false);
			this.grpTableEdit.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.picExample)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.edRowSpacing)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.edColumnSpacing)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.edY)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.edX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.edRowHeight)).EndInit();
			this.grpBarsSettings.ResumeLayout(false);
			this.grpBarsSettings.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.edSpacing)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.edThickness)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.GroupBox grpGeneral;
		private System.Windows.Forms.ComboBox cbConfigureView;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox cbDefaultView;
		private System.Windows.Forms.GroupBox grpTableEdit;
		private System.Windows.Forms.ListBox lstColumns;
		private System.Windows.Forms.Button btnAddColumn;
		private System.Windows.Forms.Label lblY;
		private System.Windows.Forms.Label lblX;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label lblRowSpacing;
		private System.Windows.Forms.Label lblRowHeight;
		private System.Windows.Forms.NumericUpDown edRowSpacing;
		private System.Windows.Forms.NumericUpDown edColumnSpacing;
		private System.Windows.Forms.NumericUpDown edY;
		private System.Windows.Forms.NumericUpDown edX;
		private System.Windows.Forms.NumericUpDown edRowHeight;
		private System.Windows.Forms.Button btnDeleteColumn;
		private System.Windows.Forms.Button btnEditColumn;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.Button btnDown;
		private System.Windows.Forms.Button btnUp;
		private System.Windows.Forms.ToolStripMenuItem reloadToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.PictureBox picExample;
		private System.Windows.Forms.ColorDialog dlgColor;
		private System.Windows.Forms.TextBox edTitle;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ToolStripMenuItem selectModToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem miModCnc;
		private System.Windows.Forms.ToolStripMenuItem miModRA;
		private System.Windows.Forms.ToolStripMenuItem miModD2k;
		private System.Windows.Forms.ToolStripMenuItem miModTS;
		private System.Windows.Forms.ComboBox cbBackground;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.GroupBox grpBarsSettings;
		private System.Windows.Forms.NumericUpDown edSpacing;
		private System.Windows.Forms.Label lblSpacing;
		private System.Windows.Forms.NumericUpDown edThickness;
		private System.Windows.Forms.Label lblThickness;
		private System.Windows.Forms.Label lblEvenColor;
		private System.Windows.Forms.Label lblOddColor;
		private Microsoft.VisualBasic.PowerPacks.ShapeContainer shapeContainer1;
		private Microsoft.VisualBasic.PowerPacks.RectangleShape btnEvenColor;
		private Microsoft.VisualBasic.PowerPacks.RectangleShape btnOddColor;
		private System.Windows.Forms.Label label4;
	}
}

