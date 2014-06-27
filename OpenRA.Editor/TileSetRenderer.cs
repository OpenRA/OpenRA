#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;

namespace OpenRA.Editor
{
	public class TileSetRenderer
	{
		public readonly int TileSize;
		public TileSet TileSet;
		Dictionary<ushort, List<byte[]>> templates;

		// Extract a square tile that the editor can render
		byte[] ExtractSquareTile(ISpriteFrame frame)
		{
			var data = new byte[TileSize * TileSize];

			// Invalid tile size: return blank tile
			if (frame.Size.Width < TileSize || frame.Size.Height < TileSize)
				return new byte[0];

			var frameData = frame.Data;
			var xOffset = (frame.Size.Width - TileSize) / 2;
			var yOffset = (frame.Size.Height - TileSize) / 2;

			for (var y = 0; y < TileSize; y++)
				for (var x = 0; x < TileSize; x++)
					data[y * TileSize + x] = frameData[(yOffset + y) * frame.Size.Width + x + xOffset];

			return data;
		}

		List<byte[]> LoadTemplate(string filename, string[] exts, Dictionary<string, ISpriteSource> sourceCache, int[] frames)
		{
			ISpriteSource source;
			if (!sourceCache.ContainsKey(filename))
			{
				using (var s = GlobalFileSystem.OpenWithExts(filename, exts))
					source = SpriteSource.LoadSpriteSource(s, filename);

				if (source.CacheWhenLoadingTileset)
					sourceCache.Add(filename, source);
			}
			else
				source = sourceCache[filename];

			if (frames != null)
			{
				var ret = new List<byte[]>();
				var srcFrames = source.Frames;
				foreach (var i in frames)
					ret.Add(ExtractSquareTile(srcFrames[i]));

				return ret;
			}

			return source.Frames.Select(f => ExtractSquareTile(f)).ToList();
		}

		public TileSetRenderer(TileSet tileset, Size tileSize)
		{
			this.TileSet = tileset;
			this.TileSize = Math.Min(tileSize.Width, tileSize.Height);

			templates = new Dictionary<ushort, List<byte[]>>();
			var sourceCache = new Dictionary<string, ISpriteSource>();
			foreach (var t in TileSet.Templates)
				templates.Add(t.Key, LoadTemplate(t.Value.Image, tileset.Extensions, sourceCache, t.Value.Frames));
		}

		public Bitmap RenderTemplate(ushort id, Palette p)
		{
			var template = TileSet.Templates[id];
			var templateData = templates[id];

			var bitmap = new Bitmap(TileSize * template.Size.X, TileSize * template.Size.Y,
				PixelFormat.Format8bppIndexed);

			bitmap.Palette = p.AsSystemPalette();

			var data = bitmap.LockBits(bitmap.Bounds(),
				ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

			unsafe
			{
				var q = (byte*)data.Scan0.ToPointer();
				var stride = data.Stride;

				for (var u = 0; u < template.Size.X; u++)
				{
					for (var v = 0; v < template.Size.Y; v++)
					{
						var rawImage = templateData[u + v * template.Size.X];
						if (rawImage != null && rawImage.Length > 0)
						{
							for (var i = 0; i < TileSize; i++)
								for (var j = 0; j < TileSize; j++)
									q[(v * TileSize + j) * stride + u * TileSize + i] = rawImage[i + TileSize * j];
						}
						else
						{
							for (var i = 0; i < TileSize; i++)
								for (var j = 0; j < TileSize; j++)
									q[(v * TileSize + j) * stride + u * TileSize + i] = 0;
						}
					}
				}
			}

			bitmap.UnlockBits(data);
			return bitmap;
		}

		public List<byte[]> Data(ushort id)
		{
			return templates[id];
		}
	}
}
