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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Terrain;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.Graphics
{
	sealed class MaskedFrame : ISpriteFrame
	{
		readonly ISpriteFrame inner;
		readonly ISpriteFrame mask;
		byte[] data;

		public MaskedFrame(ISpriteFrame inner, ISpriteFrame mask)
		{
			this.inner = inner;
			this.mask = mask;
		}

		public SpriteFrameType Type => inner.Type;
		public Size Size => inner.Size;
		public Size FrameSize => inner.FrameSize;
		public Vector2 Offset => inner.Offset;
		public bool DisableExportPadding => inner.DisableExportPadding;

		public byte[] Data
		{
			get
			{
				if (data == null)
				{
					data = new byte[inner.Data.Length];

					var channels = inner.Data.Length / mask.Data.Length;
					for (var j = 0; j < mask.Data.Length; j++)
						if (mask.Data[j] != 0)
							for (var k = 0; k < channels; k++)
								data[j * channels + k] = inner.Data[j * channels + k];
				}

				return data;
			}
		}
	}

	public class RemasterSpriteSequenceLoader : ClassicTilesetSpecificSpriteSequenceLoader
	{
		public override ClassicTilesetSpecificSpriteSequence CreateSequence(ModData modData, string tileset, SpriteCache cache,
			string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			if (!((RemasterTerrain)modData.DefaultTerrainInfo[tileset]).UseRemasteredTerrain)
				return new ClassicTilesetSpecificSpriteSequence(cache, this, image, sequence, data, defaults);

			var terrainInfo = (RemasterTerrain)modData.DefaultTerrainInfo[tileset];
			var classicUpscaleFactor = terrainInfo.RemasteredTileSize.Width * 1f / terrainInfo.TileSize.Width;
			return new RemasterSpriteSequence(cache, this, tileset, image, sequence, data, defaults, classicUpscaleFactor);
		}
	}

	[Desc("A sprite sequence that can have tileset-specific variants and has the oddities " +
			"that come with first-generation Westwood titles.")]
	public class RemasterSpriteSequence : ClassicTilesetSpecificSpriteSequence
	{
		[Desc("File name of the remastered sprite to use for this sequence.")]
		static readonly SpriteSequenceField<string> RemasteredFilename = new(nameof(RemasteredFilename), null);

		[Desc("Dictionary of <tileset name>: filename to override the RemasteredFilename key.")]
		static readonly SpriteSequenceField<Dictionary<string, string>> RemasteredTilesetFilenames = new(nameof(RemasteredTilesetFilenames), null);

		[Desc("File name pattern to build the remastered sprite to use for this sequence.")]
		static readonly SpriteSequenceField<string> RemasteredFilenamePattern = new(nameof(RemasteredFilenamePattern), null);

		[Desc("Dictionary of <tileset name>: <filename pattern> to override the RemasteredFilenamePattern key.")]
		static readonly SpriteSequenceField<Dictionary<string, string>> RemasteredTilesetFilenamesPattern = new(nameof(RemasteredTilesetFilenamesPattern), null);

		[Desc("File name pattern of the sprite to mask the remastered sprite.")]
		static readonly SpriteSequenceField<string> RemasteredMaskFilename = new(nameof(RemasteredMaskFilename), null);

		[Desc("Change the position in-game on X, Y, Z.")]
		protected static readonly SpriteSequenceField<Vector3> RemasteredOffset = new(nameof(RemasteredOffset), Vector3.Zero);

		[Desc("Frame index to start from.")]
		protected static readonly SpriteSequenceField<int?> RemasteredStart = new(nameof(RemasteredStart), null);

		[Desc("Number of frames to use. Does not have to be the total amount the sprite sheet has.")]
		protected static readonly SpriteSequenceField<int?> RemasteredLength = new(nameof(RemasteredLength), null);

		[Desc("Time (in milliseconds at default game speed) to wait until playing the next frame in the animation.")]
		protected static readonly SpriteSequenceField<int?> RemasteredTick = new(nameof(RemasteredTick), null);

		[Desc("Adjusts the rendered size of the sprite")]
		protected static readonly SpriteSequenceField<float?> RemasteredScale = new(nameof(RemasteredScale), null);

		[Desc("Dictionary of <tileset name>: scale to override the RemasteredScale key.")]
		static readonly SpriteSequenceField<Dictionary<string, float>> RemasteredTilesetScales = new(nameof(RemasteredTilesetScales), null);

		[Desc("Sprite data is already pre-multiplied by alpha channel.")]
		protected static readonly SpriteSequenceField<bool> RemasteredPremultiplied = new(nameof(RemasteredPremultiplied), true);

		[Desc("Sets transparency - use one value to set for all frames or provide a value for each frame.")]
		protected static readonly SpriteSequenceField<ImmutableArray<float>> RemasteredAlpha = new(nameof(RemasteredAlpha), default);

		bool hasRemasteredSprite = true;
		readonly float classicUpscaleFactor;

		IEnumerable<ReservationInfo> ParseRemasterFilenames(ModData modData, string tileset, ImmutableArray<int> frames, MiniYaml data, MiniYaml defaults)
		{
			string filename = null;
			MiniYamlNode.SourceLocation location = default;
			var remasteredTilesetFilenamesPatternNode = data.Nodes.FirstOrDefault(n => n.Key == RemasteredTilesetFilenamesPattern.Key)
				?? defaults.Nodes.FirstOrDefault(n => n.Key == RemasteredTilesetFilenamesPattern.Key);

			if (remasteredTilesetFilenamesPatternNode != null)
			{
				var tilesetNode = remasteredTilesetFilenamesPatternNode.Value.Nodes.FirstOrDefault(n => n.Key == tileset);
				if (tilesetNode != null)
				{
					var patternStart = LoadField("Start", 0, tilesetNode.Value);
					var patternCount = LoadField("Count", 1, tilesetNode.Value);

					return Enumerable.Range(patternStart, patternCount).Select(i =>
						new ReservationInfo(string.Format(CultureInfo.InvariantCulture, tilesetNode.Value.Value, i), FirstFrame, FirstFrame, tilesetNode.Location));
				}
			}

			var remasteredTilesetFilenamesNode = data.Nodes.FirstOrDefault(n => n.Key == RemasteredTilesetFilenames.Key)
				?? defaults.Nodes.FirstOrDefault(n => n.Key == RemasteredTilesetFilenames.Key);

			if (!string.IsNullOrEmpty(remasteredTilesetFilenamesNode?.Value.Value))
			{
				var tilesetNode = remasteredTilesetFilenamesNode.Value.Nodes.FirstOrDefault(n => n.Key == tileset);
				if (tilesetNode != null)
				{
					filename = tilesetNode.Value.Value;
					location = tilesetNode.Location;
				}
			}
			else
			{
				var remasteredFilenamePatternNode = data.Nodes.FirstOrDefault(n => n.Key == RemasteredFilenamePattern.Key)
					?? defaults.Nodes.FirstOrDefault(n => n.Key == RemasteredFilenamePattern.Key);

				if (!string.IsNullOrEmpty(remasteredFilenamePatternNode?.Value.Value))
				{
					var patternStart = LoadField("Start", 0, remasteredFilenamePatternNode.Value);
					var patternCount = LoadField("Count", 1, remasteredFilenamePatternNode.Value);

					return Enumerable.Range(patternStart, patternCount).Select(i =>
						new ReservationInfo(string.Format(CultureInfo.InvariantCulture, remasteredFilenamePatternNode.Value.Value, i),
						FirstFrame, FirstFrame, remasteredFilenamePatternNode.Location));
				}
			}

			filename ??= LoadField(RemasteredFilename, data, defaults, out location);
			if (filename != null)
			{
				// Only request the subset of frames that we actually need
				var loadFrames = CalculateFrameIndices(start, length, stride ?? length ?? 0, facings, frames, transpose, reverseFacings, shadowStart);
				return [new ReservationInfo(filename, loadFrames, frames, location)];
			}
			else
			{
				hasRemasteredSprite = false;
				return ParseFilenames(modData, tileset, frames, data, defaults);
			}
		}

		IEnumerable<ReservationInfo> ParseRemasterCombineFilenames(ModData modData, string tileset, ImmutableArray<int> frames, MiniYaml data)
		{
			string filename = null;
			MiniYamlNode.SourceLocation location = default;

			var node = data.Nodes.FirstOrDefault(n => n.Key == RemasteredTilesetFilenames.Key);
			if (node != null)
			{
				var tilesetNode = node.Value.Nodes.FirstOrDefault(n => n.Key == tileset);
				if (tilesetNode != null)
				{
					filename = tilesetNode.Value.Value;
					location = tilesetNode.Location;
				}
			}

			filename ??= LoadField(RemasteredFilename, data, null, out location);
			if (frames == null && LoadField<string>("Length", null, data) != "*")
			{
				var subStart = LoadField("Start", 0, data);
				var subLength = LoadField("Length", 1, data);
				frames = Exts.MakeArray(subLength, i => subStart + i).ToImmutableArray();
			}

			if (filename != null)
			{
				return [new ReservationInfo(filename, frames, frames, location)];
			}
			else
			{
				hasRemasteredSprite = false;
				return ParseCombineFilenames(modData, tileset, frames, data);
			}
		}

		public RemasterSpriteSequence(SpriteCache cache, ISpriteSequenceLoader loader, string tileset, string image, string sequence,
			MiniYaml data, MiniYaml defaults, float classicUpscaleFactor)
			: base(cache, loader, image, sequence, data, defaults)
		{
			this.classicUpscaleFactor = classicUpscaleFactor;
			start = LoadField(RemasteredStart, data, defaults) ?? start;
			tick = LoadField(RemasteredTick, data, defaults) ?? tick;

			var node = data.Nodes.FirstOrDefault(n => n.Key == RemasteredTilesetScales.Key)
				?? defaults.Nodes.FirstOrDefault(n => n.Key == RemasteredTilesetScales.Key);

			var tilesetNode = node?.Value.Nodes.FirstOrDefault(n => n.Key == tileset);
			if (tilesetNode != null)
				scale = FieldLoader.GetValue<float>(tileset, tilesetNode.Value.Value);
			else
				scale = LoadField(RemasteredScale, data, defaults) ?? scale;

			var remasteredAlpha = LoadField(RemasteredAlpha, data, defaults);
			if (remasteredAlpha != default)
				alpha = remasteredAlpha;

			if (LoadField<string>(RemasteredLength.Key, null, data, defaults) != "*")
				length = LoadField(RemasteredLength, data, defaults) ?? length;
			else
				length = null;
		}

		public override void ReserveSprites(ModData modData, string tileset, SpriteCache cache, MiniYaml data, MiniYaml defaults)
		{
			var frames = LoadField(Frames, data, defaults);
			var flipX = LoadField(FlipX, data, defaults);
			var flipY = LoadField(FlipY, data, defaults);
			var zRamp = LoadField(ZRamp, data, defaults);
			var offset = LoadField(Offset, data, defaults);
			var remasteredOffset = LoadField(RemasteredOffset, data, defaults);
			var remasteredMaskFilename = LoadField(RemasteredMaskFilename, data, defaults, out var remasteredMaskFilenameLocation);
			var blendMode = LoadField(BlendMode, data, defaults);
			var premultiplied = LoadField(RemasteredPremultiplied, data, defaults);

			ISpriteFrame[] maskFrames = null;
			AdjustFrame adjustFrame = null;
			if (!string.IsNullOrEmpty(remasteredMaskFilename))
				adjustFrame = MaskFrame;

			ISpriteFrame MaskFrame(ISpriteFrame f, int index, int total)
			{
				if (maskFrames == null)
				{
					maskFrames = cache.LoadFramesUncached(remasteredMaskFilename);
					if (maskFrames == null)
						throw new FileNotFoundException($"{remasteredMaskFilenameLocation}: {remasteredMaskFilename} not found", remasteredMaskFilename);

					if (maskFrames.Length != total)
						throw new YamlException($"Sequence {image}.{Name} with {total} frames cannot use mask with {maskFrames.Length} frames.");
				}

				var m = maskFrames[index];
				if (f.Size != m.Size)
					throw new YamlException($"Sequence {image}.{Name} frame {index} with size {f.Size} frames cannot use mask with size {m.Size}.");

				if (m.Type != SpriteFrameType.Indexed8)
					throw new YamlException($"Sequence {image}.{Name} mask frame {index} must be an indexed image.");

				return new MaskedFrame(f, maskFrames[index]);
			}

			var combineNode = data.Nodes.FirstOrDefault(n => n.Key == Combine.Key);
			if (combineNode != null)
			{
				for (var i = 0; i < combineNode.Value.Nodes.Length; i++)
				{
					var subData = combineNode.Value.Nodes[i].Value;
					var subOffset = LoadField(Offset, subData, NoData);
					var remasteredSubOffset = LoadField(RemasteredOffset, subData, NoData);
					var subFlipX = LoadField(FlipX, subData, NoData);
					var subFlipY = LoadField(FlipY, subData, NoData);
					var subFrames = LoadField(Frames, data);

					foreach (var f in ParseRemasterCombineFilenames(modData, tileset, subFrames, subData))
					{
						var token = cache.ReserveSprites(f.Filename, f.LoadFrames, f.Location, adjustFrame, hasRemasteredSprite && premultiplied);

						spritesToLoad.Add(new SpriteReservation
						{
							Token = token,
							Offset = hasRemasteredSprite ? remasteredSubOffset + remasteredOffset : subOffset + offset,
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
				foreach (var f in ParseRemasterFilenames(modData, tileset, frames, data, defaults))
				{
					var token = cache.ReserveSprites(f.Filename, f.LoadFrames, f.Location, adjustFrame, hasRemasteredSprite && premultiplied);

					spritesToLoad.Add(new SpriteReservation
					{
						Token = token,
						Offset = hasRemasteredSprite ? remasteredOffset : offset,
						FlipX = flipX,
						FlipY = flipY,
						BlendMode = blendMode,
						ZRamp = zRamp,
						Frames = f.Frames,
					});
				}
			}
		}

		protected override float GetScale()
		{
			if (!hasRemasteredSprite)
				return classicUpscaleFactor * scale;

			return scale;
		}
	}
}
