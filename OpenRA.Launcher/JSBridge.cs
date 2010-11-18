using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace OpenRA.Launcher
{
	public class JSBridge
	{
		Dictionary<string, Mod> allMods;

		public JSBridge(Dictionary<string, Mod> allMods)
		{
			this.allMods = allMods;
		}

		public bool fileExistsInMod(string file, string mod)
		{
			return File.Exists(string.Format("mods{0}{1}{0}{2}", Path.DirectorySeparatorChar, mod, file));
		}

		public void log(string message)
		{
			Console.WriteLine("js: " + message);
		}

		public void launchMod(string mod)
		{
			string m = mod;
			List<string> modList = new List<string>();
			modList.Add(m);
			if (!allMods.ContainsKey(m)) System.Windows.Forms.MessageBox.Show("allMods does not contain " + m);
			while (!string.IsNullOrEmpty(allMods[m].Requires))
			{
				m = allMods[m].Requires;
				modList.Add(m);
			}

			Process p = new Process();
			p.StartInfo.FileName = "OpenRA.Game.exe";
			p.StartInfo.Arguments = "Game.Mods=" + string.Join(",", modList.ToArray());
			p.Start();
		}
	}
}
