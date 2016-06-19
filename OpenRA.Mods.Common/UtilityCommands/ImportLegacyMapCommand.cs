#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public abstract class ImportLegacyMapCommand
	{
		public readonly int MapSize;

		public ImportLegacyMapCommand(int mapSize)
		{
			MapSize = mapSize;
		}

		public ModData ModData;
		public Map Map;
		public List<string> Players = new List<string>();
		public MapPlayers MapPlayers;
		bool singlePlayer;
		int spawnCount;

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("FILENAME", "Convert a legacy INI/MPR map to the OpenRA format.")]
		public virtual void Run(ModData modData, string[] args)
		{
			ModData = modData;

			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var filename = args[1];
			using (var stream = File.OpenRead(filename))
			{
				var file = new IniFile(stream);
				var basic = file.GetSection("Basic");

				var player = basic.GetValue("Player", string.Empty);
				if (!string.IsNullOrEmpty(player))
					singlePlayer = !player.StartsWith("Multi");

				var mapSection = file.GetSection("Map");

				var format = GetMapFormatVersion(basic);
				ValidateMapFormat(format);

				// The original game isn't case sensitive, but we are.
				var tileset = GetTileset(mapSection).ToUpperInvariant();
				if (!modData.DefaultTileSets.ContainsKey(tileset))
					throw new InvalidDataException("Unknown tileset {0}".F(tileset));

				Map = new Map(modData, modData.DefaultTileSets[tileset], MapSize, MapSize)
				{
					Title = basic.GetValue("Name", Path.GetFileNameWithoutExtension(filename)),
					Author = "Westwood Studios",
				};

				Map.RequiresMod = modData.Manifest.Mod.Id;

				SetBounds(Map, mapSection);

				ReadPacks(file, filename);
				ReadTrees(file);

				LoadVideos(file, "BASIC");
				LoadBriefing(file);

				ReadActors(file);

				LoadSmudges(file, "SMUDGE");

				var waypoints = file.GetSection("Waypoints");
				LoadWaypoints(waypoints);

				// Create default player definitions only if there are no players to import
				MapPlayers = new MapPlayers(Map.Rules, Players.Count == 0 ? spawnCount : 0);
				foreach (var p in Players)
					LoadPlayer(file, p);

				Map.PlayerDefinitions = MapPlayers.ToMiniYaml();
			}

			Map.FixOpenAreas();

			var dest = Path.GetFileNameWithoutExtension(args[1]) + ".oramap";

			Map.Save(ZipFile.Create(dest, new Folder(".")));
			Console.WriteLine(dest + " saved.");
		}

		/*
		 * 1=Tiberium Dawn & Sole Survivor
		 * 2=Red Alert (also with Counterstrike installed)
		 * 3=Red Alert (with Aftermath installed)
		 * 4=Tiberian Sun (including Firestorm) & Red Alert 2 (including Yuri's Revenge)
		 */
		static int GetMapFormatVersion(IniSection basicSection)
		{
			var iniFormat = basicSection.GetValue("NewINIFormat", "0");

			var iniFormatVersion = 0;
			Exts.TryParseIntegerInvariant(iniFormat, out iniFormatVersion);

			return iniFormatVersion;
		}

		public abstract void ValidateMapFormat(int format);

		void LoadBriefing(IniFile file)
		{
			var briefingSection = file.GetSection("Briefing", true);
			if (briefingSection == null)
				return;

			var briefing = new StringBuilder();
			foreach (var s in briefingSection)
				briefing.AppendLine(s.Value);

			if (briefing.Length == 0)
				return;

			var worldNode = Map.RuleDefinitions.Nodes.FirstOrDefault(n => n.Key == "World");
			if (worldNode == null)
			{
				worldNode = new MiniYamlNode("World", new MiniYaml("", new List<MiniYamlNode>()));
				Map.RuleDefinitions.Nodes.Add(worldNode);
			}

			var missionData = worldNode.Value.Nodes.FirstOrDefault(n => n.Key == "MissionData");
			if (missionData == null)
			{
				missionData = new MiniYamlNode("MissionData", new MiniYaml("", new List<MiniYamlNode>()));
				worldNode.Value.Nodes.Add(missionData);
			}

			missionData.Value.Nodes.Add(new MiniYamlNode("Briefing", briefing.Replace("\n", " ").ToString()));
		}

		static void SetBounds(Map map, IniSection mapSection)
		{
			var offsetX = Exts.ParseIntegerInvariant(mapSection.GetValue("X", "0"));
			var offsetY = Exts.ParseIntegerInvariant(mapSection.GetValue("Y", "0"));
			var width = Exts.ParseIntegerInvariant(mapSection.GetValue("Width", "0"));
			var height = Exts.ParseIntegerInvariant(mapSection.GetValue("Height", "0"));

			var tl = new PPos(offsetX, offsetY);
			var br = new PPos(offsetX + width - 1, offsetY + height - 1);
			map.SetBounds(tl, br);
		}

		public abstract void ReadPacks(IniFile file, string filename);

		void LoadVideos(IniFile file, string section)
		{
			var videos = new List<MiniYamlNode>();
			foreach (var s in file.GetSection(section))
			{
				if (s.Value != "x" && s.Value != "X" && s.Value != "<none>")
				{
					switch (s.Key)
					{
					case "Intro":
						videos.Add(new MiniYamlNode("BackgroundVideo", s.Value.ToLower() + ".vqa"));
						break;
					case "Brief":
						videos.Add(new MiniYamlNode("BriefingVideo", s.Value.ToLower() + ".vqa"));
						break;
					case "Action":
						videos.Add(new MiniYamlNode("StartVideo", s.Value.ToLower() + ".vqa"));
						break;
					case "Win":
						videos.Add(new MiniYamlNode("WinVideo", s.Value.ToLower() + ".vqa"));
						break;
					case "Lose":
						videos.Add(new MiniYamlNode("LossVideo", s.Value.ToLower() + ".vqa"));
						break;
					}
				}
			}

			if (videos.Any())
			{
				var worldNode = Map.RuleDefinitions.Nodes.FirstOrDefault(n => n.Key == "World");
				if (worldNode == null)
				{
					worldNode = new MiniYamlNode("World", new MiniYaml("", new List<MiniYamlNode>()));
					Map.RuleDefinitions.Nodes.Add(worldNode);
				}

				var missionData = worldNode.Value.Nodes.FirstOrDefault(n => n.Key == "MissionData");
				if (missionData == null)
				{
					missionData = new MiniYamlNode("MissionData", new MiniYaml("", new List<MiniYamlNode>()));
					worldNode.Value.Nodes.Add(missionData);
				}

				missionData.Value.Nodes.AddRange(videos);
			}
		}

		public virtual void ReadActors(IniFile file)
		{
			LoadActors(file, "STRUCTURES", Players, MapSize, Map);
			LoadActors(file, "UNITS", Players, MapSize, Map);
			LoadActors(file, "INFANTRY", Players, MapSize, Map);
		}

		public abstract void LoadPlayer(IniFile file, string section);

		static string Truncate(string s, int maxLength)
		{
			return s.Length <= maxLength ? s : s.Substring(0, maxLength);
		}

		static string GetTileset(IniSection mapSection)
		{
			// NOTE: The original isn't case sensitive, we are.
			// NOTE: Tileset TEMPERAT exists in every C&C game.
			return Truncate(mapSection.GetValue("Theater", "TEMPERAT"), 8).ToUpperInvariant();
		}

		static int2 LocationFromMapOffset(int offset, int mapSize)
		{
			return new int2(offset % mapSize, offset / mapSize);
		}

		void LoadWaypoints(IniSection waypointSection)
		{
			var actorCount = Map.ActorDefinitions.Count;
			var wps = waypointSection
				.Where(kv => Exts.ParseIntegerInvariant(kv.Value) > 0)
				.Select(kv => Pair.New(Exts.ParseIntegerInvariant(kv.Key),
					LocationFromMapOffset(Exts.ParseIntegerInvariant(kv.Value), MapSize)));

			// Add waypoint actors
			foreach (var kv in wps)
			{
				if (!singlePlayer && kv.First <= 7)
				{
					var ar = new ActorReference("mpspawn")
					{
						new LocationInit((CPos)kv.Second),
						new OwnerInit("Neutral")
					};

					Map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, ar.Save()));
					spawnCount++;
				}
				else
				{
					var ar = new ActorReference("waypoint")
					{
						new LocationInit((CPos)kv.Second),
						new OwnerInit("Neutral")
					};

					SaveWaypoint(kv.First, ar);
				}
			}
		}

		public virtual void SaveWaypoint(int waypointNumber, ActorReference waypointReference)
		{
			var waypointName = "waypoint" + waypointNumber;
			Map.ActorDefinitions.Add(new MiniYamlNode(waypointName, waypointReference.Save()));
		}

		void LoadSmudges(IniFile file, string section)
		{
			var scorches = new List<MiniYamlNode>();
			var craters = new List<MiniYamlNode>();
			foreach (var s in file.GetSection(section, true))
			{
				// loc=type,loc,depth
				var parts = s.Value.Split(',');
				var loc = Exts.ParseIntegerInvariant(parts[1]);
				var type = parts[0].ToLowerInvariant();
				var key = "{0},{1}".F(loc % MapSize, loc / MapSize);
				var value = "{0},{1}".F(type, parts[2]);
				var node = new MiniYamlNode(key, value);
				if (type.StartsWith("sc"))
					scorches.Add(node);
				else if (type.StartsWith("cr"))
					craters.Add(node);
			}

			var worldNode = Map.RuleDefinitions.Nodes.FirstOrDefault(n => n.Key == "World");
			if (worldNode == null)
				worldNode = new MiniYamlNode("World", new MiniYaml("", new List<MiniYamlNode>()));

			if (scorches.Any())
			{
				var initialScorches = new MiniYamlNode("InitialSmudges", new MiniYaml("", scorches));
				var smudgeLayer = new MiniYamlNode("SmudgeLayer@SCORCH", new MiniYaml("", new List<MiniYamlNode>() { initialScorches }));
				worldNode.Value.Nodes.Add(smudgeLayer);
			}

			if (craters.Any())
			{
				var initialCraters = new MiniYamlNode("InitialSmudges", new MiniYaml("", craters));
				var smudgeLayer = new MiniYamlNode("SmudgeLayer@CRATER", new MiniYaml("", new List<MiniYamlNode>() { initialCraters }));
				worldNode.Value.Nodes.Add(smudgeLayer);
			}

			if (worldNode.Value.Nodes.Any() && !Map.RuleDefinitions.Nodes.Contains(worldNode))
				Map.RuleDefinitions.Nodes.Add(worldNode);
		}

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

		public static void SetMapPlayers(string section, string faction, string color, IniFile file, List<string> players, MapPlayers mapPlayers)
		{
			var pr = new PlayerReference
			{
				Name = section,
				OwnsWorld = section == "Neutral",
				NonCombatant = section == "Neutral",
				Faction = faction,
				Color = namedColorMapping[color]
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

			// Overwrite default player definitions if needed
			if (!mapPlayers.Players.ContainsKey(section))
				mapPlayers.Players.Add(section, pr);
			else
				mapPlayers.Players[section] = pr;
		}

		public virtual CPos ParseActorLocation(string input, int loc)
		{
			return new CPos(loc % MapSize, loc / MapSize);
		}

		public void LoadActors(IniFile file, string section, List<string> players, int mapSize, Map map)
		{
			foreach (var s in file.GetSection(section, true))
			{
				// Structures: num=owner,type,health,location,turret-facing,trigger
				// Units: num=owner,type,health,location,facing,action,trigger
				// Infantry: num=owner,type,health,location,subcell,action,facing,trigger
				try
				{
					var parts = s.Value.Split(',');
					if (parts[0] == "")
						parts[0] = "Neutral";

					if (!players.Contains(parts[0]))
						players.Add(parts[0]);

					var loc = Exts.ParseIntegerInvariant(parts[3]);
					var health = Exts.ParseIntegerInvariant(parts[2]) * 100 / 256;
					var facing = (section == "INFANTRY") ? Exts.ParseIntegerInvariant(parts[6]) : Exts.ParseIntegerInvariant(parts[4]);

					var actorType = parts[1].ToLowerInvariant();

					var actor = new ActorReference(actorType) {
						new LocationInit(ParseActorLocation(actorType, loc)),
						new OwnerInit(parts[0]),
					};

					var initDict = actor.InitDict;
					if (health != 100)
						initDict.Add(new HealthInit(health));
					if (facing != 0)
						initDict.Add(new FacingInit(255 - facing));

					if (section == "INFANTRY")
						actor.Add(new SubCellInit(Exts.ParseIntegerInvariant(parts[4])));

					var actorCount = map.ActorDefinitions.Count;

					if (!map.Rules.Actors.ContainsKey(parts[1].ToLowerInvariant()))
						Console.WriteLine("Ignoring unknown actor type: `{0}`".F(parts[1].ToLowerInvariant()));
					else
						map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, actor.Save()));
				}
				catch (Exception)
				{
					Console.WriteLine("Malformed actor definition: `{0}`".F(s));
				}
			}
		}

		public abstract string ParseTreeActor(string input);

		void ReadTrees(IniFile file)
		{
			var terrain = file.GetSection("TERRAIN", true);
			if (terrain == null)
				return;

			foreach (var kv in terrain)
			{
				var loc = Exts.ParseIntegerInvariant(kv.Key);
				var treeActor = ParseTreeActor(kv.Value);

				var ar = new ActorReference(treeActor)
				{
					new LocationInit(ParseActorLocation(treeActor, loc)),
					new OwnerInit("Neutral")
				};

				var actorCount = Map.ActorDefinitions.Count;
				Map.ActorDefinitions.Add(new MiniYamlNode("Actor" + actorCount++, ar.Save()));
			}
		}
	}
}
