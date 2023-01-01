#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
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
			public readonly string[] TestFiles = Array.Empty<string>();
			public readonly string[] Sources = Array.Empty<string>();
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
			public readonly ObjectCreator ObjectCreator;

			[FieldLoader.Ignore]
			public readonly MiniYaml Type;

			// Used to find installation locations for SourceType.Install
			public readonly string[] RegistryPrefixes = { string.Empty };
			public readonly string RegistryKey;
			public readonly string RegistryValue;

			public readonly string Title;

			[FieldLoader.Ignore]
			public readonly MiniYaml IDFiles;

			[FieldLoader.Ignore]
			public readonly List<MiniYamlNode> Install;

			public ModSource(MiniYaml yaml, ObjectCreator objectCreator)
			{
				ObjectCreator = objectCreator;
				Title = yaml.Value;

				var type = yaml.Nodes.FirstOrDefault(n => n.Key == "Type");
				if (type != null)
					Type = type.Value;

				var idFiles = yaml.Nodes.FirstOrDefault(n => n.Key == "IDFiles");
				if (idFiles != null)
					IDFiles = idFiles.Value;

				var installNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Install");
				if (installNode != null)
					Install = installNode.Value.Nodes;

				FieldLoader.Load(this, yaml);
			}
		}

		public class ModDownload
		{
			public readonly ObjectCreator ObjectCreator;
			public readonly string Title;
			public readonly string URL;
			public readonly string MirrorList;
			public readonly string SHA1;
			public readonly string Type;
			public readonly Dictionary<string, string> Extract;

			public ModDownload(MiniYaml yaml, ObjectCreator objectCreator)
			{
				ObjectCreator = objectCreator;
				Title = yaml.Value;
				FieldLoader.Load(this, yaml);
			}
		}

		public readonly string InstallPromptMessage;
		public readonly string QuickDownload;
		public readonly string HeaderMessage;
		public readonly string ContentInstallerMod = "modcontent";

		[FieldLoader.LoadUsing(nameof(LoadPackages))]
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

		[FieldLoader.LoadUsing(nameof(LoadDownloads))]
		public readonly string[] Downloads = Array.Empty<string>();

		static object LoadDownloads(MiniYaml yaml)
		{
			var downloadNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Downloads");
			return downloadNode != null ? downloadNode.Value.Nodes.Select(n => n.Key).ToArray() : Array.Empty<string>();
		}

		[FieldLoader.LoadUsing(nameof(LoadSources))]
		public readonly string[] Sources = Array.Empty<string>();

		static object LoadSources(MiniYaml yaml)
		{
			var sourceNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Sources");
			return sourceNode != null ? sourceNode.Value.Nodes.Select(n => n.Key).ToArray() : Array.Empty<string>();
		}
	}
}
