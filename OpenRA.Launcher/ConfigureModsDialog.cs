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
	struct Mod
	{
		public string Title;
		public string Version;
		public string Author;
		public string Description;
		public string Requires;
		public bool Standalone;
	}

	public partial class ConfigureModsDialog : Form
	{
		string[] activeMods;
		Dictionary<string, Mod> allMods;
		public ConfigureModsDialog(string[] activeMods)
		{
			InitializeComponent();

			Util.UacShield(installButton);

			this.activeMods = activeMods;

			RefreshMods();
		}

		Mod GetMetadata(string mod)
		{
			var response = UtilityProgram.Call("-i", mod);
			Mod m = new Mod();
			if (response.IsError) return m;
			string[] lines = response.ResponseLines;


			foreach (string line in lines)
			{
				string s = line.Trim(' ', '\r', '\n');
				int i = s.IndexOf(':');
				if (i + 2 > s.Length) continue;
				string value = s.Substring(i + 2);
				switch (s.Substring(0, i))
				{
					case "Title":
						m.Title = value;
						break;
					case "Version":
						m.Version = value;
						break;
					case "Author":
						m.Author = value;
						break;
					case "Description":
						m.Description = value;
						break;
					case "Requires":
						m.Requires = value;
						break;
					case "Standalone":
						m.Standalone = bool.Parse(value);
						break;
					default:
						break;
				}
			}

			return m;
		}

		void RefreshMods()
		{
			var response = UtilityProgram.Call("--list-mods");
			string[] mods;
			if (!response.IsError)
				mods = response.ResponseLines;
			else
				throw new Exception(string.Format("Could not list mods: {0}", response.Response));

			allMods = mods.ToDictionary(x => x, x => GetMetadata(x));

			RefreshModTree(treeView1, allMods.Keys.ToArray());
		}

		private void InstallMod(object sender, EventArgs e)
		{
			if (installModDialog.ShowDialog() != DialogResult.OK) return;
			UtilityProgram.CallWithAdmin("--install-mods", installModDialog.FileName);
			RefreshModTree(treeView1, allMods.Keys.ToArray());
		}

		void RefreshModTree(TreeView treeView, string[] modList)
		{
			Dictionary<string, TreeNode> nodes;
			nodes = modList.Where(x => allMods[x].Standalone).ToDictionary(x => x, x => new TreeNode(x));
			string[] rootMods = modList.Where(x => allMods[x].Standalone).ToArray();
			string[] remaining = modList.Except(nodes.Keys).ToArray();
			
			while (remaining.Length > 0)
			{
				bool progress = false;
				List<string> toRemove = new List<string>();
				foreach (string s in remaining)
				{
					var n = new TreeNode(s);
					if (allMods[s].Requires == null) continue;
					if (!nodes.ContainsKey(allMods[s].Requires)) continue;
					nodes[allMods[s].Requires].Nodes.Add(n);
					nodes.Add(s, n);
					toRemove.Add(s);
					progress = true;
				}

				if (!progress)
					break;
				remaining = remaining.Except(toRemove).ToArray();
			}

			foreach (string s in rootMods)
				treeView.Nodes.Add(nodes[s]);

			if (remaining.Length > 0)
			{
				var n = new TreeNode("<Unspecified Dependency>");
				n.ForeColor = SystemColors.GrayText;
				var m = new TreeNode("<Missing Dependency>");
				m.ForeColor = SystemColors.GrayText;

				foreach (var s in remaining)
				{
					if (allMods[s].Requires == null)
						n.Nodes.Add(new TreeNode(s) { ForeColor = SystemColors.GrayText });
					else if (!nodes.ContainsKey(allMods[s].Requires))
						m.Nodes.Add(new TreeNode(s) { ForeColor = SystemColors.GrayText });
				}

				treeView.Nodes.Add(n);
				treeView.Nodes.Add(m);
			}

			treeView.Invalidate();
		}
	}
}
