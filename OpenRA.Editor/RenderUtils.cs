#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using System.Drawing.Imaging;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Editor
{
	static class RenderUtils
	{
		public static ColorPalette MakeSystemPalette(Palette p)
		{
			ColorPalette pal;
			using (var b = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
				pal = b.Palette;
			
			for (var i = 0; i < 256; i++)
				pal.Entries[i] = p.GetColor(i);
			return pal;
		}

		public static Bitmap RenderTemplate(TileSet ts, ushort n, Palette p)
		{
			var template = ts.Templates[n];
			var tile = ts.Tiles[n];

			var bitmap = new Bitmap(ts.TileSize * template.Size.X, ts.TileSize * template.Size.Y,
				PixelFormat.Format8bppIndexed);

			bitmap.Palette = MakeSystemPalette(p);

			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

			unsafe
			{
				byte* q = (byte*)data.Scan0.ToPointer();
				var stride = data.Stride;

				for (var u = 0; u < template.Size.X; u++)
					for (var v = 0; v < template.Size.Y; v++)
						if (tile.TileBitmapBytes[u + v * template.Size.X] != null)
						{
							var rawImage = tile.TileBitmapBytes[u + v * template.Size.X];
							for (var i = 0; i < ts.TileSize; i++)
								for (var j = 0; j < ts.TileSize; j++)
									q[(v * ts.TileSize + j) * stride + u * ts.TileSize + i] = rawImage[i + ts.TileSize * j];
						}
						else
						{
							for (var i = 0; i < ts.TileSize; i++)
								for (var j = 0; j < ts.TileSize; j++)
									q[(v * ts.TileSize + j) * stride + u * ts.TileSize + i] = 0;
						}
			}

			bitmap.UnlockBits(data);
			return bitmap;
		}

		static Bitmap RenderShp(ShpReader shp, Palette p)
		{
			var frame = shp[0];

			var bitmap = new Bitmap(shp.Width, shp.Height, PixelFormat.Format8bppIndexed);

			bitmap.Palette = MakeSystemPalette(p);

			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

			unsafe
			{
				byte* q = (byte*)data.Scan0.ToPointer();
				var stride2 = data.Stride;

				for (var i = 0; i < shp.Width; i++)
					for (var j = 0; j < shp.Height; j++)
						q[j * stride2 + i] = frame.Image[i + shp.Width * j];
			}

			bitmap.UnlockBits(data);
			return bitmap;
		}

		public static ActorTemplate RenderActor(ActorInfo info, TileSet tileset, Palette p)
		{
			var ri = info.Traits.Get<RenderSimpleInfo>();
			string image = null;
			if (ri.OverrideTheater != null)
				for (int i = 0; i < ri.OverrideTheater.Length; i++)
					if (ri.OverrideTheater[i] == tileset.Id)
						image = ri.OverrideImage[i];

			image = image ?? ri.Image ?? info.Name;
			using (var s = FileSystem.OpenWithExts(image, tileset.Extensions))
			{
				var shp = new ShpReader(s);
				var bitmap = RenderShp(shp, p);

				try
				{
					using (var s2 = FileSystem.OpenWithExts(image + "2", tileset.Extensions))
					{
						var shp2 = new ShpReader(s2);
						var roofBitmap = RenderShp(shp2, p);

						using (var g = System.Drawing.Graphics.FromImage(bitmap))
							g.DrawImage(roofBitmap, 0, 0);
					}
				}
				catch { }

				return new ActorTemplate { Bitmap = bitmap, Info = info, Centered = !info.Traits.Contains<BuildingInfo>() };
			}
		}

		public static ResourceTemplate RenderResourceType(ResourceTypeInfo info, string[] exts, Palette p)
		{
			var image = info.SpriteNames[0];
			using (var s = FileSystem.OpenWithExts(image, exts))
			{
				var shp = new ShpReader(s);
				var frame = shp[shp.ImageCount - 1];

				var bitmap = new Bitmap(shp.Width, shp.Height, PixelFormat.Format8bppIndexed);
				bitmap.Palette = MakeSystemPalette(p);
				var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

				unsafe
				{
					byte* q = (byte*)data.Scan0.ToPointer();
					var stride = data.Stride;

					for (var i = 0; i < shp.Width; i++)
						for (var j = 0; j < shp.Height; j++)
							q[j * stride + i] = frame.Image[i + shp.Width * j];
				}

				bitmap.UnlockBits(data);
				return new ResourceTemplate { Bitmap = bitmap, Info = info, Value = shp.ImageCount - 1 };
			}
		}
	}
}
