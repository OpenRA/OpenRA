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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.Cnc.Graphics
{
	public class ClassicTilesetSpecificSpriteSequenceLoader : ClassicSpriteSequenceLoader
	{
		public readonly string DefaultSpriteExtension = ".shp";
		public readonly Dictionary<string, string> TilesetExtensions = new Dictionary<string, string>();
		public readonly Dictionary<string, string> TilesetCodes = new Dictionary<string, string>();

		public ClassicTilesetSpecificSpriteSequenceLoader(ModData modData)
			: base(modData)
		{
			var metadata = modData.Manifest.Get<SpriteSequenceFormat>().Metadata;
			if (metadata.TryGetValue("DefaultSpriteExtension", out var yaml))
				DefaultSpriteExtension = yaml.Value;

			if (metadata.TryGetValue("TilesetExtensions", out yaml))
				TilesetExtensions = yaml.ToDictionary(kv => kv.Value);

			if (metadata.TryGetValue("TilesetCodes", out yaml))
				TilesetCodes = yaml.ToDictionary(kv => kv.Value);
		}

		public override ISpriteSequence CreateSequence(ModData modData, string tileset, SpriteCache cache, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			return new ClassicTilesetSpecificSpriteSequence(modData, tileset, cache, this, image, sequence, data, defaults);
		}
	}

	[Desc("A sprite sequence that can have tileset-specific variants and has the oddities " +
	      "that come with first-generation Westwood titles.")]
	public class ClassicTilesetSpecificSpriteSequence : ClassicSpriteSequence
	{
		[Desc("Dictionary of <string: string> with tileset name to override -> tileset name to use instead.")]
		static readonly SpriteSequenceField<Dictionary<string, string>> TilesetOverrides = new SpriteSequenceField<Dictionary<string, string>>(nameof(TilesetOverrides), null);

		[Desc("Use `TilesetCodes` as defined in `mod.yaml` to add a letter as a second character " +
			"into the sprite filename like the Westwood 2.5D titles did for tileset-specific variants.")]
		static readonly SpriteSequenceField<bool> UseTilesetCode = new SpriteSequenceField<bool>(nameof(UseTilesetCode), false);

		[Desc("Append a tileset-specific extension to the file name " +
			"- either as defined in `mod.yaml`'s `TilesetExtensions` (if `UseTilesetExtension` is used) " +
			"or the default hardcoded one for this sequence type (.shp).")]
		static readonly SpriteSequenceField<bool> AddExtension = new SpriteSequenceField<bool>(nameof(AddExtension), true);

		[Desc("Whether `mod.yaml`'s `TilesetExtensions` should be used with the sequence's file name.")]
		static readonly SpriteSequenceField<bool> UseTilesetExtension = new SpriteSequenceField<bool>(nameof(UseTilesetExtension), false);

		public ClassicTilesetSpecificSpriteSequence(ModData modData, string tileset, SpriteCache cache, ISpriteSequenceLoader loader, string image, string sequence, MiniYaml data, MiniYaml defaults)
			: base(modData, tileset, cache, loader, image, sequence, data, defaults) { }

		static string ResolveTilesetId(string tileset, MiniYaml data, MiniYaml defaults)
		{
			var node = data.Nodes.FirstOrDefault(n => n.Key == TilesetOverrides.Key) ?? defaults.Nodes.FirstOrDefault(n => n.Key == TilesetOverrides.Key);
			var overrideNode = node?.Value.Nodes.FirstOrDefault(n => n.Key == tileset);
			return overrideNode?.Value.Value ?? tileset;
		}

		protected override string GetSpriteFilename(ModData modData, string tileset, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			var loader = (ClassicTilesetSpecificSpriteSequenceLoader)Loader;
			var filename = data.Value ?? defaults.Value ?? image;
			if (LoadField(UseTilesetCode, data, defaults))
				if (loader.TilesetCodes.TryGetValue(ResolveTilesetId(tileset, data, defaults), out var tilesetCode))
					filename = filename.Substring(0, 1) + tilesetCode + filename.Substring(2, filename.Length - 2);

			if (LoadField(AddExtension, data, defaults))
			{
				if (LoadField(UseTilesetExtension, data, defaults))
					if (loader.TilesetExtensions.TryGetValue(ResolveTilesetId(tileset, data, defaults), out var tilesetExtension))
						return filename + tilesetExtension;

				return filename + loader.DefaultSpriteExtension;
			}

			return filename;
		}
	}
}
