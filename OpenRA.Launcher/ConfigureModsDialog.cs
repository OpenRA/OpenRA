using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OpenRA.Launcher
{
	public partial class ConfigureModsDialog : Form
	{
		string[] currentMods;
		string[] allMods;
		public ConfigureModsDialog(string[] currentMods)
		{
			InitializeComponent();

			Util.UacShield(installButton);

			this.currentMods = currentMods;

			var response = UtilityProgram.Call("--list-mods");
			if (!response.IsError)
				allMods = response.ResponseLines;
			else
				throw new Exception(string.Format("Could not list mods: {0}", response.Response));
		}

		private void InstallMod(object sender, EventArgs e)
		{
			if (installModDialog.ShowDialog() != DialogResult.OK) return;
			UtilityProgram.CallWithAdmin("--install-mods", "foo.zip");
		}
	}
}
