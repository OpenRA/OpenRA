﻿#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Utility
{
	public class LegacyMapImporter
	{
		// Mapping from ra overlay index to type string
		static string[] redAlertOverlayNames =
		{
			"sbag", "cycl", "brik", "fenc", "wood",
			"gold01", "gold02", "gold03", "gold04",
			"gem01", "gem02", "gem03", "gem04",
			"v12", "v13", "v14", "v15", "v16", "v17", "v18",
			"fpls", "wcrate", "scrate", "barb", "sbag",
		};

		static Dictionary<string, Pair<byte, byte>> overlayResourceMapping = new Dictionary<string, Pair<byte, byte>>()
		{
			// RA Gold & Gems
			{ "gold01", new Pair<byte, byte>(1, 0) },
			{ "gold02", new Pair<byte, byte>(1, 1) },
			{ "gold03", new Pair<byte, byte>(1, 2) },
			{ "gold04", new Pair<byte, byte>(1, 3) },
			{ "gem01", new Pair<byte, byte>(2, 0) },
			{ "gem02", new Pair<byte, byte>(2, 1) },
			{ "gem03", new Pair<byte, byte>(2, 2) },
			{ "gem04", new Pair<byte, byte>(2, 3) },

			// CnC Tiberium
			{ "ti1", new Pair<byte, byte>(1, 0) },
			{ "ti2", new Pair<byte, byte>(1, 1) },
			{ "ti3", new Pair<byte, byte>(1, 2) },
			{ "ti4", new Pair<byte, byte>(1, 3) },
			{ "ti5", new Pair<byte, byte>(1, 4) },
			{ "ti6", new Pair<byte, byte>(1, 5) },
			{ "ti7", new Pair<byte, byte>(1, 6) },
			{ "ti8", new Pair<byte, byte>(1, 7) },
			{ "ti9", new Pair<byte, byte>(1, 8) },
			{ "ti10", new Pair<byte, byte>(1, 9) },
			{ "ti11", new Pair<byte, byte>(1, 10) },
			{ "ti12", new Pair<byte, byte>(1, 11) },
		};

		static Dictionary<string, string> overlayActorMapping = new Dictionary<string, string>() {
			// Fences
			{ "sbag", "sbag" },
			{ "cycl", "cycl" },
			{ "brik", "brik" },
			{ "fenc", "fenc" },
			{ "wood", "wood" },

			// Fields
			{ "v12", "v12" },
			{ "v13", "v13" },
			{ "v14", "v14" },
			{ "v15", "v15" },
			{ "v16", "v16" },
			{ "v17", "v17" },
			{ "v18", "v18" },

			// Crates
//			{ "wcrate", "crate" },
//			{ "scrate", "crate" },
		};

		// TODO: fix this -- will have bitrotted pretty badly.
		static Dictionary<string, HSLColor> namedColorMapping = new Dictionary<string, HSLColor>()
		{
			{ "gold", HSLColor.FromRGB(246, 214, 121) },
			{ "blue", HSLColor.FromRGB(226, 230, 246) },
			{ "red", HSLColor.FromRGB(255, 20, 0) },
			{ "neutral", HSLColor.FromRGB(238, 238, 238) },
			{ "orange", HSLColor.FromRGB(255, 230, 149) },
			{ "teal", HSLColor.FromRGB(93, 194, 165) },
			{ "salmon", HSLColor.FromRGB(210, 153, 125) },
			{ "green", HSLColor.FromRGB(160, 240, 140) },
			{ "white", HSLColor.FromRGB(255, 255, 255) },
			{ "black", HSLColor.FromRGB(80, 80, 80) },
		};

		static string Truncate(string s, int maxLength)
		{
			return s.Length <= maxLength ? s : s.Substring(0, maxLength);
		}

		int mapSize;
		int actorCount = 0;
		Map map = new Map();
		List<string> players = new List<string>();
		Action<string> errorHandler;

		LegacyMapImporter(string filename, Action<string> errorHandler)
		{
			this.errorHandler = errorHandler;
			ConvertIniMap(filename);
		}

		public static Map Import(string filename, Action<string> errorHandler)
		{
			var converter = new LegacyMapImporter(filename, errorHandler);
			return converter.map;
		}

		enum IniMapFormat { RedAlert = 3 } // otherwise, cnc (2 variants exist, we don't care to differentiate)

		public void ConvertIniMap(string iniFile)
		{
			var file = new IniFile(FileSystem.Open(iniFile));
			var basic = file.GetSection("Basic");
			var mapSection = file.GetSection("Map");
			var legacyMapFormat = (IniMapFormat)int.Parse(basic.GetValue("NewINIFormat", "0"));
			var offsetX = int.Parse(mapSection.GetValue("X", "0"));
			var offsetY = int.Parse(mapSection.GetValue("Y", "0"));
			var width = int.Parse(mapSection.GetValue("Width", "0"));
			var height = int.Parse(mapSection.GetValue("Height", "0"));
			mapSize = (legacyMapFormat == IniMapFormat.RedAlert) ? 128 : 64;

			map.Title = basic.GetValue("Name", Path.GetFileNameWithoutExtension(iniFile));
			map.Author = "Westwood Studios";
			map.Tileset = Truncate(mapSection.GetValue("Theater", "TEMPERAT"), 8);
			map.MapSize.X = mapSize;
			map.MapSize.Y = mapSize;
			map.Bounds = Rectangle.FromLTRB(offsetX, offsetY, offsetX + width, offsetY + height);
			map.Selectable = true;

			map.Smudges = Lazy.New(() => new List<SmudgeReference>());
			map.Actors = Lazy.New(() => new Dictionary<string, ActorReference>());
			map.MapResources = Lazy.New(() => new TileReference<byte, byte>[mapSize, mapSize]);
			map.MapTiles = Lazy.New(() => new TileReference<ushort, byte>[mapSize, mapSize]);

			map.Options = new MapOptions();

			if (legacyMapFormat == IniMapFormat.RedAlert)
			{
				UnpackRATileData(ReadPackedSection(file.GetSection("MapPack")));
				UnpackRAOverlayData(ReadPackedSection(file.GetSection("OverlayPack")));
				ReadRATrees(file);
			}
			else
			{
				// CnC
				UnpackCncTileData(FileSystem.Open(iniFile.Substring(0, iniFile.Length - 4) + ".bin"));
				ReadCncOverlay(file);
				ReadCncTrees(file);
			}

			LoadActors(file, "STRUCTURES");
			LoadActors(file, "UNITS");
			LoadActors(file, "INFANTRY");
			LoadSmudges(file, "SMUDGE");

			foreach (var p in players)
				LoadPlayer(file, p, legacyMapFormat == IniMapFormat.RedAlert);

			var wps = file.GetSection("Waypoints")
					.Where(kv => int.Parse(kv.Value) > 0)
					.Select(kv => Pair.New(int.Parse(kv.Key),
						LocationFromMapOffset(int.Parse(kv.Value), mapSize)))
					.ToArray();

			// Add waypoint actors
			foreach (var kv in wps)
			{
				var a = new ActorReference("waypoint");
				a.Add(new LocationInit((CPos)kv.Second));
				a.Add(new OwnerInit("Neutral"));
				map.Actors.Value.Add("waypoint" + kv.First, a);
			}
		}

		static int2 LocationFromMapOffset(int offset, int mapSize)
		{
			return new int2(offset % mapSize, offset / mapSize);
		}

		static MemoryStream ReadPackedSection(IniSection mapPackSection)
		{
			var sb = new StringBuilder();
			for (int i = 1;; i++)
			{
				var line = mapPackSection.GetValue(i.ToString(), null);
				if (line == null)
					break;

				sb.Append(line.Trim());
			}

			var data = Convert.FromBase64String(sb.ToString());
			var chunks = new List<byte[]>();
			var reader = new BinaryReader(new MemoryStream(data));

			try
			{
				while (true)
				{
					var length = reader.ReadUInt32() & 0xdfffffff;
					var dest = new byte[8192];
					var src = reader.ReadBytes((int)length);

					/*int actualLength =*/
					Format80.DecodeInto(src, dest);

					chunks.Add(dest);
				}
			}
			catch (EndOfStreamException) { }

			var ms = new MemoryStream();
			foreach (var chunk in chunks)
				ms.Write(chunk, 0, chunk.Length);

			ms.Position = 0;

			return ms;
		}

		void UnpackRATileData(MemoryStream ms)
		{
			for (int i = 0; i < mapSize; i++)
				for (int j = 0; j < mapSize; j++)
					map.MapTiles.Value[i, j] = new TileReference<ushort, byte>();

			for (int j = 0; j < mapSize; j++)
				for (int i = 0; i < mapSize; i++)
					map.MapTiles.Value[i, j].Type = ms.ReadUInt16();

			for (int j = 0; j < mapSize; j++)
				for (int i = 0; i < mapSize; i++)
					map.MapTiles.Value[i, j].Index = ms.ReadUInt8();
		}

		void UnpackRAOverlayData(MemoryStream ms)
		{
			for (int j = 0; j < mapSize; j++)
			{
				for (int i = 0; i < mapSize; i++)
				{
					var o = ms.ReadUInt8();
					var res = Pair.New((byte)0, (byte)0);

					if (o != 255 && overlayResourceMapping.ContainsKey(redAlertOverlayNames[o]))
						res = overlayResourceMapping[redAlertOverlayNames[o]];

					map.MapResources.Value[i, j] = new TileReference<byte, byte>(res.First, res.Second);

					if (o != 255 && overlayActorMapping.ContainsKey(redAlertOverlayNames[o]))
					{
						map.Actors.Value.Add("Actor" + actorCount++,
							new ActorReference(overlayActorMapping[redAlertOverlayNames[o]])
							{
								new LocationInit(new CPos(i, j)),
								new OwnerInit("Neutral")
							});
					}
				}
			}
		}

		void ReadRATrees(IniFile file)
		{
			var terrain = file.GetSection("TERRAIN", true);
			if (terrain == null)
				return;

			foreach (KeyValuePair<string, string> kv in terrain)
			{
				var loc = int.Parse(kv.Key);
				map.Actors.Value.Add("Actor" + actorCount++,
					new ActorReference(kv.Value.ToLowerInvariant())
					{
						new LocationInit(new CPos(loc % mapSize, loc / mapSize)),
						new OwnerInit("Neutral")
					});
			}
		}

		void UnpackCncTileData(Stream ms)
		{
			for (int i = 0; i < mapSize; i++)
				for (int j = 0; j < mapSize; j++)
					map.MapTiles.Value[i, j] = new TileReference<ushort, byte>();

			for (int j = 0; j < mapSize; j++)
			{
				for (int i = 0; i < mapSize; i++)
				{
					map.MapTiles.Value[i, j].Type = ms.ReadUInt8();
					map.MapTiles.Value[i, j].Index = ms.ReadUInt8();
				}
			}
		}

		void ReadCncOverlay(IniFile file)
		{
			var overlay = file.GetSection("OVERLAY", true);
			if (overlay == null)
				return;

			foreach (KeyValuePair<string, string> kv in overlay)
			{
				var loc = int.Parse(kv.Key);
				var cell = new CPos(loc % mapSize, loc / mapSize);

				var res = Pair.New((byte)0, (byte)0);
				if (overlayResourceMapping.ContainsKey(kv.Value.ToLower()))
					res = overlayResourceMapping[kv.Value.ToLower()];

				map.MapResources.Value[cell.X, cell.Y] = new TileReference<byte, byte>(res.First, res.Second);

				if (overlayActorMapping.ContainsKey(kv.Value.ToLower()))
					map.Actors.Value.Add("Actor" + actorCount++,
						new ActorReference(overlayActorMapping[kv.Value.ToLower()])
						{
							new LocationInit(cell),
							new OwnerInit("Neutral")
						});
			}
		}

		void ReadCncTrees(IniFile file)
		{
			var terrain = file.GetSection("TERRAIN", true);
			if (terrain == null)
				return;

			foreach (KeyValuePair<string, string> kv in terrain)
			{
				var loc = int.Parse(kv.Key);
				map.Actors.Value.Add("Actor" + actorCount++,
					new ActorReference(kv.Value.Split(',')[0].ToLowerInvariant())
					{
						new LocationInit(new CPos(loc % mapSize, loc / mapSize)),
						new OwnerInit("Neutral")
					});
			}
		}

		void LoadActors(IniFile file, string section)
		{
			foreach (var s in file.GetSection(section, true))
			{
				// Structures: num=owner,type,health,location,turret-facing,trigger
				// Units: num=owner,type,health,location,facing,action,trigger
				// Infantry: num=owner,type,health,location,subcell,action,facing,trigger
				try
				{
					var parts = s.Value.Split(',');
					var loc = int.Parse(parts[3]);
					if (parts[0] == "")
						parts[0] = "Neutral";

					if (!players.Contains(parts[0]))
						players.Add(parts[0]);

					var actor = new ActorReference(parts[1].ToLowerInvariant())
					{
						new LocationInit(new CPos(loc % mapSize, loc / mapSize)),
						new OwnerInit(parts[0]),
						new HealthInit(float.Parse(parts[2], NumberFormatInfo.InvariantInfo) / 256),
						new FacingInit((section == "INFANTRY") ? int.Parse(parts[6]) : int.Parse(parts[4])),
					};

					if (section == "INFANTRY")
						actor.Add(new SubCellInit(int.Parse(parts[4])));

					if (!Rules.Info.ContainsKey(parts[1].ToLowerInvariant()))
						errorHandler("Ignoring unknown actor type: `{0}`".F(parts[1].ToLowerInvariant()));
					else
						map.Actors.Value.Add("Actor" + actorCount++, actor);
				}
				catch (Exception)
				{
					errorHandler("Malformed actor definition: `{0}`".F(s));
				}
			}
		}

		void LoadSmudges(IniFile file, string section)
		{
			foreach (var s in file.GetSection(section, true))
			{
				// loc=type,loc,depth
				var parts = s.Value.Split(',');
				var loc = int.Parse(parts[1]);
				map.Smudges.Value.Add(new SmudgeReference(parts[0].ToLowerInvariant(), new int2(loc % mapSize, loc / mapSize), int.Parse(parts[2])));
			}
		}

		void LoadPlayer(IniFile file, string section, bool isRA)
		{
			string c;
			string race;
			switch (section)
			{
				case "Spain":
					c = "gold";
					race = "allies";
					break;
				case "England":
					c = "green";
					race = "allies";
					break;
				case "Ukraine":
					c = "orange";
					race = "soviet";
					break;
				case "Germany":
					c = "black";
					race = "allies";
					break;
				case "France":
					c = "teal";
					race = "allies";
					break;
				case "Turkey":
					c = "salmon";
					race = "allies";
					break;
				case "Greece":
				case "GoodGuy":
					c = isRA ? "blue" : "gold";
					race = isRA ? "allies" : "gdi";
					break;
				case "USSR":
				case "BadGuy":
					c = "red";
					race = isRA ? "soviet" : "nod";
					break;
				case "Special":
				case "Neutral":
				default:
					c = "neutral";
					race = isRA ? "allies" : "gdi";
					break;
			}

			var pr = new PlayerReference
			{
				Name = section,
				OwnsWorld = section == "Neutral",
				NonCombatant = section == "Neutral",
				Race = race,
				Color = namedColorMapping[c]
			};

			var neutral = new[] { "Neutral" };
			foreach (var s in file.GetSection(section, true))
			{
				switch (s.Key)
				{
					case "Allies":
						pr.Allies = s.Value.Split(',').Intersect(players).Except(neutral).ToArray();
						pr.Enemies = s.Value.Split(',').SymmetricDifference(players).Except(neutral).ToArray();
						break;
					default:
						Console.WriteLine("Ignoring unknown {0}={1} for player {2}", s.Key, s.Value, pr.Name);
						break;
				}
			}

			map.Players.Add(section, pr);
		}
	}
}
