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
			return new ClassicTilesetSpecificSpriteSequence(cache, this, image, sequence, data, defaults);
		}
	}

	[Desc("A sprite sequence that can have tileset-specific variants and has the oddities " +
	      "that come with first-generation Westwood titles.")]
	public class ClassicTilesetSpecificSpriteSequence : ClassicSpriteSequence
	{
		[Desc("Dictionary of <tileset name>: filename to override the Filename key.")]
		static readonly SpriteSequenceField<Dictionary<string, string>> TilesetFilenames = new(nameof(TilesetFilenames), null);

		public ClassicTilesetSpecificSpriteSequence(SpriteCache cache, ISpriteSequenceLoader loader, string image, string sequence, MiniYaml data, MiniYaml defaults)
			: base(cache, loader, image, sequence, data, defaults) { }

		protected override IEnumerable<ReservationInfo> ParseFilenames(ModData modData, string tileset, int[] frames, MiniYaml data, MiniYaml defaults)
		{
			var node = data.Nodes.FirstOrDefault(n => n.Key == TilesetFilenames.Key) ?? defaults.Nodes.FirstOrDefault(n => n.Key == TilesetFilenames.Key);
			if (node != null)
			{
				var tilesetNode = node.Value.Nodes.FirstOrDefault(n => n.Key == tileset);
				if (tilesetNode != null)
				{
					var loadFrames = CalculateFrameIndices(start, length, stride ?? length ?? 0, facings, frames, transpose, reverseFacings, shadowStart);
					return new[] { new ReservationInfo(tilesetNode.Value.Value, loadFrames, frames, tilesetNode.Location) };
				}
			}

			return base.ParseFilenames(modData, tileset, frames, data, defaults);
		}

		protected override IEnumerable<ReservationInfo> ParseCombineFilenames(ModData modData, string tileset, int[] frames, MiniYaml data)
		{
			var node = data.Nodes.FirstOrDefault(n => n.Key == TilesetFilenames.Key);
			if (node != null)
			{
				var tilesetNode = node.Value.Nodes.FirstOrDefault(n => n.Key == tileset);
				if (tilesetNode != null)
				{
					if (frames == null)
					{
						if (LoadField<string>("Length", null, data) != "*")
						{
							var subStart = LoadField("Start", 0, data);
							var subLength = LoadField("Length", 1, data);
							frames = Exts.MakeArray(subLength, i => subStart + i);
						}
					}

					return new[] { new ReservationInfo(tilesetNode.Value.Value, frames, frames, tilesetNode.Location) };
				}
			}

			return base.ParseCombineFilenames(modData, tileset, frames, data);
		}
	}
}
