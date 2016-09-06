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
		public enum SourceType { Disc, Install }
		public class ModPackage
		{
			public readonly string Title;
			public readonly string[] TestFiles = { };
			public readonly string[] Sources = { };
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

		public class ModSource
		{
			public readonly SourceType Type = SourceType.Disc;

			// Used to find installation locations for SourceType.Install
			public readonly string RegistryKey;
			public readonly string RegistryValue;

			public readonly string Title;
			public readonly Dictionary<string, string> IDFiles;

			[FieldLoader.Ignore] public readonly List<MiniYamlNode> Install;

			public ModSource(MiniYaml yaml)
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
		public readonly string[] Downloads = { };

		static object LoadDownloads(MiniYaml yaml)
		{
			var downloadNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Downloads");
			return downloadNode != null ? downloadNode.Value.Nodes.Select(n => n.Key).ToArray() : new string[0];
		}

		[FieldLoader.LoadUsing("LoadSources")]
		public readonly string[] Sources = { };

		static object LoadSources(MiniYaml yaml)
		{
			var sourceNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Sources");
			return sourceNode != null ? sourceNode.Value.Nodes.Select(n => n.Key).ToArray() : new string[0];
		}
	}
}
