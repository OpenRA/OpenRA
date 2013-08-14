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
using System.Reflection;

namespace OpenRA.FileFormats
{
	public class TileSetRenderer
	{
		public TileSet TileSet;
		Dictionary<ushort, List<byte[]>> templates;
		public Size TileSize;

		public TileSetRenderer(TileSet tileset, Size tileSize)
		{
			this.TileSet = tileset;
			this.TileSize = tileSize;

			templates = new Dictionary<ushort, List<byte[]>>();

			foreach (var t in TileSet.Templates)
			using (var s = FileSystem.OpenWithExts(t.Value.Image, tileset.Extensions))
				templates.Add(t.Key, new Terrain(s).TileBitmapBytes);
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
					for (var v = 0; v < template.Size.Y; v++)
						if (templateData[u + v * template.Size.X] != null)
						{
							var rawImage = templateData[u + v * template.Size.X];
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

			bitmap.UnlockBits(data);
			return bitmap;
		}

		public List<byte[]> Data(ushort id)
		{
			return templates[id];
		}
	}
}
