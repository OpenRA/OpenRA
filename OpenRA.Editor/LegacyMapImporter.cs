#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Traits;
using System.Drawing;
using System.Globalization;

namespace OpenRA.Editor
{
	public class LegacyMapImporter
	{
		// Mapping from ra overlay index to type string
		static string[] raOverlayNames =
		{
			"sbag", "cycl", "brik", "fenc", "wood",
			"gold01", "gold02", "gold03", "gold04",
			"gem01", "gem02", "gem03", "gem04",
			"v12", "v13", "v14", "v15", "v16", "v17", "v18",
			"fpls", "wcrate", "scrate", "barb", "sbag",
		};

		static Dictionary<string, Pair<byte, byte>> overlayResourceMapping = new Dictionary<string, Pair<byte, byte>>()
		{
			// RA Gems, Gold
			{ "gold01", new Pair<byte,byte>(1,0) },
			{ "gold02", new Pair<byte,byte>(1,1) },
			{ "gold03", new Pair<byte,byte>(1,2) },
			{ "gold04", new Pair<byte,byte>(1,3) },
			
			{ "gem01", new Pair<byte,byte>(2,0) },
			{ "gem02", new Pair<byte,byte>(2,1) },
			{ "gem03", new Pair<byte,byte>(2,2) },
			{ "gem04", new Pair<byte,byte>(2,3) },
			
			// cnc tiberium
			{ "ti1", new Pair<byte,byte>(1,0) },
			{ "ti2", new Pair<byte,byte>(1,1) },
			{ "ti3", new Pair<byte,byte>(1,2) },
			{ "ti4", new Pair<byte,byte>(1,3) },
			{ "ti5", new Pair<byte,byte>(1,4) },
			{ "ti6", new Pair<byte,byte>(1,5) },
			{ "ti7", new Pair<byte,byte>(1,6) },
			{ "ti8", new Pair<byte,byte>(1,7) },
			{ "ti9", new Pair<byte,byte>(1,8) },
			{ "ti10", new Pair<byte,byte>(1,9) },
			{ "ti11", new Pair<byte,byte>(1,10) },
			{ "ti12", new Pair<byte,byte>(1,11) },
		};

		static Dictionary<string, string> overlayActorMapping = new Dictionary<string, string>() {
			// Fences
			{"sbag","sbag"},
			{"cycl","cycl"},
			{"brik","brik"},
			{"fenc","fenc"},
			{"wood","wood"},
			
			// Fields
			{"v12","v12"},
			{"v13","v13"},
			{"v14","v14"},
			{"v15","v15"},
			{"v16","v16"},
			{"v17","v17"},
			{"v18","v18"},
			
			// Crates
//			{"wcrate","crate"},
//			{"scrate","crate"},
		};
		
		static Dictionary<string,Pair<Color,Color>> namedColorMapping = new Dictionary<string, Pair<Color, Color>>()
		{
			{"gold",Pair.New(Color.FromArgb(246,214,121),Color.FromArgb(40,32,8))},
			{"blue",Pair.New(Color.FromArgb(226,230,246),Color.FromArgb(8,20,52))},
			{"red",Pair.New(Color.FromArgb(255,20,0),Color.FromArgb(56,0,0))},
			{"neutral",Pair.New(Color.FromArgb(238,238,238),Color.FromArgb(44,28,24))},
			{"orange",Pair.New(Color.FromArgb(255,230,149),Color.FromArgb(56,0,0))},
			{"teal",Pair.New(Color.FromArgb(93,194,165),Color.FromArgb(0,32,32))},
			{"salmon",Pair.New(Color.FromArgb(210,153,125),Color.FromArgb(56,0,0))},
			{"green",Pair.New(Color.FromArgb(160,240,140),Color.FromArgb(20,20,20))},
			{"white",Pair.New(Color.FromArgb(255,255,255),Color.FromArgb(75,75,75))},
			{"black",Pair.New(Color.FromArgb(80,80,80),Color.FromArgb(5,5,5))},
		};
		
		int MapSize;
		int ActorCount = 0;
		Map Map = new Map();
		List<string> Players = new List<string>();
		
		LegacyMapImporter(string filename)
		{
			ConvertIniMap(filename);
		}

