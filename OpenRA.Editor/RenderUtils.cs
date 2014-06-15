#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Editor
{
	static class RenderUtils
	{
		static Bitmap RenderShp(ISpriteSource shp, Palette p)
		{
			var frame = shp.Frames.First();

			var bitmap = new Bitmap(frame.Size.Width, frame.Size.Height, PixelFormat.Format8bppIndexed);

			bitmap.Palette = p.AsSystemPalette();

			var data = bitmap.LockBits(bitmap.Bounds(),
				ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

			unsafe
			{
				byte* q = (byte*)data.Scan0.ToPointer();
				var stride2 = data.Stride;

				for (var i = 0; i < frame.Size.Width; i++)
					for (var j = 0; j < frame.Size.Height; j++)
						q[j * stride2 + i] = frame.Data[i + frame.Size.Width * j];
			}

			bitmap.UnlockBits(data);
			return bitmap;
		}

		public static ActorTemplate RenderActor(ActorInfo info, TileSet tileset, Palette p)
		{
			var image = RenderSprites.GetImage(info);

			using (var s = GlobalFileSystem.OpenWithExts(image, tileset.Extensions))
			{
				var shp = new ShpReader(s);
				var bitmap = RenderShp(shp, p);

				try
				{
					using (var s2 = GlobalFileSystem.OpenWithExts(image + "2", tileset.Extensions))
					{
						var shp2 = new ShpReader(s2);
						var roofBitmap = RenderShp(shp2, p);

						using (var g = System.Drawing.Graphics.FromImage(bitmap))
							g.DrawImage(roofBitmap, 0, 0);
					}
				}
				catch { }

				return new ActorTemplate
				{
					Bitmap = bitmap,
					Info = info,
					Appearance = info.Traits.GetOrDefault<EditorAppearanceInfo>()
				};
			}
		}

		public static ResourceTemplate RenderResourceType(ResourceTypeInfo info, string[] exts, Palette p)
		{
			var image = info.EditorSprite;
			using (var s = GlobalFileSystem.OpenWithExts(image, exts))
			{
				// TODO: Do this properly
				var shp = new ShpReader(s) as ISpriteSource;
				var frame = shp.Frames.Last();

				var bitmap = new Bitmap(frame.Size.Width, frame.Size.Height, PixelFormat.Format8bppIndexed);
				bitmap.Palette = p.AsSystemPalette();
				var data = bitmap.LockBits(bitmap.Bounds(),
					ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

				unsafe
				{
					byte* q = (byte*)data.Scan0.ToPointer();
					var stride = data.Stride;

					for (var i = 0; i < frame.Size.Width; i++)
						for (var j = 0; j < frame.Size.Height; j++)
							q[j * stride + i] = frame.Data[i + frame.Size.Width * j];
				}

				bitmap.UnlockBits(data);
				return new ResourceTemplate { Bitmap = bitmap, Info = info, Value = shp.Frames.Count() - 1 };
			}
		}
	}
}
