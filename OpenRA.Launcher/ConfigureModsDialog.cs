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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OpenRA.Launcher
{
	public partial class ConfigureModsDialog : Form
	{
		List<string> activeMods;

		public List<string> ActiveMods
		{
			get { return activeMods; }
		}

		Dictionary<string, Mod> allMods;
		public ConfigureModsDialog(string[] activeMods)
		{
			InitializeComponent();

			Util.UacShield(installButton);

			this.activeMods = new List<string>(activeMods);

			listView1.Items.AddRange(activeMods.Select(x => new ListViewItem(x)).ToArray());
			
			RefreshMods();
		}

		Mod GetMetadata(string mod)
		{
			string responseString;
			using (var response = UtilityProgram.Call("-i", mod))
			{
				responseString = response.ReadToEnd();
			}

			if (Util.IsError(ref responseString)) return null;
			string[] lines = responseString.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			string title = "", version = "", author = "", description = "", requires = "";
			bool standalone = false;
			foreach (string line in lines)
			{
				string s = line.Trim(' ', '\r', '\n');
				int i = s.IndexOf(':');
				if (i + 2 > s.Length) continue;
				string value = s.Substring(i + 2);
				switch (s.Substring(0, i))
				{
					case "Title":
						title = value;
						break;
					case "Version":
						version = value;
						break;
					case "Author":
						author = value;
						break;
					case "Description":
						description = value;
						break;
					case "Requires":
						requires = value;
						break;
					case "Standalone":
						standalone = bool.Parse(value);
						break;
					default:
						break;
				}
			}

			return new Mod(title, version, author, description, requires, standalone);
		}

		void RefreshMods()
		{
			string responseString;
			using (var response = UtilityProgram.Call("--list-mods"))
			{
				responseString = response.ReadToEnd();
			}

			string[] mods;
			if (!Util.IsError(ref responseString))
				mods = responseString.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
			else
				throw new Exception(string.Format("Could not list mods: {0}", responseString));

			allMods = mods.ToDictionary(x => x, x => GetMetadata(x));

			RefreshModTree(treeView1, allMods.Keys.ToArray());
		}

		private void InstallMod(object sender, EventArgs e)
		{
			if (installModDialog.ShowDialog() != DialogResult.OK) return;
			using (var response = UtilityProgram.CallWithAdmin("--install-mod", installModDialog.FileName))
			{
				string s = response.ReadToEnd();
			}

			RefreshMods();
		}

		void RefreshModTree(TreeView treeView, string[] modList)
		{
			treeView.Nodes.Clear();
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

		void TreeViewSelect(object sender, TreeViewEventArgs e)
		{
			SelectMod(e.Node.Text);
		}

		void ListViewSelect(object sender, EventArgs e)
		{
			if (listView1.SelectedItems.Count > 0)
				SelectMod(listView1.SelectedItems[0].Text);
			else
				SelectMod("");
		}
		
		void treeView1_Enter(object sender, EventArgs e)
		{
			if (treeView1.SelectedNode != null)
				SelectMod(treeView1.SelectedNode.Text);
			else
				SelectMod("");
		}

		void SelectMod(string mod)
		{
			if (!allMods.ContainsKey(mod))
				propertyGrid1.SelectedObject = null;
			else
				propertyGrid1.SelectedObject = allMods[mod];
		}

		void ActivateMod(object sender, EventArgs e)
		{
			if (treeView1.SelectedNode == null) return;
			string mod = treeView1.SelectedNode.Text;
			if (!allMods.ContainsKey(mod)) return;
			if (activeMods.Contains(mod)) return;
			
			Mod m = allMods[mod];
			Stack<string> toAdd = new Stack<string>();
			toAdd.Push(mod);
			while (!string.IsNullOrEmpty(m.Requires))
			{
				string r = m.Requires;
				if (!allMods.ContainsKey(r))
				{
					MessageBox.Show(string.Format("A requirement for the mod \"{0}\" is missing. Please install \"{1}\" or the game may not run properly.", mod, r));
					return;
				}
				if (!activeMods.Contains(r))
					toAdd.Push(r);
				mod = r;
				m = allMods[mod];
			}

			while (toAdd.Count > 0)
				activeMods.Add(toAdd.Pop());

			listView1.Items.Clear();
			listView1.Items.AddRange(activeMods.Select(x => new ListViewItem(x)).ToArray());
		}

		void DeactivateMod(object sender, EventArgs e)
		{
			if (listView1.SelectedItems.Count < 1) return;
			string mod = listView1.SelectedItems[0].Text;
			List<string> toRemove = new List<string>();
			
			Stack<string> nodes = new Stack<string>();
			nodes.Push(mod);
			string currentNode;
			while (nodes.Count > 0)
			{
				currentNode = nodes.Pop();
				toRemove.Add(currentNode);
				foreach (string n in activeMods.Where(x => allMods[x].Requires == currentNode))
					nodes.Push(n);
			}

			listView1.SuspendLayout();
			foreach (string s in toRemove)
			{
				listView1.Items.Remove(listView1.Items.OfType<ListViewItem>().Where(x => x.Text == s).SingleOrDefault());
				activeMods.Remove(s);
			}
			listView1.ResumeLayout();
		}
	}

	class Mod
	{
		public string Title { get; private set; }
		public string Version { get; private set; }
		public string Author { get; private set; }
		public string Description { get; private set; }
		public string Requires { get; private set; }
		public bool Standalone { get; private set; }

		public Mod(string title, string version, string author, string description, string requires, bool standalone)
		{
			Title = title;
			Version = version;
			Author = author;
			Description = description;
			Requires = requires;
			Standalone = standalone;
		}
	}
}