		public static Map Import(string filename)
		{
			var converter = new LegacyMapImporter(filename);
			return converter.Map;
		}

		enum IniMapFormat { RedAlert = 3, /* otherwise, cnc (2 variants exist, we don't care to differentiate) */ };
		
		public void ConvertIniMap(string iniFile)
		{
			var file = new IniFile(FileSystem.Open(iniFile));
			var basic = file.GetSection("Basic");
			var map = file.GetSection("Map");
			var legacyMapFormat = (IniMapFormat)int.Parse(basic.GetValue("NewINIFormat", "0"));
			var XOffset = int.Parse(map.GetValue("X", "0"));
			var YOffset = int.Parse(map.GetValue("Y", "0"));
			var Width = int.Parse(map.GetValue("Width", "0"));
			var Height = int.Parse(map.GetValue("Height", "0"));
			MapSize = (legacyMapFormat == IniMapFormat.RedAlert) ? 128 : 64;

			Map.Title = basic.GetValue("Name", "(null)");
			Map.Author = "Westwood Studios";
			Map.Tileset = Truncate(map.GetValue("Theater", "TEMPERAT"), 8);
			Map.MapSize.X = MapSize;
			Map.MapSize.Y = MapSize;
			Map.TopLeft = new int2(XOffset, YOffset);
			Map.BottomRight = new int2(XOffset + Width, YOffset + Height);
			Map.Selectable = true;

			if (legacyMapFormat == IniMapFormat.RedAlert)
			{
				UnpackRATileData(ReadPackedSection(file.GetSection("MapPack")));
				UnpackRAOverlayData(ReadPackedSection(file.GetSection("OverlayPack")));
				ReadRATrees(file);
			}
			else // CNC
			{
				UnpackCncTileData(FileSystem.Open(iniFile.Substring(0, iniFile.Length - 4) + ".bin"));
				ReadCncOverlay(file);
				ReadCncTrees(file);
			}

			LoadActors(file, "STRUCTURES");
			LoadActors(file, "UNITS");
			LoadActors(file, "INFANTRY");
			LoadSmudges(file, "SMUDGE");

			foreach (var p in Players)
				LoadPlayer(file, p, (legacyMapFormat == IniMapFormat.RedAlert));
			
			var wp = file.GetSection("Waypoints")
					.Where(kv => int.Parse(kv.Value) > 0)
					.Select(kv => Pair.New(int.Parse(kv.Key),
						LocationFromMapOffset(int.Parse(kv.Value), MapSize)))
					.ToArray();

			Map.PlayerCount = wp.Count();

			foreach (var kv in wp)
				Map.Waypoints.Add("spawn" + kv.First, kv.Second);
		}

		static int2 LocationFromMapOffset(int offset, int mapSize)
		{
			return new int2(offset % mapSize, offset / mapSize);
		}

		static MemoryStream ReadPackedSection(IniSection mapPackSection)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 1; ; i++)
			{
				string line = mapPackSection.GetValue(i.ToString(), null);
				if (line == null)
					break;

				sb.Append(line.Trim());
			}

			byte[] data = Convert.FromBase64String(sb.ToString());
			List<byte[]> chunks = new List<byte[]>();
			BinaryReader reader = new BinaryReader(new MemoryStream(data));

			try
			{
				while (true)
				{
					uint length = reader.ReadUInt32() & 0xdfffffff;
					byte[] dest = new byte[8192];
					byte[] src = reader.ReadBytes((int)length);

					/*int actualLength =*/
					Format80.DecodeInto(src, dest);

					chunks.Add(dest);
				}
			}
			catch (EndOfStreamException) { }

			MemoryStream ms = new MemoryStream();
			foreach (byte[] chunk in chunks)
				ms.Write(chunk, 0, chunk.Length);

			ms.Position = 0;

