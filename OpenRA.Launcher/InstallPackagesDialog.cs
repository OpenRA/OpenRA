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
using System.IO;
using System.Windows.Forms;

namespace OpenRA.Launcher
{
	public partial class InstallPackagesDialog : Form
	{
		string mod;
		public InstallPackagesDialog(string mod)
		{
			InitializeComponent();
			Util.UacShield(button2);
			this.mod = mod;
			if (mod == "cnc") radioButton2.Enabled = false;
			label1.Text = string.Format("In order to play OpenRA with the mod \"{0}\", you must install the original game files. " + 
				"These can either be downloaded or for some mods be installed from a CD copy of the original game. " +
				"Please select your choice below and click continue", mod);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			UtilityProgramResponse response = null;
			if (radioButton1.Checked)
				response = UtilityProgram.CallWithAdmin("--download-packages", mod);

			if (radioButton2.Checked)
			{
				if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
					response = UtilityProgram.CallWithAdmin(string.Format("--install-{0}-packages", mod), 
						folderBrowserDialog1.SelectedPath + Path.DirectorySeparatorChar);
			}

			if (response.IsError)
				DialogResult = DialogResult.No;
			else
				DialogResult = DialogResult.OK;

			Close();
		}
	}
}
