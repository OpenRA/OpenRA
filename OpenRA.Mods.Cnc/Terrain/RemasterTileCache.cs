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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using OpenRA.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.Terrain
{
	public sealed class RemasterTileCache : IDisposable
	{
		static readonly int[] ChannelsBGRA = [0, 1, 2, 3];
		static readonly int[] ChannelsRGBA = [2, 1, 0, 3];
		static readonly ImmutableArray<int> FirstFrame = [0];

		readonly Dictionary<ushort, Dictionary<int, Sprite[]>> sprites = [];
		readonly Dictionary<ushort, float> scale = [];
		public SpriteCache SpriteCache { get; }
		Sprite BlankSprite { get; }

		Sprite GenerateCompositeFrame(string path)
		{
			var frames = FrameLoader.GetFrames(Game.ModData.DefaultFileSystem, path, Game.ModData.SpriteLoaders, out var allMetadata);
			var frame = frames[0];

			// Composite frames are assembled at runtime from one or more source images
			var metadata = allMetadata?.GetOrDefault<PngSheetMetadata>();
			if (frame.Type != SpriteFrameType.Indexed8 || metadata == null)
				throw new InvalidDataException($"{path} is not a valid composite tile template.");

			// Preload source assets to avoid duplicated loading
			var sourceFrames = new Dictionary<string, ISpriteFrame[]>();
			for (var i = 0; i < frames.Length; i++)
			{
				if (!metadata.Metadata.TryGetValue($"SourceFilename[{i}]", out var sf))
					throw new InvalidDataException($"{path}: SourceFilename[{i}] not defined.");

				try
				{
					if (!sourceFrames.ContainsKey(sf))
						sourceFrames.Add(sf, FrameLoader.GetFrames(Game.ModData.DefaultFileSystem, sf, Game.ModData.SpriteLoaders, out _));
				}
				catch (FileNotFoundException)
				{
					throw new InvalidDataException($"{path}: SourceFilename[{i}]={sf} does not exist.");
				}
			}

			var data = new byte[frame.Size.Width * frame.Size.Height * 4];
			for (var i = 0; i < frames.Length; i++)
			{
				var f = 0;
				if (metadata.Metadata.TryGetValue($"SourceFrame[{i}]", out var sfr))
					f = FieldLoader.GetValue<int>($"SourceFrame[{i}]", sfr);

				var source = sourceFrames[metadata.Metadata[$"SourceFilename[{i}]"]][f];
				if (source.Type != SpriteFrameType.Bgra32 && source.Type != SpriteFrameType.Rgba32)
					throw new InvalidDataException($"{path}: SourceFilename[{i}] is not a 32 bit sprite.");

				var channels = source.Type == SpriteFrameType.Bgra32 ? ChannelsBGRA : ChannelsRGBA;
				var sWidth = source.Size.Width;
				var dWidth = frames[i].Size.Width;
				var sRect = new Rectangle(int2.Zero, source.Size);
				var dRect = new Rectangle(int2.Zero, frames[i].Size);

				if (metadata.Metadata.TryGetValue($"SourceOffset[{i}]", out var so))
				{
					var offset = FieldLoader.GetValue<int2>($"SourceOffset[{i}]", so);

					// Constrain source and dest rectangles to their offset union
					var sl = sRect.Left.Clamp(dRect.Left - offset.X, dRect.Right - offset.X);
					var sr = sRect.Right.Clamp(dRect.Left - offset.X, dRect.Right - offset.X);
					var st = sRect.Top.Clamp(dRect.Top - offset.Y, dRect.Bottom - offset.Y);
					var sb = sRect.Bottom.Clamp(dRect.Top - offset.Y, dRect.Bottom - offset.Y);
					sRect = Rectangle.FromLTRB(sl, st, sr, sb);
					dRect = new Rectangle(sRect.Location + offset, sRect.Size);
				}

				List<float[]> colorShifts = null;
				if (metadata.Metadata.ContainsKey($"ColorShift[{i}][0]"))
				{
					colorShifts = [];
					while (metadata.Metadata.TryGetValue($"ColorShift[{i}][{colorShifts.Count}]", out var cs))
					{
						var shift = FieldLoader.GetValue<float[]>($"ColorShift[{i}][{colorShifts.Count}]", cs);
						if (shift.Length != 5)
							throw new InvalidDataException($"{path}: ColorShift[{i}][{colorShifts.Count}] must define 5 entries.");

						colorShifts.Add(shift);
					}
				}

				for (var y = 0; y < dRect.Size.Height; y++)
				{
					var sy = y + sRect.Y;
					var dy = y + dRect.Y;
					for (var x = 0; x < dRect.Size.Width; x++)
					{
						var si = 4 * (sy * sWidth + x + sRect.X);
						var di = 4 * (dy * dWidth + x + dRect.X);

						// The mask is defined in the destination coordinate space
						var maskAlpha = frames[i].Data[dy * frames[i].Size.Width + x + dRect.X];

						// We want to pre-multiply the colour channels by the alpha channel,
						// but not the alpha channel itself. The simplest way to do this is to
						// always include the overlay alpha in the alpha component, and
						// special-case the alpha's channel value instead.
						var b = source.Data[si + channels[0]];
						var g = source.Data[si + channels[1]];
						var r = source.Data[si + channels[2]];
						var overlayAlpha = source.Data[si + 3] * maskAlpha;

						if (colorShifts != null)
						{
							var (lr, lg, lb) = Color.FromArgb(255, r, g, b).ToLinear();
							var (h, s, v) = Color.RgbToHsv(lr, lg, lb);

							var updated = false;
							foreach (var cs in colorShifts)
							{
								if (h <= cs[3] || h > cs[4])
									continue;

								h = (h + cs[0]) % 1.0f;
								s = (s + cs[1]).Clamp(0, 1);
								v *= cs[2].Clamp(0, 1);
								updated = true;
							}

							if (updated)
							{
								(lr, lg, lb) = Color.HsvToRgb(h, s, v);
								var c = Color.FromLinear(255, lr, lg, lb);
								r = c.R;
								g = c.G;
								b = c.B;
							}
						}

						// Base channels have already been pre-multiplied by alpha
						var baseAlpha = 65205 - overlayAlpha;
						data[di] = (byte)((b * overlayAlpha + data[di++] * baseAlpha) / 65205);
						data[di] = (byte)((g * overlayAlpha + data[di++] * baseAlpha) / 65205);
						data[di] = (byte)((r * overlayAlpha + data[di++] * baseAlpha) / 65205);
						data[di] = (byte)((255 * overlayAlpha + data[di] * baseAlpha) / 65205);
					}
				}
			}

			var sc = SpriteCache.SheetBuilders[SheetType.BGRA].Allocate(frame.Size, 1f, frame.Offset.AsVector3());
			OpenRA.Graphics.Util.FastCopyIntoChannel(sc, data, SpriteFrameType.Bgra32);
			return sc;
		}

		public RemasterTileCache(RemasterTerrain terrainInfo)
		{
			SpriteCache = new SpriteCache(Game.ModData.DefaultFileSystem, Game.ModData.SpriteLoaders, terrainInfo.BgraSheetSize, terrainInfo.IndexedSheetSize, 0);

			var blankToken = SpriteCache.ReserveSprites(terrainInfo.BlankTile, FirstFrame, default);
			var classicUpscaleFactor = terrainInfo.UseRemasteredTerrain ? terrainInfo.RemasteredTileSize.Width * 1f / terrainInfo.TileSize.Width : 1;

			var remasteredSpriteReservations = new Dictionary<ushort, Dictionary<int, int[]>>();
			foreach (var t in terrainInfo.Templates)
			{
				var templateInfo = (RemasterTerrainTemplateInfo)t.Value;
				var templateTokens = new Dictionary<int, int[]>();
				sprites[t.Key] = [];

				if (terrainInfo.UseRemasteredTerrain && (templateInfo.RemasteredFilenames != null || templateInfo.RemasteredCompositeFilenames != null))
				{
					// Composite frames are built directly, bypassing the frame cache
					if (templateInfo.RemasteredCompositeFilenames != null)
						foreach (var kv in templateInfo.RemasteredCompositeFilenames)
							sprites[t.Key][kv.Key] = kv.Value.Select(GenerateCompositeFrame).ToArray();

					if (templateInfo.RemasteredFilenames != null)
						foreach (var kv in templateInfo.RemasteredFilenames)
							templateTokens[kv.Key] = kv.Value
								.Select(f => SpriteCache.ReserveSprites(f, FirstFrame, default))
								.ToArray();

					scale[t.Key] = 1f;
				}
				else
				{
					for (var i = 0; i < t.Value.TilesCount; i++)
					{
						if (t.Value[i] == null)
							continue;

						templateTokens[i] = [SpriteCache.ReserveSprites(templateInfo.Filename, [i], default)];
					}

					scale[t.Key] = classicUpscaleFactor;
				}

				remasteredSpriteReservations[t.Key] = templateTokens;
			}

			SpriteCache.LoadReservations(Game.ModData);

			BlankSprite = SpriteCache.ResolveSprites(blankToken).First(s => s != null);
			foreach (var kv in remasteredSpriteReservations)
				foreach (var tokens in kv.Value)
					sprites[kv.Key][tokens.Key] = tokens.Value
						.Select(t => SpriteCache.ResolveSprites(t).FirstOrDefault(s => s != null))
						.ToArray();
		}

		public bool HasTileSprite(TerrainTile r, int frame)
		{
			return TileSprite(r, frame) != BlankSprite;
		}

		public Sprite TileSprite(TerrainTile r, int frame)
		{
			if (!sprites.TryGetValue(r.Type, out var templateSprites))
				return BlankSprite;

			if (!templateSprites.TryGetValue(r.Index, out var tileSprites))
				return BlankSprite;

			return tileSprites[frame % tileSprites.Length];
		}

		public float TileScale(TerrainTile r)
		{
			if (!scale.TryGetValue(r.Type, out var templateScale))
				return 1f;

			return templateScale;
		}

		public Sprite MissingTile => BlankSprite;

		public void Dispose()
		{
			SpriteCache.Dispose();
		}
	}
}