			return ms;
		}

		static byte ReadByte(Stream s)
		{
			int ret = s.ReadByte();
			if (ret == -1)
				throw new NotImplementedException();
			return (byte)ret;
		}

		static ushort ReadWord(Stream s)
		{
			ushort ret = ReadByte(s);
			ret |= (ushort)(ReadByte(s) << 8);

			return ret;
		}

		void UnpackRATileData(MemoryStream ms)
		{
			Map.MapTiles = new TileReference<ushort, byte>[MapSize, MapSize];
			for (int i = 0; i < MapSize; i++)
				for (int j = 0; j < MapSize; j++)
					Map.MapTiles[i, j] = new TileReference<ushort, byte>();

			for (int j = 0; j < MapSize; j++)
				for (int i = 0; i < MapSize; i++)
					Map.MapTiles[i, j].type = ReadWord(ms);

			for (int j = 0; j < MapSize; j++)
				for (int i = 0; i < MapSize; i++)
				{
					Map.MapTiles[i, j].index = ReadByte(ms);
					if (Map.MapTiles[i, j].type == 0xff || Map.MapTiles[i, j].type == 0xffff)
						Map.MapTiles[i, j].index = byte.MaxValue;
				}
		}

		void UnpackRAOverlayData(MemoryStream ms)
		{
			Map.MapResources = new TileReference<byte, byte>[MapSize, MapSize];
			for (int j = 0; j < MapSize; j++)
				for (int i = 0; i < MapSize; i++)
				{
					byte o = ReadByte(ms);
					var res = Pair.New((byte)0, (byte)0);

					if (o != 255 && overlayResourceMapping.ContainsKey(raOverlayNames[o]))
						res = overlayResourceMapping[raOverlayNames[o]];

					Map.MapResources[i, j] = new TileReference<byte, byte>(res.First, res.Second);

					if (o != 255 && overlayActorMapping.ContainsKey(raOverlayNames[o]))
						Map.Actors.Add("Actor" + ActorCount++,
							new ActorReference(overlayActorMapping[raOverlayNames[o]]) 
							{ 
								new LocationInit( new int2(i, j) ),
								new OwnerInit( "Neutral" ) 
							});
				}
		}

		void ReadRATrees(IniFile file)
		{
			IniSection terrain = file.GetSection("TERRAIN", true);
			if (terrain == null)
				return;

			foreach (KeyValuePair<string, string> kv in terrain)
			{
				var loc = int.Parse(kv.Key);
				Map.Actors.Add("Actor" + ActorCount++, 
					new ActorReference(kv.Value.ToLowerInvariant())
					{
						new LocationInit(new int2(loc % MapSize, loc / MapSize)),
						new OwnerInit("Neutral")
					});
			}
		}

		void UnpackCncTileData(Stream ms)
		{
			Map.MapTiles = new TileReference<ushort, byte>[MapSize, MapSize];
			for (int i = 0; i < MapSize; i++)
				for (int j = 0; j < MapSize; j++)
					Map.MapTiles[i, j] = new TileReference<ushort, byte>();

			for (int j = 0; j < MapSize; j++)
				for (int i = 0; i < MapSize; i++)
				{
					Map.MapTiles[i, j].type = ReadByte(ms);
					Map.MapTiles[i, j].index = ReadByte(ms);

					if (Map.MapTiles[i, j].type == 0xff)
						Map.MapTiles[i, j].index = byte.MaxValue;
				}
		}

		void ReadCncOverlay(IniFile file)
		{
			IniSection overlay = file.GetSection("OVERLAY", true);
			if (overlay == null)
				return;

			Map.MapResources = new TileReference<byte, byte>[MapSize, MapSize];
			foreach (KeyValuePair<string, string> kv in overlay)
			{
				var loc = int.Parse(kv.Key);
				int2 cell = new int2(loc % MapSize, loc / MapSize);

				var res = Pair.New((byte)0, (byte)0);
				if (overlayResourceMapping.ContainsKey(kv.Value.ToLower()))
					res = overlayResourceMapping[kv.Value.ToLower()];

				Map.MapResources[cell.X, cell.Y] = new TileReference<byte, byte>(res.First, res.Second);

				if (overlayActorMapping.ContainsKey(kv.Value.ToLower()))
					Map.Actors.Add("Actor" + ActorCount++, 
						new ActorReference(overlayActorMapping[kv.Value.ToLower()])
						{
							new LocationInit(cell),
							new OwnerInit("Neutral")
						});
			}
		}

		void ReadCncTrees(IniFile file)
		{
			IniSection terrain = file.GetSection("TERRAIN", true);
			if (terrain == null)
				return;

			foreach (KeyValuePair<string, string> kv in terrain)
			{
				var loc = int.Parse(kv.Key);
				Map.Actors.Add("Actor" + ActorCount++,
					new ActorReference(kv.Value.Split(',')[0].ToLowerInvariant())
					{
						new LocationInit(new int2(loc % MapSize, loc / MapSize)),
						new OwnerInit("Neutral")
					});
			}
		}

		void LoadActors(IniFile file, string section)
		{
			foreach (var s in file.GetSection(section, true))
			{
				//Structures: num=owner,type,health,location,turret-facing,trigger
				//Units: num=owner,type,health,location,facing,action,trigger
				//Infantry: num=owner,type,health,location,subcell,action,facing,trigger
				var parts = s.Value.Split(',');
				var loc = int.Parse(parts[3]);
				if (parts[0] == "")
					parts[0] = "Neutral";
				
				
				if (!Players.Contains(parts[0]))
					Players.Add(parts[0]);
				
				var stance = ActorStance.Stance.None;
				switch(parts[5])
				{
					case "Area Guard":
					case "Guard":
						stance = ActorStance.Stance.Guard;
					break;
					case "Defend Base":
						stance = ActorStance.Stance.Defend;
					break;
					case "Hunt":
					case "Rampage":
					case "Attack Base":
					case "Attack Units":
					case "Attack Civil.":
					case "Attack Tarcom":
						stance = ActorStance.Stance.Hunt;
					break;
					case "Retreat":
					case "Return":
						stance = ActorStance.Stance.Retreat;
					break;
					// do we care about `Harvest' and `Sticky'?
				}
				
				var actor = new ActorReference(parts[1].ToLowerInvariant())
				{
					new LocationInit(new int2(loc % MapSize, loc / MapSize)),
					new OwnerInit(parts[0]),
					new HealthInit(float.Parse(parts[2], NumberFormatInfo.InvariantInfo)/256),
					new FacingInit((section == "INFANTRY") ? int.Parse(parts[6]) : int.Parse(parts[4])),
					new ActorStanceInit(stance),
				};
				
				if (section == "INFANTRY")
					actor.Add(new SubcellInit(int.Parse(parts[4])));
				
				Map.Actors.Add("Actor" + ActorCount++,actor);
					
			}
		}

		void LoadSmudges(IniFile file, string section)
		{
			foreach (var s in file.GetSection(section, true))
			{
				//loc=type,loc,depth
				var parts = s.Value.Split(',');
				var loc = int.Parse(parts[1]);
				Map.Smudges.Add(new SmudgeReference(parts[0].ToLowerInvariant(), new int2(loc % MapSize, loc / MapSize), int.Parse(parts[2])));
			}
		}
		
		void LoadPlayer(IniFile file, string section, bool isRA)
		{			
			var c = (section == "BadGuy") ? "red" :
						(isRA) ? "blue" : "gold";
			
			var color = namedColorMapping[c];
			
			var pr = new PlayerReference
			{
				Name = section,
				OwnsWorld = (section == "Neutral"),
				NonCombatant = (section == "Neutral"),
				Race = (isRA) ? ((section == "BadGuy") ? "soviet" : "allies") : ((section == "BadGuy") ? "nod" : "gdi"),
				Color = color.First,
				Color2 = color.Second,
			};
			
			var Neutral = new List<string>(){"Neutral"};
			foreach (var s in file.GetSection(section, true))
			{
				Console.WriteLine(s.Key);
				switch(s.Key)
				{
					case "Credits":
						pr.InitialCash = int.Parse(s.Value);
					break;
					case "Allies":
						pr.Allies = s.Value.Split(',').Intersect(Players).Except(Neutral).ToArray();
						pr.Enemies = s.Value.Split(',').SymmetricDifference(Players).Except(Neutral).ToArray();
					break;
				}
			}
			
			Map.Players.Add(section, pr);
		}

		static string Truncate(string s, int maxLength)
		{
			return s.Length <= maxLength ? s : s.Substring(0, maxLength);
		}
	}
}
