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
using System.Windows.Forms;
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
		
		static Palette LoadPalette( string filename )
		{
			using( var s = File.OpenRead( filename ) )
				return new Palette( s, false );
		}

		public static void ConvertShpToPng(string[] args)
		{
			var src = args[1];
			var dest = Path.ChangeExtension(src, ".png");

			var srcImage = ShpReader.Load(src);
			var palette = LoadPalette(args[2]);

			using (var bitmap = new Bitmap(srcImage.ImageCount * srcImage.Width, srcImage.Height, PixelFormat.Format8bppIndexed))
			{
				var x = 0;
				bitmap.Palette = palette.AsSystemPalette();

				foreach (var frame in srcImage)
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
	}
}
