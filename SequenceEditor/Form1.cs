#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Windows.Forms;

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
			Program.LoadAndResolve(shp);
		}

		void toolStripButton2_Click(object sender, EventArgs e)
		{
			Program.Save();
		}
	}
}
