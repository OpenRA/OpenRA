#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;

namespace OpenRA.Graphics
{
	class TheaterTemplate
	{
		public readonly Sprite[] Sprites;
		public readonly int Stride;
		public readonly int Variants;

		public TheaterTemplate(Sprite[] sprites, int stride, int variants)
		{
			Sprites = sprites;
			Stride = stride;
			Variants = variants;
		}
	}

	public sealed class Theater : IDisposable
	{
		readonly Dictionary<ushort, TheaterTemplate> templates = new Dictionary<ushort, TheaterTemplate>();
		SheetBuilder sheetBuilder;
		readonly Sprite missingTile;
		readonly MersenneTwister random;
		TileSet tileset;

		public Theater(TileSet tileset, Action<uint, string> onMissingImage = null)
		{
			this.tileset = tileset;
			var allocated = false;

			Func<Sheet> allocate = () =>
			{
				if (allocated)
					throw new SheetOverflowException("Terrain sheet overflow. Try increasing the tileset SheetSize parameter.");
				allocated = true;

				return new Sheet(SheetType.Indexed, new Size(tileset.SheetSize, tileset.SheetSize));
			};

			random = new MersenneTwister();

			var frameCache = new FrameCache(Game.ModData.DefaultFileSystem, Game.ModData.SpriteLoaders);
			foreach (var t in tileset.Templates)
			{
				var variants = new List<Sprite[]>();

				foreach (var i in t.Value.Images)
				{
					ISpriteFrame[] allFrames;
					if (onMissingImage != null)
					{
						try
						{
							allFrames = frameCache[i];
						}
						catch (FileNotFoundException)
						{
							onMissingImage(t.Key, i);
							continue;
						}
					}
					else
						allFrames = frameCache[i];

					var frameCount = tileset.EnableDepth ? allFrames.Length / 2 : allFrames.Length;
					var indices = t.Value.Frames != null ? t.Value.Frames : Exts.MakeArray(t.Value.TilesCount, j => j);

					var start = indices.Min();
					var end = indices.Max();
					if (start < 0 || end >= frameCount)
						throw new YamlException("Template `{0}` uses frames [{1}..{2}] of {3}, but only [0..{4}] actually exist"
							.F(t.Key, start, end, i, frameCount - 1));

					variants.Add(indices.Select(j =>
					{
						var f = allFrames[j];
						var tile = t.Value.Contains(j) ? t.Value[j] : null;

						// The internal z axis is inverted from expectation (negative is closer)
						var zOffset = tile != null ? -tile.ZOffset : 0;
						var zRamp = tile != null ? tile.ZRamp : 1f;
						var offset = new float3(f.Offset, zOffset);
						var type = SheetBuilder.FrameTypeToSheetType(f.Type);

						// Defer SheetBuilder creation until we know what type of frames we are loading!
						// TODO: Support mixed indexed and BGRA frames
						if (sheetBuilder == null)
							sheetBuilder = new SheetBuilder(SheetBuilder.FrameTypeToSheetType(f.Type), allocate);
						else if (type != sheetBuilder.Type)
							throw new YamlException("Sprite type mismatch. Terrain sprites must all be either Indexed or RGBA.");

						var s = sheetBuilder.Allocate(f.Size, zRamp, offset);
						Util.FastCopyIntoChannel(s, f.Data, f.Type);

						if (tileset.EnableDepth)
						{
							var ss = sheetBuilder.Allocate(f.Size, zRamp, offset);
							var depthFrame = allFrames[j + frameCount];
							Util.FastCopyIntoChannel(ss, depthFrame.Data, depthFrame.Type);

							// s and ss are guaranteed to use the same sheet
							// because of the custom terrain sheet allocation
							s = new SpriteWithSecondaryData(s, s.Sheet, ss.Bounds, ss.Channel);
						}

						return s;
					}).ToArray());
				}

				var allSprites = variants.SelectMany(s => s);

				// Ignore the offsets baked into R8 sprites
				if (tileset.IgnoreTileSpriteOffsets)
					allSprites = allSprites.Select(s => new Sprite(s.Sheet, s.Bounds, s.ZRamp, new float3(float2.Zero, s.Offset.Z), s.Channel, s.BlendMode));

				if (onMissingImage != null && !variants.Any())
					continue;

				templates.Add(t.Value.Id, new TheaterTemplate(allSprites.ToArray(), variants.First().Count(), t.Value.Images.Length));
			}

			// 1x1px transparent tile
			if (sheetBuilder.Type == SheetType.BGRA)
				missingTile = sheetBuilder.Add(new byte[4], SpriteFrameType.Bgra32, new Size(1, 1));
			else
				missingTile = sheetBuilder.Add(new byte[1], SpriteFrameType.Indexed8, new Size(1, 1));

			Sheet.ReleaseBuffer();
		}

		public bool HasTileSprite(TerrainTile r, int? variant = null)
		{
			return TileSprite(r, variant) != missingTile;
		}

		public Sprite TileSprite(TerrainTile r, int? variant = null)
		{
			if (!templates.TryGetValue(r.Type, out var template))
				return missingTile;

			if (r.Index >= template.Stride)
				return missingTile;

			var start = template.Variants > 1 ? variant.HasValue ? variant.Value : random.Next(template.Variants) : 0;
			return template.Sprites[start * template.Stride + r.Index];
		}

		public Rectangle TemplateBounds(TerrainTemplateInfo template, Size tileSize, MapGridType mapGrid)
		{
			Rectangle? templateRect = null;

			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++)
				{
					var tile = new TerrainTile(template.Id, (byte)(i++));
					if (!tileset.TryGetTileInfo(tile, out var tileInfo))
						continue;

					var sprite = TileSprite(tile);
					var u = mapGrid == MapGridType.Rectangular ? x : (x - y) / 2f;
					var v = mapGrid == MapGridType.Rectangular ? y : (x + y) / 2f;

					var tl = new float2(u * tileSize.Width, (v - 0.5f * tileInfo.Height) * tileSize.Height) - 0.5f * sprite.Size;
					var rect = new Rectangle((int)(tl.X + sprite.Offset.X), (int)(tl.Y + sprite.Offset.Y), (int)sprite.Size.X, (int)sprite.Size.Y);
					templateRect = templateRect.HasValue ? Rectangle.Union(templateRect.Value, rect) : rect;
				}
			}

			return templateRect.HasValue ? templateRect.Value : Rectangle.Empty;
		}

		public Sheet Sheet { get { return sheetBuilder.Current; } }

		public void Dispose()
		{
			sheetBuilder.Dispose();
		}
	}
}
