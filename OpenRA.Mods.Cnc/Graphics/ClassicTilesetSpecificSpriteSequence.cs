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
		public ClassicTilesetSpecificSpriteSequenceLoader(ModData modData)
			: base(modData) { }

		public override ISpriteSequence CreateSequence(ModData modData, string tileset, SpriteCache cache, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			return new ClassicTilesetSpecificSpriteSequence(modData, tileset, cache, this, image, sequence, data, defaults);
		}
	}

	[Desc("A sprite sequence that can have tileset-specific variants and has the oddities " +
	      "that come with first-generation Westwood titles.")]
	public class ClassicTilesetSpecificSpriteSequence : ClassicSpriteSequence
	{
		[Desc("Dictionary of <tileset name>: filename to override the Filename key.")]
		static readonly SpriteSequenceField<Dictionary<string, string>> TilesetFilenames = new SpriteSequenceField<Dictionary<string, string>>(nameof(TilesetFilenames), null);

		public ClassicTilesetSpecificSpriteSequence(ModData modData, string tileset, SpriteCache cache, ISpriteSequenceLoader loader, string image, string sequence, MiniYaml data, MiniYaml defaults)
			: base(modData, tileset, cache, loader, image, sequence, data, defaults) { }

		protected override string GetSpriteFilename(ModData modData, string tileset, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			var node = data.Nodes.FirstOrDefault(n => n.Key == TilesetFilenames.Key) ?? defaults.Nodes.FirstOrDefault(n => n.Key == TilesetFilenames.Key);
			if (node != null)
			{
				var tilesetNode = node.Value.Nodes.FirstOrDefault(n => n.Key == tileset);
				if (tilesetNode != null)
					return tilesetNode.Value.Value;
			}

			return base.GetSpriteFilename(modData, tileset, image, sequence, data, defaults);
		}
	}
}
