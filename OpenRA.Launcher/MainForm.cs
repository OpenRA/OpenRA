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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OpenRA.Launcher
{
	public partial class MainForm : Form
	{
		string configPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + Path.DirectorySeparatorChar + "OpenRA";
		string[] currentMods;
		public MainForm()
		{
			InitializeComponent();
			quitButton.Click += (o, e) => { Application.Exit(); };
			var response = UtilityProgram.Call("--settings-value", configPath, "Game.Mods");

			if (response.IsError)
				currentMods = new string[] { "ra" };
			else
				currentMods = response.Response.Split(',');

			UpdateModLabel();
		}

		void UpdateModLabel()
		{
			label1.Text = string.Format("Current Mods: {0}", currentMods.Length > 0 ? string.Join(",", currentMods) : "ra");
		}

		void ConfigureMods(object sender, EventArgs e)
		{
			var d = new ConfigureModsDialog(currentMods);
			if (d.ShowDialog() != DialogResult.OK)
				return;

			currentMods = d.ActiveMods.ToArray();

			UpdateModLabel();
		}

		void LaunchGame(object sender, EventArgs e)
		{
			string[] officialMods = { "ra", "cnc" };

			bool allOk = true;
			foreach(string s in officialMods)
				if (currentMods.Contains(s))
					allOk = CheckAndInstallPackages(s);

			if (!allOk) return;

			Process p = new Process();
			p.StartInfo.FileName = "OpenRA.Game.exe";
			p.StartInfo.Arguments = "Game.Mods=" + string.Join(",", currentMods);
			p.Start();
		}

		bool CheckAndInstallPackages(string mod)
		{
			string packageDir = "mods" + Path.DirectorySeparatorChar + mod + Path.DirectorySeparatorChar + "packages";
			if (Directory.Exists(packageDir) &&
				Directory.GetFiles(packageDir, "*.mix").Length > 0) return true;
			var dialog = new InstallPackagesDialog(mod);
			if (dialog.ShowDialog() != DialogResult.OK) return false;
			return true;
		}
	}
}
