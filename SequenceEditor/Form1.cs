using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenRa.FileFormats;

namespace SequenceEditor
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			Text += " - " + Program.UnitName;
		}

		void toolStripButton1_Click(object sender, EventArgs e)
		{
			var shp = GetTextForm.GetString( "Add SHP...", "" );
			if (shp == null) return;
			Program.Shps.Add(shp, Program.LoadAndResolve(shp));
		}
	}
}
