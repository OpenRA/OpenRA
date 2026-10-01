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
using System.Collections.Frozen;
using System.Collections.Immutable;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.FileSystem
{
	public class ContentOrigin
	{
		public class IDFile
		{
			[Desc("SHA1 hash expected for this file.")]
			public readonly string SHA1;

			[Desc("Use with Length to restrict the SHA1 calculation to a subset of the file.")]
			public readonly int Offset;

			[Desc("Use with Offset to restrict the SHA1 calculation to a subset of the file.")]
			public readonly int Length;

			public IDFile(MiniYaml yaml)
			{
				SHA1 = yaml.Value;
				FieldLoader.Load(this, yaml);
			}
		}

		[FieldLoader.LoadUsing(nameof(LoadAttributes))]
		[Desc("Attributes that this origin will provide.")]
		public readonly FrozenDictionary<string, ImmutableArray<string>> Attributes;

		[FieldLoader.LoadUsing(nameof(LoadIDFiles))]
		[Desc("Dictionary of <path>: <SHA1 identification metadata>.")]
		public readonly FrozenDictionary<string, IDFile> IDFiles;

		public static FrozenDictionary<string, ImmutableArray<string>> LoadAttributes(MiniYaml yaml)
		{
			var node = yaml.NodeWithKeyOrDefault("Attributes")?.Value;
			if (node == null)
				return FrozenDictionary<string, ImmutableArray<string>>.Empty;

			return node.Nodes.ToFrozenDictionary(
				n => n.Key,
				n => FieldLoader.GetValue<ImmutableArray<string>>("Attributes", n.Value.Value));
		}

		public static FrozenDictionary<string, IDFile> LoadIDFiles(MiniYaml yaml)
		{
			var node = yaml.NodeWithKeyOrDefault("IDFiles")?.Value;
			if (node == null)
				return FrozenDictionary<string, IDFile>.Empty;

			return node.Nodes.ToFrozenDictionary(
				n => n.Key,
				n => new IDFile(n.Value));
		}

		public virtual bool TryMount(OpenRA.FileSystem.FileSystem fileSystem) => true;
		public virtual bool CanMount(OpenRA.FileSystem.FileSystem fileSystem) => true;

		public virtual FrozenDictionary<string, ImmutableArray<string>> GetAttributes()
		{
			return Attributes;
		}

		public static bool IDFilesMatch(IReadOnlyPackage package, FrozenDictionary<string, IDFile> idFiles)
		{
			try
			{
				foreach (var kv in idFiles)
				{
					using var stream = package.GetStream(kv.Key);
					if (kv.Value.Offset != 0 || kv.Value.Length != 0)
					{
						stream.Position = kv.Value.Offset;
						var data = stream.ReadBytes(kv.Value.Length);
						if (CryptoUtil.SHA1Hash(data) != kv.Value.SHA1)
							return false;
					}
					else if (CryptoUtil.SHA1Hash(stream) != kv.Value.SHA1)
						return false;
				}
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}
	}
}
