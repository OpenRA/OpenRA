#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using System.Text;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Utility
{
	public static class Command
	{
		static IEnumerable<string> GlobArgs(string[] args, int startIndex = 1)
		{
			for (var i = startIndex; i < args.Length; i++)
				foreach (var path in Glob.Expand(args[i]))
					yield return path;
		}

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
			var inputFiles = GlobArgs(args).OrderBy(a => a).ToList();
			var dest = inputFiles[0].Split('-').First() + ".shp";
			var frames = inputFiles.Select(a => PngLoader.Load(a));

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

			var palette = new ImmutablePalette(args[2], shadowIndex);

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
			GlobalFileSystem.LoadFromManifest(manifest);

			foreach (var f in files)
			{
				var src = GlobalFileSystem.Open(f);
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
			GlobalFileSystem.LoadFromManifest(Game.modData.Manifest);
			var srcRules = Game.modData.RulesetCache.LoadDefaultRules();
			var srcPaletteInfo = srcRules.Actors["player"].Traits.Get<PlayerColorPaletteInfo>();
			var srcRemapIndex = srcPaletteInfo.RemapIndex;

			var destMod = args[2].Split(':')[0];
			Game.modData = new ModData(destMod);
			GlobalFileSystem.LoadFromManifest(Game.modData.Manifest);
			var destRules = Game.modData.RulesetCache.LoadDefaultRules();
			var destPaletteInfo = destRules.Actors["player"].Traits.Get<PlayerColorPaletteInfo>();
			var destRemapIndex = destPaletteInfo.RemapIndex;
			var shadowIndex = new int[] { };

			// the remap range is always 16 entries, but their location and order changes
			for (var i = 0; i < 16; i++)
				remap[PlayerColorRemap.GetRemapIndex(srcRemapIndex, i)]
					= PlayerColorRemap.GetRemapIndex(destRemapIndex, i);

			// map everything else to the best match based on channel-wise distance
			var srcPalette = new ImmutablePalette(args[1].Split(':')[1], shadowIndex);
			var destPalette = new ImmutablePalette(args[2].Split(':')[1], shadowIndex);

			for (var i = 0; i < Palette.Size; i++)
				if (!remap.ContainsKey(i))
					remap[i] = Enumerable.Range(0, Palette.Size)
						.Where(a => !remap.ContainsValue(a))
						.MinBy(a => ColorDistance(destPalette[a], srcPalette[i]));

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
				var start = Exts.ParseIntegerInvariant(args[z]);
				var m = Exts.ParseIntegerInvariant(args[z + 1]);
				var n = Exts.ParseIntegerInvariant(args[z + 2]);

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

			if (t.IsSubclassOf(typeof(Array)))
				return "Multiple {0}".F(FriendlyTypeName(t.GetElementType()));

			if (t == typeof(int) || t == typeof(uint))
				return "Integer";

			if (t == typeof(int2))
				return "2D Integer";

			if (t == typeof(float) || t == typeof(decimal))
				return "Real Number";

			if (t == typeof(float2))
				return "2D Real Number";

			if (t == typeof(CPos))
				return "2D Cell Position";

			if (t == typeof(CVec))
				return "2D Cell Vector";

			if (t == typeof(WAngle))
				return "1D World Angle";

			if (t == typeof(WRot))
				return "3D World Rotation";

			if (t == typeof(WPos))
				return "3D World Position";

			if (t == typeof(WRange))
				return "1D World Range";

			if (t == typeof(WVec))
				return "3D World Vector";

			return t.Name;
		}

		[Desc("MOD", "Generate trait documentation in MarkDown format.")]
		public static void ExtractTraitDocs(string[] args)
		{
			Game.modData = new ModData(args[1]);

			Console.WriteLine(
				"This documentation is aimed at modders. It displays all traits with default values and developer commentary. " +
				"Please do not edit it directly, but add new `[Desc(\"String\")]` tags to the source code. This file has been " +
				"automatically generated for version {0} of OpenRA.", Game.modData.Manifest.Mod.Version);
			Console.WriteLine();

			var toc = new StringBuilder();
			var doc = new StringBuilder();

			foreach (var t in Game.modData.ObjectCreator.GetTypesImplementing<ITraitInfo>().OrderBy(t => t.Namespace))
			{
				if (t.ContainsGenericParameters || t.IsAbstract)
					continue; // skip helpers like TraitInfo<T>

				var traitName = t.Name.EndsWith("Info") ? t.Name.Substring(0, t.Name.Length - 4) : t.Name;
				toc.AppendLine("* [{0}](#{1})".F(traitName, traitName.ToLowerInvariant()));
				var traitDescLines = t.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines);
				doc.AppendLine();
				doc.AppendLine("### {0}".F(traitName));
				foreach (var line in traitDescLines)
					doc.AppendLine(line);

				var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
				if (!fields.Any())
					continue;
				doc.AppendLine("<table>");
				doc.AppendLine("<tr><th>Property</th><th>Default Value</th><th>Type</th><th>Description</th></tr>");
				var liveTraitInfo = Game.modData.ObjectCreator.CreateBasic(t);
				foreach (var f in fields)
				{
					var fieldDescLines = f.GetCustomAttributes<DescAttribute>(true).SelectMany(d => d.Lines);
					var fieldType = FriendlyTypeName(f.FieldType);
					var defaultValue = FieldSaver.SaveField(liveTraitInfo, f.Name).Value.Value;
					doc.Append("<tr><td>{0}</td><td>{1}</td><td>{2}</td>".F(f.Name, defaultValue, fieldType));
					doc.Append("<td>");
					foreach (var line in fieldDescLines)
						doc.Append(line);
					doc.AppendLine("</td></tr>");
				}
				doc.AppendLine("</table>");
			}

			Console.Write(toc.ToString());
			Console.Write(doc.ToString());
		}

		static string[] RequiredTraitNames(Type t)
		{
			// Returns the inner types of all the Requires<T> interfaces on this type
			var outer = t.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Requires<>));

			// Get the inner types
			var inner = outer.SelectMany(i => i.GetGenericArguments()).ToArray();

			// Remove the namespace and the trailing "Info"
			return inner.Select(i => i.Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault())
				.Select(s => s.EndsWith("Info") ? s.Remove(s.Length - 4, 4) : s)
				.ToArray();
		}

		[Desc("MOD", "Generate Lua API documentation in MarkDown format.")]
		public static void ExtractLuaDocs(string[] args)
		{
			Game.modData = new ModData(args[1]);

			Console.WriteLine("This is an automatically generated lising of the new Lua map scripting API, generated for {0} of OpenRA.", Game.modData.Manifest.Mod.Version);
			Console.WriteLine();
			Console.WriteLine("OpenRA allows custom maps and missions to be scripted using Lua 5.1.\n" +
				"These scripts run in a sandbox that prevents access to unsafe functions (e.g. OS or file access), " +
				"and limits the memory and CPU usage of the scripts.");
			Console.WriteLine();
			Console.WriteLine("You can access this interface by adding the [LuaScript](Traits#luascript) trait to the world actor in your map rules (note, you must replace the spaces in the snippet below with a single tab for each level of indentation):");
			Console.WriteLine("```\nRules:\n\tWorld:\n\t\tLuaScript:\n\t\t\tScripts: myscript.lua\n```");
			Console.WriteLine();
			Console.WriteLine("Map scripts can interact with the game engine in three ways:\n" +
				"* Global tables provide functions for interacting with the global world state, or performing general helper tasks.\n" +
				"They exist in the global namespace, and can be called directly using ```<table name>.<function name>```.\n" +
				"* Individual actors expose a collection of properties and commands that query information of modify their state.\n" +
				"  * Some commands, marked as <em>queued activity</em>, are asynchronous.  Activities are queued on the actor, and will run in " +
				"sequence until the queue is empty or the Stop command is called.  Actors that are not performing an activity are Idle " +
				"(actor.IsIdle will return true).  The properties and commands available on each actor depends on the traits that the actor " +
				"specifies in its rule definitions.\n" +
				"* Individual players explose a collection of properties and commands that query information of modify their state.\n" +
				"The properties and commands available on each actor depends on the traits that the actor specifies in its rule definitions.\n");
			Console.WriteLine();

			var tables = Game.modData.ObjectCreator.GetTypesImplementing<ScriptGlobal>()
				.OrderBy(t => t.Name);

			Console.WriteLine("<h3>Global Tables</h3>");

			foreach (var t in tables)
			{
				var name = t.GetCustomAttributes<ScriptGlobalAttribute>(true).First().Name;
				var members = ScriptMemberWrapper.WrappableMembers(t);

				Console.WriteLine("<table align=\"center\" width=\"1024\"><tr><th colspan=\"2\" width=\"1024\">{0}</th></tr>", name);
				foreach (var m in members.OrderBy(m => m.Name))
				{
					var desc = m.HasAttribute<DescAttribute>() ? m.GetCustomAttributes<DescAttribute>(true).First().Lines.JoinWith("\n") : "";
					Console.WriteLine("<tr><td align=\"right\" width=\"50%\"><strong>{0}</strong></td><td>{1}</td></tr>".F(m.LuaDocString(), desc));
				}
				Console.WriteLine("</table>");
			}

			Console.WriteLine("<h3>Actor Properties / Commands</h3>");

			var actorCategories = Game.modData.ObjectCreator.GetTypesImplementing<ScriptActorProperties>().SelectMany(cg =>
			{
				var catAttr = cg.GetCustomAttributes<ScriptPropertyGroupAttribute>(false).FirstOrDefault();
				var category = catAttr != null ? catAttr.Category : "Unsorted";

				var required = RequiredTraitNames(cg);
				return ScriptMemberWrapper.WrappableMembers(cg).Select(mi => Tuple.Create(category, mi, required));
			}).GroupBy(g => g.Item1).OrderBy(g => g.Key);

			foreach (var kv in actorCategories)
			{
				Console.WriteLine("<table align=\"center\" width=\"1024\"><tr><th colspan=\"2\" width=\"1024\">{0}</th></tr>", kv.Key);

				foreach (var property in kv.OrderBy(p => p.Item2.Name))
				{
					var mi = property.Item2;
					var required = property.Item3;
					var hasDesc = mi.HasAttribute<DescAttribute>();
					var hasRequires = required.Any();
					var isActivity = mi.HasAttribute<ScriptActorPropertyActivityAttribute>();

					Console.WriteLine("<tr><td width=\"50%\" align=\"right\"><strong>{0}</strong>", mi.LuaDocString());

					if (isActivity)
						Console.WriteLine("<br /><em>Queued Activity</em>");

					Console.WriteLine("</td><td>");

					if (hasDesc)
						Console.WriteLine(mi.GetCustomAttributes<DescAttribute>(false).First().Lines.JoinWith("\n"));

					if (hasDesc && hasRequires)
						Console.WriteLine("<br />");

					if (hasRequires)
						Console.WriteLine("<b>Requires {1}:</b> {0}".F(required.JoinWith(", "), required.Length == 1 ? "Trait" : "Traits"));

					Console.WriteLine("</td></tr>");
				}
				Console.WriteLine("</table>");
			}

			Console.WriteLine("<h3>Player Properties / Commands</h3>");

			var playerCategories = Game.modData.ObjectCreator.GetTypesImplementing<ScriptPlayerProperties>().SelectMany(cg =>
			{
				var catAttr = cg.GetCustomAttributes<ScriptPropertyGroupAttribute>(false).FirstOrDefault();
				var category = catAttr != null ? catAttr.Category : "Unsorted";

				var required = RequiredTraitNames(cg);
				return ScriptMemberWrapper.WrappableMembers(cg).Select(mi => Tuple.Create(category, mi, required));
			}).GroupBy(g => g.Item1).OrderBy(g => g.Key);

			foreach (var kv in playerCategories)
			{
				Console.WriteLine("<table align=\"center\" width=\"1024\"><tr><th colspan=\"2\" width=\"1024\">{0}</th></tr>", kv.Key);

				foreach (var property in kv.OrderBy(p => p.Item2.Name))
				{
					var mi = property.Item2;
					var required = property.Item3;
					var hasDesc = mi.HasAttribute<DescAttribute>();
					var hasRequires = required.Any();
					var isActivity = mi.HasAttribute<ScriptActorPropertyActivityAttribute>();

					Console.WriteLine("<tr><td width=\"50%\" align=\"right\"><strong>{0}</strong>", mi.LuaDocString());

					if (isActivity)
						Console.WriteLine("<br /><em>Queued Activity</em>");

					Console.WriteLine("</td><td>");

					if (hasDesc)
						Console.WriteLine(mi.GetCustomAttributes<DescAttribute>(false).First().Lines.JoinWith("\n"));

					if (hasDesc && hasRequires)
						Console.WriteLine("<br />");

					if (hasRequires)
						Console.WriteLine("<b>Requires {1}:</b> {0}".F(required.JoinWith(", "), required.Length == 1 ? "Trait" : "Traits"));

					Console.WriteLine("</td></tr>");
				}

				Console.WriteLine("</table>");
			}
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

			GlobalFileSystem.UnmountAll();
			foreach (var dir in Game.modData.Manifest.Folders)
				GlobalFileSystem.Mount(dir);

			var minimap = Minimap.RenderMapPreview(map.Rules.TileSets[map.Tileset], map, true);

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
			var rules = Game.modData.RulesetCache.LoadDefaultRules();
			var map = LegacyMapImporter.Import(filename, rules, e => Console.WriteLine(e));
			map.RequiresMod = mod;
			map.MakeDefaultPlayers();
			map.FixOpenAreas(rules);
			var dest = map.Title + ".oramap";
			map.Save(dest);
			Console.WriteLine(dest + " saved.");
		}

		// TODO: flat OpenDocument XML (.fods) may be nicer
		[Desc("MOD", "[--pure-data]", "Export the game rules into a CSV file for inspection.")]
		public static void ExportCharacterSeparatedRules(string[] args)
		{
			var mod = args[1];
			var pureData = args.Contains("--pure-data");
			Game.modData = new ModData(mod);
			var rules = Game.modData.RulesetCache.LoadDefaultRules();

			var armorList = new List<string>();
			foreach (var actorInfo in rules.Actors.Values)
			{
				var armor = actorInfo.Traits.GetOrDefault<ArmorInfo>();
				if (armor != null)
					if (!armorList.Contains(armor.Type))
						armorList.Add(armor.Type);
			}

			armorList.Sort();
			var vsArmor = "";
			foreach (var armorType in armorList)
				vsArmor = vsArmor + ";vs. " + armorType;

			var dump = new StringBuilder();
			if (pureData)
				dump.AppendLine("Name;Faction;Health;Cost;Weapon;Damage;Burst;Delay;Rate of Fire");
			else
				dump.AppendLine("Name;Faction;Health;Cost;Weapon;Damage;Burst;Delay;Rate of Fire;Damage per Second" + vsArmor);

			var line = 1;
			foreach (var actorInfo in rules.Actors.Values)
			{
				if (actorInfo.Name.StartsWith("^"))
					continue;

				var buildable = actorInfo.Traits.GetOrDefault<BuildableInfo>();
				if (buildable == null)
					continue;

				line++;

				var tooltip = actorInfo.Traits.GetOrDefault<TooltipInfo>();
				var name = tooltip != null ? tooltip.Name : actorInfo.Name;

				var faction = FieldSaver.FormatValue(buildable.Owner, buildable.Owner.GetType());

				var health = actorInfo.Traits.GetOrDefault<HealthInfo>();
				var hp = health != null ? health.HP : 0;

				var value = actorInfo.Traits.GetOrDefault<ValuedInfo>();
				var cost = value != null ? value.Cost : 0;

				dump.Append("{0};{1};{2};{3}".F(name, faction, hp, cost));

				var armaments = actorInfo.Traits.WithInterface<ArmamentInfo>();
				if (armaments.Any())
				{
					var weapons = armaments.Select(a => a.Weapon);
					var weaponCount = 0;
					foreach (var weaponName in weapons)
					{
						var weapon = rules.Weapons[weaponName.ToLowerInvariant()];
						weaponCount++;
						if (weaponCount > 1)
						{
							line++;
							dump.AppendLine();
							dump.Append(" ; ; ; ");
						}

						var rateOfFire = (weapon.ROF > 1 ? weapon.ROF : 1).ToString();
						var burst = weapon.Burst.ToString();
						var warhead = weapon.Warheads.First(); // TODO
						var damage = warhead.Damage.ToString();
						var delay = weapon.BurstDelay;
						var damagePerSecond = "=(F{0}*G{0})/(H{0}+G{0}*I{0})*25".F(line);

						var versus = "";
						foreach (var armorType in armorList)
						{
							var vs = warhead.Versus.ContainsKey(armorType) ? warhead.Versus[armorType] : 1f;
							versus = versus + "=J{0}*{1};".F(line, vs);
						}

						if (pureData)
							dump.Append(";{0};{1};{2};{3};{4}".F(weaponName, damage, burst, delay, rateOfFire));
						else
							dump.Append(";{0};{1};{2};{3};{4};{5};{6}".F(weaponName, damage, burst, delay, rateOfFire, damagePerSecond, versus));
					}
				}
				dump.AppendLine();
			}

			var filename = "{0}-mod-rules.csv".F(mod);
			using (StreamWriter outfile = new StreamWriter(filename))
				outfile.Write(dump.ToString());
			Console.WriteLine("{0} has been saved.".F(filename));
			if (!pureData)
				Console.WriteLine("Open in a spreadsheet application as values separated by semicolon.");
		}
	}
}
