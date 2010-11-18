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
using System.IO.Pipes;
using System.IO;

namespace OpenRA.Launcher
{
	public partial class Launcher : Form
	{
		Dictionary<string, Mod> allMods;
		public Launcher()
		{
			InitializeComponent();

			Util.UacShield(installButton);

			//treeView.Nodes["ModsNode"].ImageIndex = 1;
			//treeView.Nodes["ModsNode"].SelectedImageIndex = 1;

			RefreshMods();
			webBrowser.ObjectForScripting = new JSBridge(allMods);
		}

		Mod GetMetadata(string mod)
		{
			string responseString;
			using (var response = UtilityProgram.Call("-i", mod))
			{
				responseString = response.ReadToEnd();
			}

			if (Util.IsError(ref responseString)) return null;
			string[] lines = responseString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < lines.Length; i++)
				lines[i] = lines[i].Trim('\r');

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
				mods = responseString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			else
				throw new Exception(string.Format("Could not list mods: {0}", responseString));
			
			for (int i = 0; i < mods.Length; i++)
				mods[i] = mods[i].Trim('\r');

			allMods = mods.ToDictionary(x => x, x => GetMetadata(x));

			RefreshModTree(treeView, allMods.Keys.ToArray());
		}

		private void InstallMod(object sender, EventArgs e)
		{
			if (installModDialog.ShowDialog() != DialogResult.OK) return;
			var p = UtilityProgram.CallWithAdmin("--install-mod", installModDialog.FileName);
			var pipe = new NamedPipeClientStream(".", "OpenRA.Utility", PipeDirection.In);
			pipe.Connect();

			p.WaitForExit();

			using (var response = new StreamReader(pipe))
			{
				string s = response.ReadToEnd();
			}

			RefreshMods();
		}

		void RefreshModTree(TreeView treeView, string[] modList)
		{
			treeView.Nodes["ModsNode"].Nodes.Clear();
			Dictionary<string, TreeNode> nodes;
			nodes = modList.Where(x => allMods[x].Standalone).ToDictionary(x => x, 
				x => new TreeNode(allMods[x].Title) { Name = x });
			string[] rootMods = modList.Where(x => allMods[x].Standalone).ToArray();
			Stack<string> remaining = new Stack<string>(modList.Except(nodes.Keys));

			bool progress = true;
			while (remaining.Count > 0 && progress)
			{
				progress = false;
				string s = remaining.Pop();
				var n = new TreeNode(allMods[s].Title) { Name = s };
				if (allMods[s].Requires == null) { remaining.Push(s); continue; }
				if (!nodes.ContainsKey(allMods[s].Requires)) { remaining.Push(s); continue; }
				nodes[allMods[s].Requires].Nodes.Add(n);
				nodes.Add(s, n);
				progress = true;
			}

			foreach (string s in rootMods)
				treeView.Nodes["ModsNode"].Nodes.Add(nodes[s]);

			if (remaining.Count > 0)
			{
				var unspecified = new TreeNode("<Unspecified Dependency>") { ForeColor = SystemColors.GrayText };
				var missing = new TreeNode("<Missing Dependency>") { ForeColor = SystemColors.GrayText };

				foreach (var s in remaining)
				{
					if (allMods[s].Requires == null)
						unspecified.Nodes.Add(new TreeNode(allMods[s].Title) 
						{ ForeColor = SystemColors.GrayText, Name = s });
					else if (!nodes.ContainsKey(allMods[s].Requires))
						missing.Nodes.Add(new TreeNode(allMods[s].Title) 
						{ ForeColor = SystemColors.GrayText, Name = s });
				}

				treeView.Nodes["BrokenModsNode"].Nodes.Add(unspecified);
				treeView.Nodes["BrokenModsNode"].Nodes.Add(missing);
			}
			treeView.Nodes["ModsNode"].ExpandAll();
			treeView.Invalidate();
		}

		void treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			Mod selectedMod;
			if (!allMods.TryGetValue(e.Node.Name, out selectedMod)) return;
			string modHtmlPath = string.Format("mods{0}{1}{0}mod.html", Path.DirectorySeparatorChar, e.Node.Name);
			if (!File.Exists(modHtmlPath)) return;
			webBrowser.Navigate(Path.GetFullPath(modHtmlPath));
		}
	}
}
