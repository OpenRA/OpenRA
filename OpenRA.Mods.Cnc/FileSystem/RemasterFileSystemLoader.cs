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
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Mods.Common.FileSystem;

namespace OpenRA.Mods.Cnc.FileSystem
{
	public class RemasterFileSystemLoader : ContentSourcesFileSystemLoader
	{
		[FieldLoader.LoadUsing(nameof(LoadOverrideVoices))]
		[Desc("A list of paths for each content source to replace the manifest Voices definition. " +
			"Items enclosed in braces will be replaced by the named ContentSourceSettings key.")]
		public readonly FrozenDictionary<string, ImmutableArray<string>> OverrideVoices = null;

		[FieldLoader.LoadUsing(nameof(LoadOverrideNotifications))]
		[Desc("A list of paths for each content source to replace the manifest Voices definition. " +
			"Items enclosed in braces will be replaced by the named ContentSourceSettings key.")]
		public readonly FrozenDictionary<string, ImmutableArray<string>> OverrideNotifications = null;

		[FieldLoader.LoadUsing(nameof(LoadOverrideMusic))]
		[Desc("A list of paths for each content source to replace the manifest Voices definition. " +
			"Items enclosed in braces will be replaced by the named ContentSourceSettings key.")]
		public readonly FrozenDictionary<string, ImmutableArray<string>> OverrideMusic = null;

		public static FrozenDictionary<string, ImmutableArray<string>> LoadOverrideVoices(MiniYaml yaml)
			=> LoadOverride("OverrideVoices", yaml);

		public static FrozenDictionary<string, ImmutableArray<string>> LoadOverrideNotifications(MiniYaml yaml)
			=> LoadOverride("OverrideNotifications", yaml);

		public static FrozenDictionary<string, ImmutableArray<string>> LoadOverrideMusic(MiniYaml yaml)
			=> LoadOverride("OverrideMusic", yaml);

		public static FrozenDictionary<string, ImmutableArray<string>> LoadOverride(string key, MiniYaml yaml)
		{
			var ret = new Dictionary<string, ImmutableArray<string>>();
			var sourcesNode = yaml.Nodes.SingleOrDefault(n => n.Key == key);
			if (sourcesNode != null)
				foreach (var s in sourcesNode.Value.Nodes)
					ret.Add(s.Key, s.Value.Nodes.Select(n => n.Key).ToImmutableArray());

			return ret.ToFrozenDictionary();
		}

		void OverrideManifestPaths(Manifest manifest, string key, ImmutableArray<string> overridePaths)
		{
			var paths = new List<string>();
			foreach (var p in overridePaths)
			{
				var path = p;
				foreach (var setting in sourceSettings.Yaml.Nodes)
					path = path.Replace("{" + setting.Key + "}", sourceSettings[setting.Key]);

				paths.Add(path);
			}

			manifest.GetType().GetField(key)?.SetValue(manifest, paths.ToImmutableArray());
		}

		protected override bool InstallContentIfRequired(ModData modData)
		{
			if (base.InstallContentIfRequired(modData))
				return true;

			if (OverrideVoices != null && OverrideVoices.TryGetValue(sourceSettings.ContentSource, out var paths))
				OverrideManifestPaths(modData.Manifest, "Voices", paths);

			if (OverrideNotifications != null && OverrideNotifications.TryGetValue(sourceSettings.ContentSource, out paths))
				OverrideManifestPaths(modData.Manifest, "Notifications", paths);

			if (OverrideMusic != null && OverrideMusic.TryGetValue(sourceSettings.ContentSource, out paths))
				OverrideManifestPaths(modData.Manifest, "Music", paths);

			return false;
		}
	}
}
