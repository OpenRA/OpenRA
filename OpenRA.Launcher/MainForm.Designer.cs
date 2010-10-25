namespace OpenRA.Launcher
{
	partial class MainForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.launchButton = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.quitButton = new System.Windows.Forms.Button();
			this.configModsButton = new System.Windows.Forms.Button();
			this.configGameButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(51, 3);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(192, 64);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			// 
			// launchButton
			// 
			this.launchButton.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.launchButton.Location = new System.Drawing.Point(51, 97);
			this.launchButton.Name = "launchButton";
			this.launchButton.Size = new System.Drawing.Size(192, 50);
			this.launchButton.TabIndex = 1;
			this.launchButton.Text = "Launch OpenRA";
			this.launchButton.UseVisualStyleBackColor = true;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.pictureBox1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.launchButton, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.quitButton, 0, 5);
			this.tableLayoutPanel1.Controls.Add(this.configModsButton, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.configGameButton, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 6;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(294, 372);
			this.tableLayoutPanel1.TabIndex = 2;
			// 
			// quitButton
			// 
			this.quitButton.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.quitButton.Location = new System.Drawing.Point(51, 311);
			this.quitButton.Name = "quitButton";
			this.quitButton.Size = new System.Drawing.Size(192, 50);
			this.quitButton.TabIndex = 2;
			this.quitButton.Text = "Quit";
			this.quitButton.UseVisualStyleBackColor = true;
			// 
			// configModsButton
			// 
			this.configModsButton.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.configModsButton.Location = new System.Drawing.Point(51, 168);
			this.configModsButton.Name = "configModsButton";
			this.configModsButton.Size = new System.Drawing.Size(192, 50);
			this.configModsButton.TabIndex = 3;
			this.configModsButton.Text = "Configure Mods...";
			this.configModsButton.UseVisualStyleBackColor = true;
			this.configModsButton.Click += new System.EventHandler(this.ConfigureMods);
			// 
			// configGameButton
			// 
			this.configGameButton.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.configGameButton.Location = new System.Drawing.Point(51, 239);
			this.configGameButton.Name = "configGameButton";
			this.configGameButton.Size = new System.Drawing.Size(192, 50);
			this.configGameButton.TabIndex = 4;
			this.configGameButton.Text = "Configure Game...";
			this.configGameButton.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(3, 70);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(288, 17);
			this.label1.TabIndex = 5;
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(294, 372);
			this.Controls.Add(this.tableLayoutPanel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "OpenRA Launcher";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Button launchButton;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Button quitButton;
		private System.Windows.Forms.Button configModsButton;
		private System.Windows.Forms.Button configGameButton;
		private System.Windows.Forms.Label label1;
	}
}