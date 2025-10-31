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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.D2k.SpriteLoaders;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2k.Graphics
{
	public class D2kSpriteSequenceLoader : DefaultSpriteSequenceLoader
	{
		public D2kSpriteSequenceLoader(ModData modData)
			: base(modData) { }

		public override D2kSpriteSequence CreateSequence(
			ModData modData, string tileset, SpriteCache cache, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			return new D2kSpriteSequence(cache, this, image, sequence, data, defaults);
		}
	}

	[Desc("A sprite sequence that understands how to apply colour remapping to D2k sprites.")]
	public class D2kSpriteSequence : DefaultSpriteSequence
	{
		[Desc("Sets the player remap reference colour.")]
		static readonly SpriteSequenceField<Color> Remap = new(nameof(Remap), default);

		[Desc("Remap embedded palette index 1 to shadow.")]
		static readonly SpriteSequenceField<bool> UseShadow = new(nameof(UseShadow), true);

		[Desc("Indicates that this is a fog sprite definition.")]
		static readonly SpriteSequenceField<bool> ConvertShroudToFog = new(nameof(ConvertShroudToFog), false);

		readonly Color remapColor;
		readonly bool useShadow;
		readonly bool convertShroudToFog;

		public D2kSpriteSequence(SpriteCache cache, ISpriteSequenceLoader loader, string image, string sequence, MiniYaml data, MiniYaml defaults)
			: base(cache, loader, image, sequence, data, defaults)
		{
			remapColor = LoadField(Remap, data, defaults);
			useShadow = LoadField(UseShadow, data, defaults);
			convertShroudToFog = LoadField(ConvertShroudToFog, data, defaults);
		}

		public override void ReserveSprites(ModData modData, string tileset, SpriteCache cache, MiniYaml data, MiniYaml defaults)
		{
			var frames = LoadField(Frames, data, defaults);
			var flipX = LoadField(FlipX, data, defaults);
			var flipY = LoadField(FlipY, data, defaults);
			var zRamp = LoadField(ZRamp, data, defaults);
			var offset = LoadField(Offset, data, defaults);
			var blendMode = LoadField(BlendMode, data, defaults);

			AdjustFrame adjustFrame = null;
			if (remapColor != default || convertShroudToFog)
				adjustFrame = RemapFrame;

			ISpriteFrame RemapFrame(ISpriteFrame f, int index, int total) =>
				(f is R8Loader.RemappableFrame rf) ? rf.WithSequenceFlags(useShadow, convertShroudToFog, remapColor) : f;

			var combineNode = data.NodeWithKeyOrDefault(Combine.Key);
			if (combineNode != null)
			{
				for (var i = 0; i < combineNode.Value.Nodes.Length; i++)
				{
					var subData = combineNode.Value.Nodes[i].Value;
					var subOffset = LoadField(Offset, subData, NoData);
					var subFlipX = LoadField(FlipX, subData, NoData);
					var subFlipY = LoadField(FlipY, subData, NoData);
					var subFrames = LoadField(Frames, subData);

					foreach (var f in ParseCombineFilenames(modData, tileset, subFrames, subData))
					{
						var token = cache.ReserveSprites(f.Filename, f.LoadFrames, f.Location, adjustFrame);

						spritesToLoad.Add(new SpriteReservation
						{
							Token = token,
							Offset = subOffset + offset,
							FlipX = subFlipX ^ flipX,
							FlipY = subFlipY ^ flipY,
							BlendMode = blendMode,
							ZRamp = zRamp,
							Frames = f.Frames
						});
					}
				}
			}
			else
			{
				foreach (var f in ParseFilenames(modData, tileset, frames, data, defaults))
				{
					var token = cache.ReserveSprites(f.Filename, f.LoadFrames, f.Location, adjustFrame);

					spritesToLoad.Add(new SpriteReservation
					{
						Token = token,
						Offset = offset,
						FlipX = flipX,
						FlipY = flipY,
						BlendMode = blendMode,
						ZRamp = zRamp,
						Frames = f.Frames,
					});
				}
			}
		}
	}
}
