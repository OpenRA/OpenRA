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
using System.IO;
using System.Linq;

namespace OpenRA.Mods.Common.FileSystem
{
	public class CncFreewareContentOrigin : ContentOrigin
	{
		public class ISOVolume
		{
			[Desc("Mount the volume using this explicit mount name.")]
			public readonly string Mount;

			[FieldLoader.LoadUsing(nameof(LoadAttributes))]
			[Desc("Attributes that this volume will provide in addition to the main origin attributes.")]
			public readonly FrozenDictionary<string, ImmutableArray<string>> Attributes;

			public static FrozenDictionary<string, ImmutableArray<string>> LoadAttributes(MiniYaml yaml) => ContentOrigin.LoadAttributes(yaml);
		}

		[Desc("Path to the package containing the quick install assets.")]
		public readonly string Package;

		[Desc("Set to false in the content mod to allow this origin to be selected to show the download button.")]
		public readonly bool PackageRequired = true;

		[Desc("Mount the quick install package using this explicit mount name.")]
		public readonly string PackageMount;

		[Desc("Directory to search for game ISO images.")]
		public readonly string ISODirectory;

		[FieldLoader.LoadUsing(nameof(LoadISOVolumes))]
		[Desc("Directory of <iso volume name>: <mount and attribute metadata>.")]
		public readonly FrozenDictionary<string, ISOVolume> ISOVolumes = null;

		public static FrozenDictionary<string, ISOVolume> LoadISOVolumes(MiniYaml yaml)
		{
			var ret = new Dictionary<string, ISOVolume>();
			var volumesNode = yaml.Nodes.SingleOrDefault(n => n.Key == "ISOVolumes");
			foreach (var v in volumesNode.Value.Nodes)
				ret.Add(v.Key, FieldLoader.Load<ISOVolume>(v.Value));

			return ret.ToFrozenDictionary();
		}

		public override bool TryMount(OpenRA.FileSystem.FileSystem fileSystem) => ResolveOrigin(fileSystem, true);
		public override bool CanMount(OpenRA.FileSystem.FileSystem fileSystem) => ResolveOrigin(fileSystem, false);

		bool ResolveOrigin(OpenRA.FileSystem.FileSystem fileSystem, bool mount)
		{
			var resolved = Package != null && fileSystem.Exists(Package);
			if (resolved)
			{
				var package = fileSystem.OpenPackage(Package);
				if (!IDFilesMatch(package, IDFiles))
					resolved = false;

				if (resolved && mount)
					fileSystem.Mount(package, PackageMount);
			}

			var isoDirectory = !string.IsNullOrEmpty(ISODirectory) ? Platform.ResolvePath(ISODirectory) : null;
			if (mount && Path.Exists(isoDirectory) && ISOVolumes.Count > 0)
			{
				var volumes = GetISOVolumes(isoDirectory);
				foreach (var volume in ISOVolumes)
					if (volumes.TryGetValue(volume.Key, out var isoPath))
						fileSystem.Mount(isoPath, volume.Value.Mount);
			}

			return resolved || !PackageRequired;
		}

		public override FrozenDictionary<string, ImmutableArray<string>> GetAttributes()
		{
			var isoDirectory = !string.IsNullOrEmpty(ISODirectory) ? Platform.ResolvePath(ISODirectory) : null;
			if (ISOVolumes.Count == 0 || !Path.Exists(isoDirectory))
				return Attributes;

			var volumes = GetISOVolumes(isoDirectory);
			var volumeAttributes = new Dictionary<string, HashSet<string>>();
			foreach (var volume in ISOVolumes)
			{
				if (!volumes.ContainsKey(volume.Key))
					continue;

				foreach (var attribute in volume.Value.Attributes)
					foreach (var value in attribute.Value)
						volumeAttributes.GetOrAdd(attribute.Key).Add(value);
			}

			if (volumeAttributes.Count == 0)
				return Attributes;

			foreach (var attribute in Attributes)
				foreach (var value in attribute.Value)
					volumeAttributes.GetOrAdd(attribute.Key).Add(value);

			return volumeAttributes.ToFrozenDictionary(
				kv => kv.Key,
				kv => kv.Value.ToImmutableArray());
		}

		static Dictionary<string, string> GetISOVolumes(string isoDirectory)
		{
			var volumes = new Dictionary<string, string>();
			foreach (var isoPath in Directory.GetFiles(isoDirectory, "*.ISO", SearchOption.TopDirectoryOnly))
			{
				using var s = new FileStream(isoPath, FileMode.Open);
				if (s.Length < 34816)
					return null;

				s.Position = 32769;
				if (s.ReadASCII(5) != "CD001")
					return null;

				s.Position = 32808;
				volumes[s.ReadASCII(32).Trim()] = isoPath;
			}

			return volumes;
		}
	}
}
