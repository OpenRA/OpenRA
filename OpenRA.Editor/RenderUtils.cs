#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Traits;

namespace OpenRA.Editor
{
	static class RenderUtils
	{
		static Bitmap RenderShp(ShpTDSprite shp, IPalette p)
		{
			var frame = shp.Frames.First();

			var bitmap = new Bitmap(frame.Size.Width, frame.Size.Height, PixelFormat.Format8bppIndexed);

			bitmap.Palette = p.AsSystemPalette();

			var data = bitmap.LockBits(bitmap.Bounds(),
				ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

			unsafe
			{
				var q = (byte*)data.Scan0.ToPointer();
				var stride2 = data.Stride;

				for (var i = 0; i < frame.Size.Width; i++)
					for (var j = 0; j < frame.Size.Height; j++)
						q[j * stride2 + i] = frame.Data[i + frame.Size.Width * j];
			}

			bitmap.UnlockBits(data);
			return bitmap;
		}

		static readonly string[] LegacyExtensions = new[] { ".shp", ".tem", "" };

		static string ResolveFilename(string name, TileSet tileSet)
		{
			var ssl = Game.ModData.SpriteSequenceLoader as TilesetSpecificSpriteSequenceLoader;
			var extensions = ssl != null ? new[] { ssl.TilesetExtensions[tileSet.Id], ssl.DefaultSpriteExtension }.Append(LegacyExtensions) :
				LegacyExtensions.AsEnumerable();

			foreach (var e in extensions)
				if (GlobalFileSystem.Exists(name + e))
					return name + e;

			return name;
		}

		public static ActorTemplate RenderActor(ActorInfo info, SequenceProvider sequenceProvider, TileSet tileset, IPalette p, string race)
		{
			var image = info.Traits.Get<ILegacyEditorRenderInfo>().EditorImage(info, sequenceProvider, race);
			image = ResolveFilename(image, tileset);
			using (var s = GlobalFileSystem.Open(image))
			{
				var shp = new ShpTDSprite(s);
				var bitmap = RenderShp(shp, p);

				return new ActorTemplate
				{
					Bitmap = bitmap,
					Info = info,
					Appearance = info.Traits.GetOrDefault<EditorAppearanceInfo>()
				};
			}
		}

		public static ResourceTemplate RenderResourceType(ResourceTypeInfo info, TileSet tileset, IPalette p)
		{
			var image = ResolveFilename(info.EditorSprite, tileset);
			using (var s = GlobalFileSystem.Open(image))
			{
				// TODO: Do this properly
				var shp = new ShpTDSprite(s);
				var frame = shp.Frames.Last();

				var bitmap = new Bitmap(frame.Size.Width, frame.Size.Height, PixelFormat.Format8bppIndexed);
				bitmap.Palette = p.AsSystemPalette();
				var data = bitmap.LockBits(bitmap.Bounds(),
					ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

				unsafe
				{
					var q = (byte*)data.Scan0.ToPointer();
					var stride = data.Stride;

					for (var i = 0; i < frame.Size.Width; i++)
						for (var j = 0; j < frame.Size.Height; j++)
							q[j * stride + i] = frame.Data[i + frame.Size.Width * j];
				}

				bitmap.UnlockBits(data);
				return new ResourceTemplate { Bitmap = bitmap, Info = info, Value = shp.Frames.Count - 1 };
			}
		}
	}
}
