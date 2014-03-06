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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Utility
{
	public static class Command
	{
		[Desc("KEY", "Get value of KEY from settings.yaml")]
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

		[Desc("PNGFILE [PNGFILE ...]", "Combine a list of PNG images into a SHP")]
		public static void ConvertPngToShp(string[] args)
		{
			var dest = args[1].Split('-').First() + ".shp";
			var frames = args.Skip(1).Select(a => PngLoader.Load(a));

			var size = frames.First().Size;
			if (frames.Any(f => f.Size != size))
				throw new InvalidOperationException("All frames must be the same size");

			using (var destStream = File.Create(dest))
				ShpReader.Write(destStream, size, frames.Select(f => f.ToBytes()));

			Console.WriteLine(dest + " saved.");
		}

		static byte[] ToBytes(this Bitmap bitmap)
		{
			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
				PixelFormat.Format8bppIndexed);

			var bytes = new byte[bitmap.Width * bitmap.Height];
			for (var i = 0; i < bitmap.Height; i++)
				Marshal.Copy(new IntPtr(data.Scan0.ToInt64() + i * data.Stride),
					bytes, i * bitmap.Width, bitmap.Width);

			bitmap.UnlockBits(data);

			return bytes;
		}

		[Desc("SPRITEFILE PALETTE [--noshadow] [--nopadding]",
		      "Convert a shp/tmp/R8 to a series of PNGs, optionally removing shadow")]
		public static void ConvertSpriteToPng(string[] args)
		{
			var src = args[1];
			var shadowIndex = new int[] { };
			if (args.Contains("--noshadow"))
			{
					Array.Resize(ref shadowIndex, shadowIndex.Length + 3);
					shadowIndex[shadowIndex.Length - 1] = 1;
					shadowIndex[shadowIndex.Length - 2] = 3;
					shadowIndex[shadowIndex.Length - 3] = 4;
			}

			var palette = Palette.Load(args[2], shadowIndex);

			ISpriteSource source;
			using (var stream = File.OpenRead(src))
				source = SpriteSource.LoadSpriteSource(stream, src);

			// The r8 padding requires external information that we can't access here.
			var usePadding = !(args.Contains("--nopadding") || source is R8Reader);
			var count = 0;
			var prefix = Path.GetFileNameWithoutExtension(src);

			foreach (var frame in source.Frames)
			{
				var frameSize = usePadding ? frame.FrameSize : frame.Size;
				var offset = usePadding ? (frame.Offset - 0.5f * new float2(frame.Size - frame.FrameSize)).ToInt2() : int2.Zero;

				// shp(ts) may define empty frames
				if (frameSize.Width == 0 && frameSize.Height == 0)
				{
					count++;
					continue;
				}

				using (var bitmap = new Bitmap(frameSize.Width, frameSize.Height, PixelFormat.Format8bppIndexed))
				{
					bitmap.Palette = palette.AsSystemPalette();
					var data = bitmap.LockBits(new Rectangle(0, 0, frameSize.Width, frameSize.Height),
						ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

					// Clear the frame
					if (usePadding)
					{
						var clearRow = new byte[data.Stride];
						for (var i = 0; i < frameSize.Height; i++)
							Marshal.Copy(clearRow, 0, new IntPtr(data.Scan0.ToInt64() + i * data.Stride), data.Stride);
					}

					for (var i = 0; i < frame.Size.Height; i++)
					{
						var destIndex = new IntPtr(data.Scan0.ToInt64() + (i + offset.Y) * data.Stride + offset.X);
						Marshal.Copy(frame.Data, i * frame.Size.Width, destIndex, frame.Size.Width);
					}

					bitmap.UnlockBits(data);

					var filename = "{0}-{1:D4}.png".F(prefix, count++);
					bitmap.Save(filename);
				}
			}

			Console.WriteLine("Saved {0}-[0..{1}].png", prefix, count - 1);
		}

		[Desc("MOD FILES", "Extract files from mod packages to the current directory")]
		public static void ExtractFiles(string[] args)
		{
			var mod = args[1];
			var files = args.Skip(2);

			var manifest = new Manifest(mod);
			FileSystem.LoadFromManifest(manifest);

			foreach (var f in files)
			{
				var src = FileSystem.Open(f);
				if (src == null)
					throw new InvalidOperationException("File not found: {0}".F(f));
				var data = src.ReadAllBytes();
				File.WriteAllBytes(f, data);
				Console.WriteLine(f + " saved.");
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

		[Desc("SRCMOD:PAL DESTMOD:PAL SRCSHP DESTSHP", "Remap SHPs to another palette")]
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
				ShpReader.Write(destStream, srcImage.Size,
					srcImage.Frames.Select(im => im.Data.Select(px => (byte)remap[px]).ToArray()));
		}

		[Desc("SRCSHP DESTSHP START N M [START N M ...]",
		      "Transpose the N*M block of frames starting at START.")]
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
				ShpReader.Write(destStream, srcImage.Size, destFrames.Select(f => f.Data));
		}

		static string FriendlyTypeName(Type t)
		{
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
				return "Dictionary<{0},{1}>".F(t.GetGenericArguments().Select(FriendlyTypeName).ToArray());

			return t.Name;
		}

		[Desc("MOD", "Generate trait documentation in MarkDown format.")]
		public static void ExtractTraitDocs(string[] args)
		{
			Game.modData = new ModData(args[1]);
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

		[Desc("MAPFILE", "Generate hash of specified oramap file.")]
		public static void GetMapHash(string[] args)
		{
			var result = new Map(args[1]).Uid;
			Console.WriteLine(result);
		}

		[Desc("MAPFILE", "Render PNG minimap of specified oramap file.")]
		public static void GenerateMinimap(string[] args)
		{
			var map = new Map(args[1]);
			Game.modData = new ModData(map.RequiresMod);

			FileSystem.UnmountAll();
			foreach (var dir in Game.modData.Manifest.Folders)
				FileSystem.Mount(dir);

			Rules.LoadRules(Game.modData.Manifest, map);

			var minimap = Minimap.RenderMapPreview(map, true);

			var dest = Path.GetFileNameWithoutExtension(args[1]) + ".png";
			minimap.Save(dest);
			Console.WriteLine(dest + " saved.");
		}

		[Desc("MAPFILE", "MOD", "Upgrade a version 5 map to version 6.")]
		public static void UpgradeV5Map(string[] args)
		{
			var map = args[1];
			var mod = args[2];
			Game.modData = new ModData(mod);
			new Map(map, mod);
		}

		[Desc("MOD", "FILENAME", "Convert a legacy INI/MPR map to the OpenRA format.")]
		public static void ImportLegacyMap(string[] args)
		{
			var mod = args[1];
			var filename = args[2];
			Game.modData = new ModData(mod);
			Rules.LoadRules(Game.modData.Manifest, new Map());
			var map = LegacyMapImporter.Import(filename, e => Console.WriteLine(e));
			map.RequiresMod = mod;
			map.MakeDefaultPlayers();
			map.FixOpenAreas();
			var dest = map.Title + ".oramap";
			map.Save(dest);
			Console.WriteLine(dest + " saved.");
		}

		[Desc("MOD", "OUTPUTDIR", "Output game rules for visual representation.")]
		public static void GenerateStats(string[] args)
		{
			try
			{
				var mod = args[1];

				var outputDir = "html";
				if (args.Length > 2)
					outputDir = args[2];

				YamlToHtml yth = new YamlToHtml();
				if (mod == "all")
				{
					string[] dirs = Directory.GetDirectories("mods");
					foreach (var dir in dirs)
					{
						yth.Run(dir, outputDir);
					}
				}
				else
				{
					var modFolder = Path.Combine("mods", mod);
					yth.Run(modFolder, outputDir);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: {0}", e.Message);
				Console.WriteLine("Usage: --stats MOD [output directory]");
			}
		}
	}
}
