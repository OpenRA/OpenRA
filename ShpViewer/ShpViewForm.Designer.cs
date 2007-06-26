namespace ShpViewer
{
	partial class ShpViewForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.mapViewControl1 = new ShpViewer.MapViewControl();
			this.SuspendLayout();
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
					| System.Windows.Forms.AnchorStyles.Left )
					| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.flowLayoutPanel1.Location = new System.Drawing.Point( 1, 1 );
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size( 292, 273 );
			this.flowLayoutPanel1.TabIndex = 0;
			// 
			// mapViewControl1
			// 
			this.mapViewControl1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
					| System.Windows.Forms.AnchorStyles.Left )
					| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.mapViewControl1.BackColor = System.Drawing.Color.Black;
			this.mapViewControl1.Location = new System.Drawing.Point( 0, 0 );
			this.mapViewControl1.Name = "mapViewControl1";
			this.mapViewControl1.Size = new System.Drawing.Size( 293, 274 );
			this.mapViewControl1.TabIndex = 1;
			this.mapViewControl1.Text = "mapViewControl1";
			this.mapViewControl1.Visible = false;
			// 
			// ShpViewForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 292, 273 );
			this.Controls.Add( this.mapViewControl1 );
			this.Controls.Add( this.flowLayoutPanel1 );
			this.Name = "ShpViewForm";
			this.Text = "Form1";
			this.ResumeLayout( false );

		}

		#endregion

		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private MapViewControl mapViewControl1;
	}
}

