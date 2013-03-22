#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
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
using System.Reflection;
using System.Runtime.InteropServices;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Utility
{
	public static class Command
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

			Console.WriteLine(dest+" saved");
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
			int[] ShadowIndex = { };
			if (args.Contains("--noshadow"))
			{
					Array.Resize(ref ShadowIndex, ShadowIndex.Length + 3);
					ShadowIndex[ShadowIndex.Length - 1] = 1;
					ShadowIndex[ShadowIndex.Length - 2] = 3;
					ShadowIndex[ShadowIndex.Length - 1] = 4;
			}
			var palette = Palette.Load(args[2], ShadowIndex);

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

		public static void ConvertR8ToPng(string[] args)
		{
			var srcImage = new R8Reader(File.OpenRead(args[1]));
			int[] ShadowIndex = { };
			if (args.Contains("--noshadow"))
			{
					Array.Resize(ref ShadowIndex, ShadowIndex.Length + 1);
					ShadowIndex[ShadowIndex.Length - 1] = 3;
			}
			var palette = Palette.Load(args[2], ShadowIndex);
			var startFrame = int.Parse(args[3]);
			var endFrame = int.Parse(args[4]) + 1;
			var filename = args[5];
			var FrameCount = endFrame - startFrame;

			var frame = srcImage[startFrame];
			var bitmap = new Bitmap(frame.FrameWidth * FrameCount, frame.FrameHeight, PixelFormat.Format8bppIndexed);
			bitmap.Palette = palette.AsSystemPalette();

			int OffsetX = 0;
			int OffsetY = 0;

			int x = 0;

			frame = srcImage[startFrame];

			if (args.Contains("--infantry")) //resorting to RA/CnC compatible counter-clockwise frame order
			{
				endFrame = startFrame-1;
				for (int e = 8; e < FrameCount+1; e=e+8) //assuming 8 facings each animation set
				{
					
					for (int f = startFrame+e-1; f > endFrame; f--)
					{
						OffsetX = frame.FrameWidth/2 - frame.Width/2;
						OffsetY = frame.FrameHeight/2 - frame.Height/2;

						Console.WriteLine("calculated OffsetX: {0}", OffsetX);
						Console.WriteLine("calculated OffsetY: {0}", OffsetY);

						var data = bitmap.LockBits(new Rectangle(x+OffsetX, 0+OffsetY, frame.Width, frame.Height), ImageLockMode.WriteOnly,
							PixelFormat.Format8bppIndexed);

						for (var i = 0; i < frame.Height; i++)
							Marshal.Copy(frame.Image, i * frame.Width,
								new IntPtr(data.Scan0.ToInt64() + i * data.Stride), frame.Width);

						bitmap.UnlockBits(data);

						x += frame.FrameWidth;

						frame = srcImage[f];
						Console.WriteLine("f: {0}", f);
					}
					endFrame = startFrame+e-1;
					frame = srcImage[startFrame+e];
					Console.WriteLine("e: {0}", e);
					Console.WriteLine("FrameCount: {0}", FrameCount);
				}
			}
			else if (args.Contains("--vehicle")) //resorting to RA/CnC compatible counter-clockwise frame order
			{
				frame = srcImage[startFrame];

				for (int f = endFrame-1; f > startFrame-1; f--)
				{
					OffsetX = frame.FrameWidth/2 - frame.OffsetX;
					OffsetY = frame.FrameHeight/2 - frame.OffsetY;

					Console.WriteLine("calculated OffsetX: {0}", OffsetX);
					Console.WriteLine("calculated OffsetY: {0}", OffsetY);

					var data = bitmap.LockBits(new Rectangle(x+OffsetX, 0+OffsetY, frame.Width, frame.Height), ImageLockMode.WriteOnly,
						PixelFormat.Format8bppIndexed);

					for (var i = 0; i < frame.Height; i++)
						Marshal.Copy(frame.Image, i * frame.Width,
							new IntPtr(data.Scan0.ToInt64() + i * data.Stride), frame.Width);

					bitmap.UnlockBits(data);

					x += frame.FrameWidth;

					frame = srcImage[f];
				}
			}
			else if (args.Contains("--turret")) //resorting to RA/CnC compatible counter-clockwise frame order
			{
				frame = srcImage[startFrame];

				for (int f = endFrame-1; f > startFrame-1; f--)
				{
					if (frame.OffsetX < 0) { frame.OffsetX = 0 - frame.OffsetX; }
					if (frame.OffsetY < 0) { frame.OffsetY = 0 - frame.OffsetY; }
					OffsetX = 0 + frame.OffsetX;
					OffsetY = frame.FrameHeight - frame.OffsetY;

					Console.WriteLine("calculated OffsetX: {0}", OffsetX);
					Console.WriteLine("calculated OffsetY: {0}", OffsetY);

					var data = bitmap.LockBits(new Rectangle(x+OffsetX, 0+OffsetY, frame.Width, frame.Height), ImageLockMode.WriteOnly,
						PixelFormat.Format8bppIndexed);

					for (var i = 0; i < frame.Height; i++)
						Marshal.Copy(frame.Image, i * frame.Width,
							new IntPtr(data.Scan0.ToInt64() + i * data.Stride), frame.Width);

					bitmap.UnlockBits(data);

					x += frame.FrameWidth;

					frame = srcImage[f];
				}
			}
			else if (args.Contains("--wall")) //complex resorting to RA/CnC compatible frame order
			{
				int[] D2kBrikFrameOrder = {1, 4, 2, 12, 5, 6, 16, 9, 3, 13, 7, 8, 14, 10, 11, 15, 17, 20, 18, 28, 21, 22, 32, 25, 19, 29, 23, 24, 30, 26, 27, 31};
				foreach (int o in D2kBrikFrameOrder)
				{
					int f = startFrame -1 + o;

					frame = srcImage[f];

					if (frame.OffsetX < 0) { frame.OffsetX = 0 - frame.OffsetX; }
					if (frame.OffsetY < 0) { frame.OffsetY = 0 - frame.OffsetY; }
					OffsetX = 0 + frame.OffsetX;
					OffsetY = frame.FrameHeight - frame.OffsetY;
					Console.WriteLine("calculated OffsetX: {0}", OffsetX);
					Console.WriteLine("calculated OffsetY: {0}", OffsetY);

					var data = bitmap.LockBits(new Rectangle(x+OffsetX, 0+OffsetY, frame.Width, frame.Height), ImageLockMode.WriteOnly,
						PixelFormat.Format8bppIndexed);

					for (var i = 0; i < frame.Height; i++)
						Marshal.Copy(frame.Image, i * frame.Width,
							new IntPtr(data.Scan0.ToInt64() + i * data.Stride), frame.Width);

					bitmap.UnlockBits(data);

					x += frame.FrameWidth;
				}
			}
			else if (args.Contains("--tileset"))
			{
				int f = 0;
				var tileset = new Bitmap(frame.FrameWidth * 20, frame.FrameHeight * 40, PixelFormat.Format8bppIndexed);
				tileset.Palette = palette.AsSystemPalette();

				for (int h = 0; h < 40; h++)
				{
					for (int w = 0; w < 20; w++)
					{
						if (h * 20 + w < FrameCount)
						{
							Console.WriteLine(f);
							frame = srcImage[f];

							var data = tileset.LockBits(new Rectangle(w * frame.Width, h * frame.Height, frame.Width, frame.Height),
								ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

							for (var i = 0; i < frame.Height; i++)
								Marshal.Copy(frame.Image, i * frame.Width,
									new IntPtr(data.Scan0.ToInt64() + i * data.Stride), frame.Width);

							tileset.UnlockBits(data);
							f++;
						}
					}
				}
				bitmap = tileset;
			}
			else
			{
				for (int f = startFrame; f < endFrame; f++)
				{
					frame = srcImage[f];
					if (args.Contains("--infantrydeath"))
					{
						OffsetX = frame.FrameWidth/2 - frame.Width/2;
						OffsetY = frame.FrameHeight/2 - frame.Height/2;
					}
					else if (args.Contains("--projectile"))
					{
						OffsetX = frame.FrameWidth/2 - frame.OffsetX;
						OffsetY = frame.FrameHeight/2 - frame.OffsetY;
					}
					else if (args.Contains("--building"))
					{
						if (frame.OffsetX < 0) { frame.OffsetX = 0 - frame.OffsetX; }
						if (frame.OffsetY < 0) { frame.OffsetY = 0 - frame.OffsetY; }
						OffsetX = 0 + frame.OffsetX;
						OffsetY = frame.FrameHeight - frame.OffsetY;
					}
					Console.WriteLine("calculated OffsetX: {0}", OffsetX);
					Console.WriteLine("calculated OffsetY: {0}", OffsetY);

					var data = bitmap.LockBits(new Rectangle(x+OffsetX, 0+OffsetY, frame.Width, frame.Height), ImageLockMode.WriteOnly,
						PixelFormat.Format8bppIndexed);

					for (var i = 0; i < frame.Height; i++)
						Marshal.Copy(frame.Image, i * frame.Width,
							new IntPtr(data.Scan0.ToInt64() + i * data.Stride), frame.Width);

					bitmap.UnlockBits(data);

					x += frame.FrameWidth;
				}
			}
			bitmap.Save(filename+".png");
			Console.WriteLine(filename+".png saved");
		}

		public static void ConvertTmpToPng(string[] args)
		{
			var mods = args[1].Split(',');
			var theater = args[2];
			var templateNames = args.Skip(3);
			int[] ShadowIndex = { 3, 4 };

			var manifest = new Manifest(mods);
			FileSystem.LoadFromManifest(manifest);

			var tileset = manifest.TileSets.Select( a => new TileSet(a) )
				.FirstOrDefault( ts => ts.Name == theater );

			if (tileset == null)
				throw new InvalidOperationException("No theater named '{0}'".F(theater));

			tileset.LoadTiles();
			var palette = new Palette(FileSystem.Open(tileset.Palette), ShadowIndex);

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
			for (var i = 0; i < 4; i++)
				remap[i] = i;

			var srcMod = args[1].Split(':')[0];
			Game.modData = new ModData(srcMod);
			FileSystem.LoadFromManifest(Game.modData.Manifest);
			Rules.LoadRules(Game.modData.Manifest, new Map());
			var srcPaletteInfo = Rules.Info["player"].Traits.Get<PlayerColorPaletteInfo>();
			int[] srcRemapIndex = srcPaletteInfo.RemapIndex;

			var destMod = args[2].Split(':')[0];
			Game.modData = new ModData(destMod);
			FileSystem.LoadFromManifest(Game.modData.Manifest);
			Rules.LoadRules(Game.modData.Manifest, new Map());
			var destPaletteInfo = Rules.Info["player"].Traits.Get<PlayerColorPaletteInfo>();
			int[] destRemapIndex = destPaletteInfo.RemapIndex;

			int[] ShadowIndex = { };
			// the remap range is always 16 entries, but their location and order changes
			for (var i = 0; i < 16; i++)
				remap[PlayerColorRemap.GetRemapIndex(srcRemapIndex, i)]
					= PlayerColorRemap.GetRemapIndex(destRemapIndex, i);

			// map everything else to the best match based on channel-wise distance
			var srcPalette = Palette.Load(args[1].Split(':')[1], ShadowIndex);
			var destPalette = Palette.Load(args[2].Split(':')[1], ShadowIndex);

			var fullIndexRange = Exts.MakeArray<int>(256, x => x);

			for (var i = 0; i < 256; i++)
				if (!remap.ContainsKey(i))
					remap[i] = fullIndexRange
						.Where(a => !remap.ContainsValue(a))
						.OrderBy(a => ColorDistance(destPalette.Values[a], srcPalette.Values[i]))
						.First();

			var srcImage = ShpReader.Load(args[3]);

			using (var destStream = File.Create(args[4]))
				ShpWriter.Write(destStream, srcImage.Width, srcImage.Height,
					srcImage.Frames.Select( im => im.Image.Select(px => (byte)remap[px]).ToArray() ));
		}

		//This is needed because the run and shoot animation are next to each other for each sequence in RA/CnC, but not in D2k.
		public static void TransposeShp(string[] args)
		{
			var srcImage = ShpReader.Load(args[1]);

			var srcFrames = srcImage.Frames.ToArray();
			var destFrames = srcImage.Frames.ToArray();

			for( var z = 3; z < args.Length - 2; z += 3 )
			{
				var start = int.Parse(args[z]);
				var m = int.Parse(args[z+1]);
				var n = int.Parse(args[z+2]);

				for( var i = 0; i < m; i++ )
					for( var j = 0; j < n; j++ )
						destFrames[ start + i*n + j ] = srcFrames[ start + j*m + i ];
			}

			using( var destStream = File.Create(args[2]) )
				ShpWriter.Write(destStream, srcImage.Width, srcImage.Height,
					destFrames.Select(f => f.Image));
		}

		static string FriendlyTypeName(Type t)
		{
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
				return "Dictionary<{0},{1}>".F(t.GetGenericArguments().Select(FriendlyTypeName).ToArray());

			return t.Name;
		}

		public static void ExtractTraitDocs(string[] args)
		{
			Game.modData = new ModData(args[1]);
			FileSystem.LoadFromManifest(Game.modData.Manifest);
			Rules.LoadRules(Game.modData.Manifest, new Map());

			Console.WriteLine("## Documentation");
			Console.WriteLine(
				"This documentation is aimed at modders and contributers of OpenRA. It displays all traits with default values and developer commentary. " +
				"Please do not edit it directly, but add new `[Desc(\"String\")]` tags to the source code. This file has been automatically generated on {0}. " +
				"Type `make docs` to create a new one and put it on https://github.com/OpenRA/OpenRA/wiki/Traits afterwards. " +
				"A copy of this is compiled to HTML and shipped with every release during the automated packaging process.", DateTime.Now);
			Console.WriteLine("```yaml");
			Console.WriteLine();

			foreach (var t in Game.modData.ObjectCreator.GetTypesImplementing<ITraitInfo>())
			{
				if (t.ContainsGenericParameters || t.IsAbstract)
					continue; // skip helpers like TraitInfo<T>

				var traitName = t.Name.EndsWith("Info") ? t.Name.Substring(0, t.Name.Length - 4) : t.Name;
				var traitDescLines = t.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines);
				Console.WriteLine("{0}:{1}", traitName, traitDescLines.Count() == 1 ? " # " + traitDescLines.First() : "");
				if (traitDescLines.Count() >= 2)
					foreach (var line in traitDescLines)
						Console.WriteLine("\t# {0}", line);

				var liveTraitInfo = Game.modData.ObjectCreator.CreateBasic(t);

				foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
				{
					var fieldDescLines = f.GetCustomAttributes<DescAttribute>(true).SelectMany(d => d.Lines);
					var fieldType = FriendlyTypeName(f.FieldType);
					var defaultValue = FieldSaver.SaveField(liveTraitInfo, f.Name).Value.Value;
					Console.WriteLine("\t{0}: {1} # Type: {2}{3}", f.Name, defaultValue, fieldType, fieldDescLines.Count() == 1 ? ". " + fieldDescLines.First() : "");
					if (fieldDescLines.Count() >= 2)
						foreach (var line in fieldDescLines)
							Console.WriteLine("\t# {0}", line);
				}
			}

			Console.WriteLine();
			Console.WriteLine("```");
		}
	}
}
