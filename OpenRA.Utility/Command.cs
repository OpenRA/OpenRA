#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;

namespace OpenRA.Utility
{
	static class Command
	{
		public static void Settings(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}

			var section = args[1].Split('.')[0];
			var field = args[1].Split('.')[1];
			var settings = new Settings(Platform.SupportDir + "settings.yaml", Arguments.Empty);
			var result = settings.Sections[section].GetType().GetField(field).GetValue(settings.Sections[section]);
			Console.WriteLine(result);
		}

		public static void ConvertPngToShp(string[] args)
		{
			var src = args[1];
			var dest = Path.ChangeExtension(src, ".shp");
			var width = int.Parse(args[2]);

			var srcImage = PngLoader.Load(src);

			if (srcImage.Width % width != 0)
				throw new InvalidOperationException("Bogus width; not a whole number of frames");

			using (var destStream = File.Create(dest))
				ShpWriter.Write(destStream, width, srcImage.Height,
					srcImage.ToFrames(width));
		}

		static IEnumerable<byte[]> ToFrames(this Bitmap bitmap, int width)
		{
			for (var x = 0; x < bitmap.Width; x += width)
			{
				var data = bitmap.LockBits(new Rectangle(x, 0, width, bitmap.Height), ImageLockMode.ReadOnly,
					PixelFormat.Format8bppIndexed);

				var bytes = new byte[width * bitmap.Height];
				for (var i = 0; i < bitmap.Height; i++)
					Marshal.Copy(new IntPtr(data.Scan0.ToInt64() + i * data.Stride),
						bytes, i * width, width);

				bitmap.UnlockBits(data);

				yield return bytes;
			}
		}

		public static void ConvertShpToPng(string[] args)
		{
			var src = args[1];
			var dest = Path.ChangeExtension(src, ".png");

			var srcImage = ShpReader.Load(src);
			var shouldRemap = args.Contains( "--transparent" );
			var palette = Palette.Load(args[2], shouldRemap);

			using (var bitmap = new Bitmap(srcImage.ImageCount * srcImage.Width, srcImage.Height, PixelFormat.Format8bppIndexed))
			{
				var x = 0;
				bitmap.Palette = palette.AsSystemPalette();

				foreach (var frame in srcImage.Frames)
				{
					var data = bitmap.LockBits(new Rectangle(x, 0, srcImage.Width, srcImage.Height), ImageLockMode.WriteOnly,
						PixelFormat.Format8bppIndexed);

					for (var i = 0; i < bitmap.Height; i++)
						Marshal.Copy(frame.Image, i * srcImage.Width,
							new IntPtr(data.Scan0.ToInt64() + i * data.Stride), srcImage.Width);

					x += srcImage.Width;

					bitmap.UnlockBits( data );
				}

				bitmap.Save(dest);
			}
		}

		public static void ConvertTmpToPng(string[] args)
		{
			var mods = args[1].Split(',');
			var theater = args[2];
			var templateNames = args.Skip(3);

			var manifest = new Manifest(mods);
			FileSystem.LoadFromManifest(manifest);

			var tileset = manifest.TileSets.Select( a => new TileSet(a) )
				.FirstOrDefault( ts => ts.Name == theater );

			if (tileset == null)
				throw new InvalidOperationException("No theater named '{0}'".F(theater));

			tileset.LoadTiles();
			var palette = new Palette(FileSystem.Open(tileset.Palette), true);

			foreach( var templateName in templateNames )
			{
				var template = tileset.Templates.FirstOrDefault(tt => tt.Value.Image == templateName);
				if (template.Value == null)
					throw new InvalidOperationException("No such template '{0}'".F(templateName));

				using( var image = tileset.RenderTemplate(template.Value.Id, palette) )
					image.Save( Path.ChangeExtension( templateName, ".png" ) );
			}
		}

		public static void ConvertFormat2ToFormat80(string[] args)
		{
			var src = args[1];
			var dest = args[2];

			Dune2ShpReader srcImage = null;
			using( var s = File.OpenRead( src ) )
				srcImage = new Dune2ShpReader(s);

			var size = srcImage.First().Size;

			if (!srcImage.All( im => im.Size == size ))
				throw new InvalidOperationException("All the frames must be the same size to convert from Dune2 to RA");

			using( var destStream = File.Create(dest) )
				ShpWriter.Write(destStream, size.Width, size.Height,
					srcImage.Select( im => im.Image ));
		}

		public static void ExtractFiles(string[] args)
		{
			var mods = args[1].Split(',');
			var files = args.Skip(2);

			var manifest = new Manifest(mods);
			FileSystem.LoadFromManifest(manifest);

			foreach( var f in files )
			{
				var src = FileSystem.Open(f);
				if (src == null)
					throw new InvalidOperationException("File not found: {0}".F(f));
				var data = src.ReadAllBytes();

				File.WriteAllBytes( f, data );
			}
		}

		static int ColorDistance(uint a, uint b)
		{
			var ca = Color.FromArgb((int)a);
			var cb = Color.FromArgb((int)b);

			return Math.Abs((int)ca.R - (int)cb.R) +
				Math.Abs((int)ca.G - (int)cb.G) +
				Math.Abs((int)ca.B - (int)cb.B);
		}

		public static void RemapShp(string[] args)
		{
			var remap = new Dictionary<int,int>();

			/* the first 4 entries are fixed */
			for( var i = 0; i < 4; i++ )
				remap[i] = i;

			var srcPaletteType = Enum<PaletteFormat>.Parse(args[1].Split(':')[0]);
			var destPaletteType = Enum<PaletteFormat>.Parse(args[2].Split(':')[0]);

			/* the remap range is always 16 entries, but their location and order changes */
			for( var i = 0; i < 16; i++ )
				remap[ PlayerColorRemap.GetRemapIndex(srcPaletteType, i) ]
					= PlayerColorRemap.GetRemapIndex(destPaletteType, i);

			/* map everything else to the best match based on channel-wise distance */
			var srcPalette = Palette.Load(args[1].Split(':')[1], false);
			var destPalette = Palette.Load(args[2].Split(':')[1], false);

			var fullIndexRange = Exts.MakeArray<int>(256, x => x);

			for( var i = 0; i < 256; i++ )
				if (!remap.ContainsKey(i))
					remap[i] = fullIndexRange
						.Where(a => !remap.ContainsValue(a))
						.OrderBy(a => ColorDistance(destPalette.Values[a], srcPalette.Values[i]))
						.First();

			var srcImage = ShpReader.Load(args[3]);

			using( var destStream = File.Create(args[4]) )
				ShpWriter.Write(destStream, srcImage.Width, srcImage.Height,
					srcImage.Frames.Select( im => im.Image.Select(px => (byte)remap[px]).ToArray() ));
		}
	}
}
