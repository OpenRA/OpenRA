#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA
{
	public class ModContent : IGlobalModData
	{
		public class ModPackage
		{
			public readonly string Title;
			public readonly string[] TestFiles = { };
			public readonly string[] Discs = { };
			public readonly bool Required;
			public readonly string Download;

			public ModPackage(MiniYaml yaml)
			{
				Title = yaml.Value;
				FieldLoader.Load(this, yaml);
			}

			public bool IsInstalled()
			{
				return TestFiles.All(file => File.Exists(Path.GetFullPath(Platform.ResolvePath(file))));
			}
		}

		public class ModDisc
		{
			public readonly string Title;
			public readonly Dictionary<string, string> IDFiles;

			[FieldLoader.Ignore] public readonly List<MiniYamlNode> Install;

			public ModDisc(MiniYaml yaml)
			{
				Title = yaml.Value;
				var installNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Install");
				if (installNode != null)
					Install = installNode.Value.Nodes;

				FieldLoader.Load(this, yaml);
			}
		}

		public class ModDownload
		{
			public readonly string Title;
			public readonly string URL;
			public readonly string MirrorList;
			public readonly Dictionary<string, string> Extract;

			public ModDownload(MiniYaml yaml)
			{
				Title = yaml.Value;
				FieldLoader.Load(this, yaml);
			}
		}

		public readonly string InstallPromptMessage;
		public readonly string QuickDownload;
		public readonly string HeaderMessage;

		[FieldLoader.LoadUsing("LoadPackages")]
		public readonly Dictionary<string, ModPackage> Packages = new Dictionary<string, ModPackage>();

		static object LoadPackages(MiniYaml yaml)
		{
			var packages = new Dictionary<string, ModPackage>();
			var packageNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Packages");
			if (packageNode != null)
				foreach (var node in packageNode.Value.Nodes)
					packages.Add(node.Key, new ModPackage(node.Value));

			return packages;
		}

		[FieldLoader.LoadUsing("LoadDownloads")]
		public readonly Dictionary<string, ModDownload> Downloads;

		static object LoadDownloads(MiniYaml yaml)
		{
			var downloads = new Dictionary<string, ModDownload>();
			var downloadNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Downloads");
			if (downloadNode != null)
				foreach (var node in downloadNode.Value.Nodes)
					downloads.Add(node.Key, new ModDownload(node.Value));

			return downloads;
		}

		[FieldLoader.LoadUsing("LoadDiscs")]
		public readonly Dictionary<string, ModDisc> Discs;

		static object LoadDiscs(MiniYaml yaml)
		{
			var discs = new Dictionary<string, ModDisc>();
			var discNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Discs");
			if (discNode != null)
				foreach (var node in discNode.Value.Nodes)
					discs.Add(node.Key, new ModDisc(node.Value));

			return discs;
		}
	}
}
