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
            this.colTheater = new System.Windows.Forms.ColumnHeader();
            this.MapIconsList = new System.Windows.Forms.ImageList(this.components);
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.lblNew = new System.Windows.Forms.Label();
            this.txtNew = new System.Windows.Forms.TextBox();
            this.txtPath = new System.Windows.Forms.TextBox();
            this.colTitle = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            // 
            // MapList
            // 
            this.MapList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colMapName,
            this.colTitle,
            this.colTheater});
            this.MapList.Dock = System.Windows.Forms.DockStyle.Top;
            this.MapList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.MapList.LargeImageList = this.MapIconsList;
            this.MapList.Location = new System.Drawing.Point(0, 0);
            this.MapList.MultiSelect = false;
            this.MapList.Name = "MapList";
            this.MapList.Size = new System.Drawing.Size(472, 252);
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
            this.colMapName.Width = 170;
            // 
            // colTheater
            // 
            this.colTheater.Text = "Theater";
            this.colTheater.Width = 110;
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
            this.btnCancel.Location = new System.Drawing.Point(385, 258);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(304, 258);
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
            this.lblNew.Location = new System.Drawing.Point(12, 263);
            this.lblNew.Name = "lblNew";
            this.lblNew.Size = new System.Drawing.Size(70, 13);
            this.lblNew.TabIndex = 3;
            this.lblNew.Text = "New name:";
            this.lblNew.Visible = false;
            // 
            // txtNew
            // 
            this.txtNew.Location = new System.Drawing.Point(88, 260);
            this.txtNew.Name = "txtNew";
            this.txtNew.Size = new System.Drawing.Size(210, 20);
            this.txtNew.TabIndex = 4;
            this.txtNew.Visible = false;
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(12, 307);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(448, 20);
            this.txtPath.TabIndex = 5;
            this.txtPath.Visible = false;
            // 
            // colTitle
            // 
            this.colTitle.Text = "Title";
            this.colTitle.Width = 170;
            // 
            // MapSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(472, 303);
            this.Controls.Add(this.txtPath);
            this.Controls.Add(this.txtNew);
            this.Controls.Add(this.lblNew);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.MapList);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MapSelect";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select map";
            this.Load += new System.EventHandler(this.MapSelect_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ColumnHeader colMapName;
        private System.Windows.Forms.ColumnHeader colTheater;
        private System.Windows.Forms.ImageList MapIconsList;
        public System.Windows.Forms.TextBox txtPath;
        public System.Windows.Forms.ListView MapList;
        public System.Windows.Forms.Button btnCancel;
        public System.Windows.Forms.Button btnOk;
        public System.Windows.Forms.Label lblNew;
        public System.Windows.Forms.TextBox txtNew;
        private System.Windows.Forms.ColumnHeader colTitle;
    }
}