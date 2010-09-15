namespace OpenRA.Editor
{
    partial class PaletteBox
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
            this.LayerBox = new System.Windows.Forms.ComboBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.pbtilePalette = new System.Windows.Forms.FlowLayoutPanel();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.pbactorPalette = new System.Windows.Forms.FlowLayoutPanel();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.pbresourcePalette = new System.Windows.Forms.FlowLayoutPanel();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // LayerBox
            // 
            this.LayerBox.Dock = System.Windows.Forms.DockStyle.Top;
            this.LayerBox.FormattingEnabled = true;
            this.LayerBox.Location = new System.Drawing.Point(0, 0);
            this.LayerBox.Name = "LayerBox";
            this.LayerBox.Size = new System.Drawing.Size(194, 21);
            this.LayerBox.TabIndex = 0;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 21);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.tabControl1.Multiline = true;
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(194, 357);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.pbtilePalette);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(186, 331);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Templates";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // pbtilePalette
            // 
            this.pbtilePalette.AutoScroll = true;
            this.pbtilePalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pbtilePalette.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbtilePalette.Location = new System.Drawing.Point(3, 3);
            this.pbtilePalette.Name = "pbtilePalette";
            this.pbtilePalette.Size = new System.Drawing.Size(180, 325);
            this.pbtilePalette.TabIndex = 1;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.pbactorPalette);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(186, 331);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Actors";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // pbactorPalette
            // 
            this.pbactorPalette.AutoScroll = true;
            this.pbactorPalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pbactorPalette.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbactorPalette.Location = new System.Drawing.Point(3, 3);
            this.pbactorPalette.Name = "pbactorPalette";
            this.pbactorPalette.Size = new System.Drawing.Size(180, 325);
            this.pbactorPalette.TabIndex = 2;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.pbresourcePalette);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(186, 331);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Resources";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // pbresourcePalette
            // 
            this.pbresourcePalette.AutoScroll = true;
            this.pbresourcePalette.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pbresourcePalette.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbresourcePalette.Location = new System.Drawing.Point(0, 0);
            this.pbresourcePalette.Name = "pbresourcePalette";
            this.pbresourcePalette.Size = new System.Drawing.Size(186, 331);
            this.pbresourcePalette.TabIndex = 3;
            // 
            // PaletteBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(194, 378);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.LayerBox);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PaletteBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Palette Box";
            this.TopMost = true;
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.ComboBox LayerBox;
        public System.Windows.Forms.TabControl tabControl1;
        public System.Windows.Forms.TabPage tabPage1;
        public System.Windows.Forms.FlowLayoutPanel pbtilePalette;
        public System.Windows.Forms.TabPage tabPage2;
        public System.Windows.Forms.FlowLayoutPanel pbactorPalette;
        public System.Windows.Forms.TabPage tabPage3;
        public System.Windows.Forms.FlowLayoutPanel pbresourcePalette;

    }
}