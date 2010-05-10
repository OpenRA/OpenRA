namespace OpenRA.Editor
{
	partial class PropertiesDialog
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
			this.button2 = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.title = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.desc = new System.Windows.Forms.TextBox();
			this.selectable = new System.Windows.Forms.CheckBox();
			this.label3 = new System.Windows.Forms.Label();
			this.author = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// button2
			// 
			this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button2.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.button2.Location = new System.Drawing.Point(196, 193);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 14;
			this.button2.Text = "OK";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(277, 193);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 15;
			this.button1.Text = "Cancel";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 50);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(27, 13);
			this.label1.TabIndex = 16;
			this.label1.Text = "Title";
			// 
			// title
			// 
			this.title.Location = new System.Drawing.Point(66, 47);
			this.title.Name = "title";
			this.title.Size = new System.Drawing.Size(286, 20);
			this.title.TabIndex = 17;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 76);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(32, 13);
			this.label2.TabIndex = 16;
			this.label2.Text = "Desc";
			// 
			// desc
			// 
			this.desc.Location = new System.Drawing.Point(66, 73);
			this.desc.Name = "desc";
			this.desc.Size = new System.Drawing.Size(286, 20);
			this.desc.TabIndex = 17;
			// 
			// selectable
			// 
			this.selectable.AutoSize = true;
			this.selectable.Location = new System.Drawing.Point(118, 138);
			this.selectable.Name = "selectable";
			this.selectable.Size = new System.Drawing.Size(130, 17);
			this.selectable.TabIndex = 18;
			this.selectable.Text = "Show in Map Chooser";
			this.selectable.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 102);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(38, 13);
			this.label3.TabIndex = 16;
			this.label3.Text = "Author";
			// 
			// author
			// 
			this.author.Location = new System.Drawing.Point(66, 99);
			this.author.Name = "author";
			this.author.Size = new System.Drawing.Size(286, 20);
			this.author.TabIndex = 17;
			// 
			// PropertiesDialog
			// 
			this.AcceptButton = this.button2;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button1;
			this.ClientSize = new System.Drawing.Size(370, 228);
			this.Controls.Add(this.selectable);
			this.Controls.Add(this.author);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.desc);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.title);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "PropertiesDialog";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "PropertiesDialog";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.TextBox title;
		private System.Windows.Forms.Label label2;
		public System.Windows.Forms.TextBox desc;
		public System.Windows.Forms.CheckBox selectable;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.TextBox author;
	}
}