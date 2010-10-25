using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

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
				currentMods = new string[] { };
			else
				currentMods = response.Response.Split(',');

			label1.Text = string.Format("Current Mods: {0}", currentMods.Length > 0 ? string.Join(",", currentMods) : "ra");
		}

		private void ConfigureMods(object sender, EventArgs e)
		{
			var d = new ConfigureModsDialog(currentMods);
			d.ShowDialog();
		}
	}
}
