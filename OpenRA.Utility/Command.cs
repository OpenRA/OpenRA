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

			Console.WriteLine(dest + " saved.");
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
			var shadowIndex = new int[] { };
			if (args.Contains("--noshadow"))
			{
					Array.Resize(ref shadowIndex, shadowIndex.Length + 3);
					shadowIndex[shadowIndex.Length - 1] = 1;
					shadowIndex[shadowIndex.Length - 2] = 3;
					shadowIndex[shadowIndex.Length - 3] = 4;
			}

			var palette = Palette.Load(args[2], shadowIndex);

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

					bitmap.UnlockBits(data);
				}

				bitmap.Save(dest);
				Console.WriteLine(dest + " saved");
			}
		}

		public static void ConvertR8ToPng(string[] args)
		{
			var srcImage = new R8Reader(File.OpenRead(args[1]));
			var shadowIndex = new int[] { };
			if (args.Contains("--noshadow"))
			{
				Array.Resize(ref shadowIndex, shadowIndex.Length + 1);
				shadowIndex[shadowIndex.Length - 1] = 3;
			}

			var palette = Palette.Load(args[2], shadowIndex);
			var startFrame = int.Parse(args[3]);
			var endFrame = int.Parse(args[4]) + 1;
			var filename = args[5];
			var frameCount = endFrame - startFrame;

			var frame = srcImage[startFrame];
			var bitmap = new Bitmap(frame.FrameSize.Width * frameCount, frame.FrameSize.Height, PixelFormat.Format8bppIndexed);
			bitmap.Palette = palette.AsSystemPalette();

			int x = 0;

			frame = srcImage[startFrame];

			if (args.Contains("--vehicle"))
			{
				frame = srcImage[startFrame];

				for (int f = endFrame - 1; f > startFrame - 1; f--)
				{
					var offsetX = frame.FrameSize.Width / 2 - frame.Offset.X;
					var offsetY = frame.FrameSize.Height / 2 - frame.Offset.Y;

					Console.WriteLine("calculated OffsetX: {0}", offsetX);
					Console.WriteLine("calculated OffsetY: {0}", offsetY);

					var data = bitmap.LockBits(new Rectangle(x + offsetX, 0 + offsetY, frame.Size.Width, frame.Size.Height), ImageLockMode.WriteOnly,
						PixelFormat.Format8bppIndexed);

					for (var i = 0; i < frame.Size.Height; i++)
						Marshal.Copy(frame.Image, i * frame.Size.Width,
						             new IntPtr(data.Scan0.ToInt64() + i * data.Stride), frame.Size.Width);

					bitmap.UnlockBits(data);

					x += frame.FrameSize.Width;

					frame = srcImage[f];
				}
			}
			else if (args.Contains("--turret"))
			{
				frame = srcImage[startFrame];

				for (int f = endFrame - 1; f > startFrame - 1; f--)
				{
					var offsetX = Math.Abs(frame.Offset.X);
					var offsetY = frame.FrameSize.Height - Math.Abs(frame.Offset.Y);

					Console.WriteLine("calculated OffsetX: {0}", offsetX);
					Console.WriteLine("calculated OffsetY: {0}", offsetY);

					var data = bitmap.LockBits(new Rectangle(x + offsetX, 0 + offsetY, frame.Size.Width, frame.Size.Height), ImageLockMode.WriteOnly,
						PixelFormat.Format8bppIndexed);

					for (var i = 0; i < frame.Size.Height; i++)
						Marshal.Copy(frame.Image, i * frame.Size.Width,
							new IntPtr(data.Scan0.ToInt64() + i * data.Stride), frame.Size.Width);

					bitmap.UnlockBits(data);

					x += frame.FrameSize.Width;

					frame = srcImage[f];
				}
			}
			else if (args.Contains("--tileset"))
			{
				int f = 0;
				var tileset = new Bitmap(frame.FrameSize.Width * 20, frame.FrameSize.Height * 40, PixelFormat.Format8bppIndexed);
				tileset.Palette = palette.AsSystemPalette();

				for (int h = 0; h < 40; h++)
				{
					for (int w = 0; w < 20; w++)
					{
						if (h * 20 + w < frameCount)
						{
							Console.WriteLine(f);
							frame = srcImage[f];

							var data = tileset.LockBits(new Rectangle(w * frame.Size.Width, h * frame.Size.Height, frame.Size.Width, frame.Size.Height),
								ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

							for (var i = 0; i < frame.Size.Height; i++)
								Marshal.Copy(frame.Image, i * frame.Size.Width,
									new IntPtr(data.Scan0.ToInt64() + i * data.Stride), frame.Size.Width);

							tileset.UnlockBits(data);
							f++;
						}
					}
				}

				bitmap = tileset;
			}

			bitmap.Save(filename + ".png");
			Console.WriteLine(filename + ".png saved");
		}

		public static void ConvertTmpToPng(string[] args)
		{
			var mods = args[1].Split(',');
			var theater = args[2];
			var templateNames = args.Skip(3);
			var shadowIndex = new int[] { 3, 4 };

			var manifest = new Manifest(mods);
			FileSystem.LoadFromManifest(manifest);

			var tileset = manifest.TileSets.Select(a => new TileSet(a))
				.FirstOrDefault(ts => ts.Name == theater);

			if (tileset == null)
				throw new InvalidOperationException("No theater named '{0}'".F(theater));

			var renderer = new TileSetRenderer(tileset, new Size(manifest.TileSize, manifest.TileSize));
			var palette = new Palette(FileSystem.Open(tileset.Palette), shadowIndex);

			foreach (var templateName in templateNames)
			{
				var template = tileset.Templates.FirstOrDefault(tt => tt.Value.Image == templateName);
				if (template.Value == null)
					throw new InvalidOperationException("No such template '{0}'".F(templateName));

				using (var image = renderer.RenderTemplate(template.Value.Id, palette))
					image.Save(Path.ChangeExtension(templateName, ".png"));
			}
		}

		public static void ConvertFormat2ToFormat80(string[] args)
		{
			var src = args[1];
			var dest = args[2];

			Dune2ShpReader srcImage = null;
			using (var s = File.OpenRead(src))
				srcImage = new Dune2ShpReader(s);

			var size = srcImage.First().Size;

			if (!srcImage.All(im => im.Size == size))
				throw new InvalidOperationException("All the frames must be the same size to convert from Dune2 to RA");

			using (var destStream = File.Create(dest))
				ShpWriter.Write(destStream, size.Width, size.Height,
					srcImage.Select(im => im.Image));
		}

		public static void ExtractFiles(string[] args)
		{
			var mods = args[1].Split(',');
			var files = args.Skip(2);

			var manifest = new Manifest(mods);
			FileSystem.LoadFromManifest(manifest);

			foreach (var f in files)
			{
				if (f == "--userdir")
					break;

				var src = FileSystem.Open(f);
				if (src == null)
					throw new InvalidOperationException("File not found: {0}".F(f));
				var data = src.ReadAllBytes();
				var output = args.Contains("--userdir") ? Platform.SupportDir + f : f;
				File.WriteAllBytes(output, data);
				Console.WriteLine(output + " saved.");
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
			var remap = new Dictionary<int, int>();

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
			var destRemapIndex = destPaletteInfo.RemapIndex;
			var shadowIndex = new int[] { };

			// the remap range is always 16 entries, but their location and order changes
			for (var i = 0; i < 16; i++)
				remap[PlayerColorRemap.GetRemapIndex(srcRemapIndex, i)]
					= PlayerColorRemap.GetRemapIndex(destRemapIndex, i);

			// map everything else to the best match based on channel-wise distance
			var srcPalette = Palette.Load(args[1].Split(':')[1], shadowIndex);
			var destPalette = Palette.Load(args[2].Split(':')[1], shadowIndex);

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
					srcImage.Frames.Select(im => im.Image.Select(px => (byte)remap[px]).ToArray()));
		}

		public static void TransposeShp(string[] args)
		{
			var srcImage = ShpReader.Load(args[1]);

			var srcFrames = srcImage.Frames.ToArray();
			var destFrames = srcImage.Frames.ToArray();

			for (var z = 3; z < args.Length - 2; z += 3)
			{
				var start = int.Parse(args[z]);
				var m = int.Parse(args[z + 1]);
				var n = int.Parse(args[z + 2]);

				for (var i = 0; i < m; i++)
					for (var j = 0; j < n; j++)
						destFrames[start + i * n + j] = srcFrames[start + j * m + i];
			}

			using (var destStream = File.Create(args[2]))
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
				"This documentation is aimed at modders and contributors of OpenRA. It displays all traits with default values and developer commentary. " +
				"Please do not edit it directly, but add new `[Desc(\"String\")]` tags to the source code. This file has been automatically generated on {0}. " +
				"Type `make docs` to create a new one. A copy of this is uploaded to https://github.com/OpenRA/OpenRA/wiki/Traits " +
				"as well as compiled to HTML and shipped with every release during the automated packaging process.", DateTime.Now);
			Console.WriteLine();
			Console.WriteLine("```yaml");
			Console.WriteLine();

			foreach (var t in Game.modData.ObjectCreator.GetTypesImplementing<ITraitInfo>())
			{
				if (t.ContainsGenericParameters || t.IsAbstract)
					continue; // skip helpers like TraitInfo<T>

				var traitName = t.Name.EndsWith("Info") ? t.Name.Substring(0, t.Name.Length - 4) : t.Name;
				var traitDescLines = t.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines);
				Console.WriteLine("\t{0}:{1}", traitName, traitDescLines.Count() == 1 ? " # " + traitDescLines.First() : "");
				if (traitDescLines.Count() >= 2)
					foreach (var line in traitDescLines)
						Console.WriteLine("\t\t# {0}", line);

				var liveTraitInfo = Game.modData.ObjectCreator.CreateBasic(t);

				foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
				{
					var fieldDescLines = f.GetCustomAttributes<DescAttribute>(true).SelectMany(d => d.Lines);
					var fieldType = FriendlyTypeName(f.FieldType);
					var defaultValue = FieldSaver.SaveField(liveTraitInfo, f.Name).Value.Value;
					Console.WriteLine("\t\t{0}: {1} # Type: {2}{3}", f.Name, defaultValue, fieldType, fieldDescLines.Count() == 1 ? ". " + fieldDescLines.First() : "");
					if (fieldDescLines.Count() >= 2)
						foreach (var line in fieldDescLines)
							Console.WriteLine("\t\t# {0}", line);
				}
			}

			Console.WriteLine();
			Console.WriteLine("```");
		}

		public static void GetMapHash(string[] args)
		{
			var result = new Map(args[1]).Uid;
			Console.WriteLine(result);
		}
	}
}
