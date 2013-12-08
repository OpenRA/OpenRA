#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Editor
{
	public class TileSetRenderer
	{
		public TileSet TileSet;
		Dictionary<ushort, List<byte[]>> templates;
		public Size TileSize;

		List<byte[]> LoadTemplate(string filename, string[] exts, Dictionary<string, ISpriteSource> sourceCache, int[] frames)
		{
			ISpriteSource source;
			if (!sourceCache.ContainsKey(filename))
			{
				using (var s = FileSystem.OpenWithExts(filename, exts))
					source = SpriteSource.LoadSpriteSource(s, filename);

				if (source.CacheWhenLoadingTileset)
					sourceCache.Add(filename, source);
			}
			else
				source = sourceCache[filename];

			if (frames != null)
			{
				var ret = new List<byte[]>();
				var srcFrames = source.Frames.ToArray();
				foreach (var i in frames)
					ret.Add(srcFrames[i].Data);

				return ret;
			}

			return source.Frames.Select(f => f.Data).ToList();
		}

		public TileSetRenderer(TileSet tileset, Size tileSize)
		{
			this.TileSet = tileset;
			this.TileSize = tileSize;

			templates = new Dictionary<ushort, List<byte[]>>();
			var sourceCache = new Dictionary<string, ISpriteSource>();
			foreach (var t in TileSet.Templates)
				templates.Add(t.Key, LoadTemplate(t.Value.Image, tileset.Extensions, sourceCache, t.Value.Frames));
		}

		public Bitmap RenderTemplate(ushort id, Palette p)
		{
			var template = TileSet.Templates[id];
			var templateData = templates[id];

			var bitmap = new Bitmap(TileSize.Width * template.Size.X, TileSize.Height * template.Size.Y,
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
							for (var i = 0; i < TileSize.Width; i++)
								for (var j = 0; j < TileSize.Height; j++)
									q[(v * TileSize.Width + j) * stride + u * TileSize.Width + i] = rawImage[i + TileSize.Width * j];
						}
						else
						{
							for (var i = 0; i < TileSize.Width; i++)
								for (var j = 0; j < TileSize.Height; j++)
									q[(v * TileSize.Width + j) * stride + u * TileSize.Width + i] = 0;
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
