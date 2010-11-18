namespace OpenRA.Launcher
{
	partial class Launcher
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
			System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Mods", -2, -2);
			System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Broken Mods");
			this.installButton = new System.Windows.Forms.Button();
			this.installModDialog = new System.Windows.Forms.OpenFileDialog();
			this.treeView = new System.Windows.Forms.TreeView();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.webBrowser = new System.Windows.Forms.WebBrowser();
			this.panel1 = new System.Windows.Forms.Panel();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// installButton
			// 
			this.installButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.installButton.Location = new System.Drawing.Point(12, 12);
			this.installButton.Name = "installButton";
			this.installButton.Size = new System.Drawing.Size(116, 23);
			this.installButton.TabIndex = 2;
			this.installButton.Text = "Install Mod...";
			this.installButton.UseVisualStyleBackColor = true;
			this.installButton.Click += new System.EventHandler(this.InstallMod);
			// 
			// installModDialog
			// 
			this.installModDialog.Filter = "Zip files|*.zip";
			this.installModDialog.RestoreDirectory = true;
			// 
			// treeView
			// 
			this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeView.Location = new System.Drawing.Point(0, 0);
			this.treeView.Name = "treeView";
			treeNode1.ImageIndex = -2;
			treeNode1.Name = "ModsNode";
			treeNode1.SelectedImageIndex = -2;
			treeNode1.Text = "Mods";
			treeNode2.Name = "BrokenModsNode";
			treeNode2.Text = "Broken Mods";
			this.treeView.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2});
			this.treeView.ShowLines = false;
			this.treeView.Size = new System.Drawing.Size(146, 465);
			this.treeView.TabIndex = 3;
			this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.treeView);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.webBrowser);
			this.splitContainer1.Size = new System.Drawing.Size(671, 465);
			this.splitContainer1.SplitterDistance = 146;
			this.splitContainer1.TabIndex = 4;
			// 
			// webBrowser
			// 
			this.webBrowser.AllowWebBrowserDrop = false;
			this.webBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.webBrowser.Location = new System.Drawing.Point(0, 0);
			this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
			this.webBrowser.Name = "webBrowser";
			this.webBrowser.Size = new System.Drawing.Size(521, 465);
			this.webBrowser.TabIndex = 0;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.installButton);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point(0, 465);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(671, 47);
			this.panel1.TabIndex = 5;
			// 
			// Launcher
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(671, 512);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.panel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Launcher";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "OpenRA Launcher";
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button installButton;
		private System.Windows.Forms.OpenFileDialog installModDialog;
		private System.Windows.Forms.TreeView treeView;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.WebBrowser webBrowser;
	}
}