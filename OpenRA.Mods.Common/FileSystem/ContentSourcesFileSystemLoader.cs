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

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.FileSystem
{
	public class ContentSourcesFileSystemLoader : IFileSystemLoader, IFileSystemExternalContent
	{
		[YamlNode("ContentSource", shared: false)]
		public class ContentSourceSettings : SettingsModule
		{
			public string ContentSource = null;

			// Allow subclasses to add their own arbitrary keys without having to make their own subclass / settings module
			public string this[string key]
			{
				get => Yaml.NodeWithKeyOrDefault(key)?.Value.Value;
				set
				{
					var node = Yaml.NodeWithKeyOrDefault(key);
					if (node != null)
						node.Value.Value = value;
					else
						Yaml.Nodes.Add(new MiniYamlNodeBuilder(key, value));
				}
			}

			public bool TryGetValue(string key, out string value)
			{
				value = Yaml.NodeWithKeyOrDefault(key)?.Value?.Value;
				return value != null;
			}
		}

		[FieldLoader.Require]
		[Desc("Mod to use for source selection.")]
		public readonly string SourceSelectorMod = null;

		[Desc("A list of mod-provided packages. Anything required to display the initial load screen must be listed here.")]
		public readonly FrozenDictionary<string, string> SystemPackages = null;

		[Desc("A list of packages to mount after ContentPackages, potentially overriding their content.")]
		public readonly FrozenDictionary<string, string> OverridePackages = null;

		[FieldLoader.LoadUsing(nameof(LoadContentSources))]
		[Desc("A list of sources defining how to load the game content.")]
		public readonly FrozenDictionary<string, ContentSource> ContentSources;

		public static FrozenDictionary<string, ContentSource> LoadContentSources(MiniYaml yaml)
		{
			var ret = new Dictionary<string, ContentSource>();
			var sourcesNode = yaml.Nodes.Single(n => n.Key == "ContentSources");
			foreach (var s in sourcesNode.Value.Nodes)
				ret.Add(s.Key, new ContentSource(s.Value));

			return ret.ToFrozenDictionary();
		}

		bool contentAvailable;
		protected ContentSourceSettings sourceSettings;

		public virtual void Mount(Manifest manifest, OpenRA.FileSystem.FileSystem fileSystem, ObjectCreator objectCreator)
		{
			foreach (var kv in SystemPackages)
				fileSystem.Mount(kv.Key, kv.Value);

			sourceSettings = Game.Settings.GetOrCreate<ContentSourceSettings>(objectCreator, manifest.Id);
			if (sourceSettings.ContentSource == null || !ContentSources.TryGetValue(sourceSettings.ContentSource, out var source))
				return;

			if (!source.TryMount(fileSystem, objectCreator, out var attributes))
				return;

			// Send the player to the source selector mod if they have requested an attribute not provided by this origin.
			foreach (var attribute in attributes)
				if (sourceSettings.TryGetValue(attribute.Key, out var value) && !attribute.Value.Contains(value))
					return;

			contentAvailable = true;
			foreach (var p in source.Packages)
			{
				try
				{
					fileSystem.Mount(p.Key, p.Value);
				}
				catch
				{
					contentAvailable = false;
				}
			}

			foreach (var kv in OverridePackages)
				fileSystem.Mount(kv.Key, kv.Value);
		}

		protected virtual bool InstallContentIfRequired(ModData modData)
		{
			if (!contentAvailable && Game.Mods.TryGetValue(SourceSelectorMod, out var mod))
				Game.InitializeMod(mod, new Arguments());

			return !contentAvailable;
		}

		bool IFileSystemExternalContent.InstallContentIfRequired(ModData modData) => InstallContentIfRequired(modData);

		void IFileSystemExternalContent.ManageContent(ModData modData)
		{
			// Switching mods changes the world state (by disposing it),
			// so we can't do this inside the input handler.
			if (Game.Mods.TryGetValue(SourceSelectorMod, out var mod))
				Game.RunAfterTick(() => Game.InitializeMod(mod, new Arguments()));
		}
	}
}
