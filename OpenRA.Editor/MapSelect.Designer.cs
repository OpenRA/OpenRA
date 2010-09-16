namespace OpenRA.Editor
{
    partial class MapSelect
    {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapSelect));
            this.MapList = new System.Windows.Forms.ListView();
            this.colMapName = new System.Windows.Forms.ColumnHeader("(отсутствует)");
            this.MapIconsList = new System.Windows.Forms.ImageList(this.components);
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.lblNew = new System.Windows.Forms.Label();
            this.txtNew = new System.Windows.Forms.TextBox();
            this.pbMinimap = new System.Windows.Forms.PictureBox();
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.lblPathOut = new System.Windows.Forms.Label();
            this.lblPath = new System.Windows.Forms.Label();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.lblMapList = new System.Windows.Forms.Label();
            this.txtDesc = new System.Windows.Forms.TextBox();
            this.lblDesc = new System.Windows.Forms.Label();
            this.txtTheater = new System.Windows.Forms.TextBox();
            this.lblTheater = new System.Windows.Forms.Label();
            this.txtAuthor = new System.Windows.Forms.TextBox();
            this.lblAuthor = new System.Windows.Forms.Label();
            this.txtTitle = new System.Windows.Forms.TextBox();
            this.lblMapName = new System.Windows.Forms.Label();
            this.lblMinimap = new System.Windows.Forms.Label();
            this.txtPathOut = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbMinimap)).BeginInit();
            this.pnlBottom.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // MapList
            // 
            this.MapList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.MapList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colMapName});
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
            // 
            // colMapName
            // 
            this.colMapName.Text = "Map name";
            this.colMapName.Width = 240;
            // 
            // MapIconsList
            // 
            this.MapIconsList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("MapIconsList.ImageStream")));
            this.MapIconsList.TransparentColor = System.Drawing.Color.Transparent;
            this.MapIconsList.Images.SetKeyName(0, "mapicon");
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(407, 35);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(326, 35);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 2;
            this.btnOk.Text = "Open";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // lblNew
            // 
            this.lblNew.AutoSize = true;
            this.lblNew.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblNew.Location = new System.Drawing.Point(12, 40);
            this.lblNew.Name = "lblNew";
            this.lblNew.Size = new System.Drawing.Size(69, 13);
            this.lblNew.TabIndex = 3;
            this.lblNew.Text = "Map name:";
            // 
            // txtNew
            // 
            this.txtNew.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtNew.Location = new System.Drawing.Point(88, 37);
            this.txtNew.Name = "txtNew";
            this.txtNew.ReadOnly = true;
            this.txtNew.Size = new System.Drawing.Size(232, 20);
            this.txtNew.TabIndex = 4;
            // 
            // pbMinimap
            // 
            this.pbMinimap.BackColor = System.Drawing.Color.Black;
            this.pbMinimap.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pbMinimap.Location = new System.Drawing.Point(32, 25);
            this.pbMinimap.Name = "pbMinimap";
            this.pbMinimap.Size = new System.Drawing.Size(124, 124);
            this.pbMinimap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbMinimap.TabIndex = 5;
            this.pbMinimap.TabStop = false;
            // 
            // pnlBottom
            // 
            this.pnlBottom.Controls.Add(this.txtPathOut);
            this.pnlBottom.Controls.Add(this.lblPathOut);
            this.pnlBottom.Controls.Add(this.lblPath);
            this.pnlBottom.Controls.Add(this.btnCancel);
            this.pnlBottom.Controls.Add(this.btnOk);
            this.pnlBottom.Controls.Add(this.txtNew);
            this.pnlBottom.Controls.Add(this.lblNew);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 332);
            this.pnlBottom.MaximumSize = new System.Drawing.Size(0, 70);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(494, 70);
            this.pnlBottom.TabIndex = 6;
            // 
            // lblPathOut
            // 
            this.lblPathOut.AutoSize = true;
            this.lblPathOut.Location = new System.Drawing.Point(55, 13);
            this.lblPathOut.Name = "lblPathOut";
            this.lblPathOut.Size = new System.Drawing.Size(0, 13);
            this.lblPathOut.TabIndex = 6;
            // 
            // lblPath
            // 
            this.lblPath.AutoSize = true;
            this.lblPath.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblPath.Location = new System.Drawing.Point(12, 13);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(37, 13);
            this.lblPath.TabIndex = 5;
            this.lblPath.Text = "Path:";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.lblMapList);
            this.splitContainer1.Panel1.Controls.Add(this.MapList);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.txtDesc);
            this.splitContainer1.Panel2.Controls.Add(this.lblDesc);
            this.splitContainer1.Panel2.Controls.Add(this.txtTheater);
            this.splitContainer1.Panel2.Controls.Add(this.lblTheater);
            this.splitContainer1.Panel2.Controls.Add(this.txtAuthor);
            this.splitContainer1.Panel2.Controls.Add(this.lblAuthor);
            this.splitContainer1.Panel2.Controls.Add(this.txtTitle);
            this.splitContainer1.Panel2.Controls.Add(this.lblMapName);
            this.splitContainer1.Panel2.Controls.Add(this.lblMinimap);
            this.splitContainer1.Panel2.Controls.Add(this.pbMinimap);
            this.splitContainer1.Size = new System.Drawing.Size(494, 332);
            this.splitContainer1.SplitterDistance = 300;
            this.splitContainer1.TabIndex = 7;
            // 
            // lblMapList
            // 
            this.lblMapList.AutoSize = true;
            this.lblMapList.Location = new System.Drawing.Point(12, 9);
            this.lblMapList.Name = "lblMapList";
            this.lblMapList.Size = new System.Drawing.Size(81, 13);
            this.lblMapList.TabIndex = 1;
            this.lblMapList.Text = "Available maps:";
            // 
            // txtDesc
            // 
            this.txtDesc.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.txtDesc.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDesc.Location = new System.Drawing.Point(16, 289);
            this.txtDesc.Name = "txtDesc";
            this.txtDesc.ReadOnly = true;
            this.txtDesc.Size = new System.Drawing.Size(162, 20);
            this.txtDesc.TabIndex = 14;
            // 
            // lblDesc
            // 
            this.lblDesc.AutoSize = true;
            this.lblDesc.Location = new System.Drawing.Point(13, 273);
            this.lblDesc.Name = "lblDesc";
            this.lblDesc.Size = new System.Drawing.Size(63, 13);
            this.lblDesc.TabIndex = 13;
            this.lblDesc.Text = "Description:";
            // 
            // txtTheater
            // 
            this.txtTheater.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.txtTheater.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTheater.Location = new System.Drawing.Point(16, 252);
            this.txtTheater.Name = "txtTheater";
            this.txtTheater.ReadOnly = true;
            this.txtTheater.Size = new System.Drawing.Size(162, 20);
            this.txtTheater.TabIndex = 12;
            // 
            // lblTheater
            // 
            this.lblTheater.AutoSize = true;
            this.lblTheater.Location = new System.Drawing.Point(13, 236);
            this.lblTheater.Name = "lblTheater";
            this.lblTheater.Size = new System.Drawing.Size(47, 13);
            this.lblTheater.TabIndex = 11;
            this.lblTheater.Text = "Theater:";
            // 
            // txtAuthor
            // 
            this.txtAuthor.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.txtAuthor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtAuthor.Location = new System.Drawing.Point(16, 214);
            this.txtAuthor.Name = "txtAuthor";
            this.txtAuthor.ReadOnly = true;
            this.txtAuthor.Size = new System.Drawing.Size(162, 20);
            this.txtAuthor.TabIndex = 10;
            // 
            // lblAuthor
            // 
            this.lblAuthor.AutoSize = true;
            this.lblAuthor.Location = new System.Drawing.Point(13, 198);
            this.lblAuthor.Name = "lblAuthor";
            this.lblAuthor.Size = new System.Drawing.Size(41, 13);
            this.lblAuthor.TabIndex = 9;
            this.lblAuthor.Text = "Author:";
            // 
            // txtTitle
            // 
            this.txtTitle.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.txtTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTitle.Location = new System.Drawing.Point(16, 177);
            this.txtTitle.Name = "txtTitle";
            this.txtTitle.ReadOnly = true;
            this.txtTitle.Size = new System.Drawing.Size(162, 20);
            this.txtTitle.TabIndex = 8;
            // 
            // lblMapName
            // 
            this.lblMapName.AutoSize = true;
            this.lblMapName.Location = new System.Drawing.Point(13, 161);
            this.lblMapName.Name = "lblMapName";
            this.lblMapName.Size = new System.Drawing.Size(30, 13);
            this.lblMapName.TabIndex = 7;
            this.lblMapName.Text = "Title:";
            // 
            // lblMinimap
            // 
            this.lblMinimap.AutoSize = true;
            this.lblMinimap.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblMinimap.Location = new System.Drawing.Point(29, 9);
            this.lblMinimap.Name = "lblMinimap";
            this.lblMinimap.Size = new System.Drawing.Size(71, 13);
            this.lblMinimap.TabIndex = 6;
            this.lblMinimap.Text = "Map preview:";
            // 
            // txtPathOut
            // 
            this.txtPathOut.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPathOut.Location = new System.Drawing.Point(55, 10);
            this.txtPathOut.Name = "txtPathOut";
            this.txtPathOut.ReadOnly = true;
            this.txtPathOut.Size = new System.Drawing.Size(265, 20);
            this.txtPathOut.TabIndex = 7;
            this.txtPathOut.TextChanged += new System.EventHandler(this.txtPathOut_TextChanged);
            // 
            // MapSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(494, 402);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.pnlBottom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MapSelect";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select map";
            this.Load += new System.EventHandler(this.MapSelect_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbMinimap)).EndInit();
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.ListView MapList;
        public System.Windows.Forms.Button btnCancel;
        public System.Windows.Forms.Button btnOk;
        public System.Windows.Forms.Label lblNew;
        public System.Windows.Forms.TextBox txtNew;
        public System.Windows.Forms.ColumnHeader colMapName;
        public System.Windows.Forms.ImageList MapIconsList;
        public System.Windows.Forms.PictureBox pbMinimap;
        public System.Windows.Forms.Panel pnlBottom;
        public System.Windows.Forms.SplitContainer splitContainer1;
        public System.Windows.Forms.Label lblMinimap;
        public System.Windows.Forms.TextBox txtTheater;
        public System.Windows.Forms.Label lblTheater;
        public System.Windows.Forms.TextBox txtAuthor;
        public System.Windows.Forms.Label lblAuthor;
        public System.Windows.Forms.TextBox txtTitle;
        public System.Windows.Forms.Label lblMapName;
        public System.Windows.Forms.TextBox txtDesc;
        public System.Windows.Forms.Label lblDesc;
        public System.Windows.Forms.Label lblMapList;
        public System.Windows.Forms.Label lblPathOut;
        public System.Windows.Forms.Label lblPath;
        public System.Windows.Forms.TextBox txtPathOut;
    }
}