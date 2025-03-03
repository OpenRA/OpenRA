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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.Common
{
	public class ModContent : IGlobalModData
	{
		public class ModPackage
		{
			[FluentReference]
			public readonly string Title;
			public readonly string Identifier;
			public readonly string[] TestFiles = [];
			public readonly string[] Sources = [];
			public readonly bool Required;
			public readonly string Download;

			public ModPackage(MiniYaml yaml)
			{
				FieldLoader.Load(this, yaml);
			}

			public bool IsInstalled()
			{
				return TestFiles.All(file => File.Exists(Path.GetFullPath(Platform.ResolvePath(file))));
			}
		}

		public class ModSource
		{
			[FieldLoader.Ignore]
			public readonly MiniYaml Type;

			// Used to find installation locations for SourceType.Install
			public readonly string[] RegistryPrefixes = [string.Empty];
			public readonly string RegistryKey;
			public readonly string RegistryValue;

			public readonly string Title;

			[FieldLoader.Ignore]
			public readonly MiniYaml IDFiles;

			[FieldLoader.Ignore]
			public readonly ImmutableArray<MiniYamlNode> Install;

			public readonly string TooltipText;

			public ModSource(MiniYaml yaml)
			{
				Title = yaml.Value;

				var type = yaml.NodeWithKeyOrDefault("Type");
				if (type != null)
					Type = type.Value;

				var idFiles = yaml.NodeWithKeyOrDefault("IDFiles");
				if (idFiles != null)
					IDFiles = idFiles.Value;

				var installNode = yaml.NodeWithKeyOrDefault("Install");
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
			public readonly string SHA1;
			public readonly string Type;
			public readonly Dictionary<string, string> Extract;

			public ModDownload(MiniYaml yaml)
			{
				Title = yaml.Value;
				FieldLoader.Load(this, yaml);
			}
		}

		public readonly string QuickDownload;

		[FieldLoader.Require]
		public readonly string Mod;

		[FieldLoader.LoadUsing(nameof(LoadPackages))]
		public readonly Dictionary<string, ModPackage> Packages = [];

		static object LoadPackages(MiniYaml yaml)
		{
			var packages = new Dictionary<string, ModPackage>();
			var packageNode = yaml.NodeWithKeyOrDefault("Packages");
			if (packageNode != null)
				foreach (var node in packageNode.Value.Nodes)
					packages.Add(node.Key, new ModPackage(node.Value));

			return packages;
		}

		[FieldLoader.LoadUsing(nameof(LoadDownloads))]
		public readonly string[] Downloads = [];

		static object LoadDownloads(MiniYaml yaml)
		{
			var downloadNode = yaml.NodeWithKeyOrDefault("Downloads");
			return downloadNode != null ? downloadNode.Value.Nodes.Select(n => n.Key).ToArray() : [];
		}

		[FieldLoader.LoadUsing(nameof(LoadSources))]
		public readonly string[] Sources = [];

		static object LoadSources(MiniYaml yaml)
		{
			var sourceNode = yaml.NodeWithKeyOrDefault("Sources");
			return sourceNode != null ? sourceNode.Value.Nodes.Select(n => n.Key).ToArray() : [];
		}
	}
}
