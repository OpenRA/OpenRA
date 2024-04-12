#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	public abstract class ImportGen1MapCommand
	{
		public readonly int MapSize;

		protected ImportGen1MapCommand(int mapSize)
		{
			MapSize = mapSize;
		}

		public ModData ModData;
		public Map Map;
		public List<string> Players = new();
		public MapPlayers MapPlayers;
		bool singlePlayer;
		int spawnCount;

		protected bool ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		protected void Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = ModData = utility.ModData;

			var filename = args[1];
			using (var stream = File.OpenRead(filename))
			{
				var file = new IniFile(stream);
				var basic = file.GetSection("Basic");

				var player = basic.GetValue("Player", string.Empty);
				if (!string.IsNullOrEmpty(player))
					singlePlayer = !player.StartsWith("Multi", StringComparison.Ordinal);

				var mapSection = file.GetSection("Map");

				var format = GetMapFormatVersion(basic);
				ValidateMapFormat(format);

				// The original game isn't case sensitive, but we are.
				var tileset = GetTileset(mapSection).ToUpperInvariant();
				if (!ModData.DefaultTerrainInfo.TryGetValue(tileset, out var terrainInfo))
					throw new InvalidDataException($"Unknown tileset {tileset}");

				Map = new Map(ModData, terrainInfo, MapSize, MapSize)
				{
					Title = basic.GetValue("Name", Path.GetFileNameWithoutExtension(filename)),
					Author = "Westwood Studios",
					RequiresMod = ModData.Manifest.Id
				};

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

			if (Map.Rules.TerrainInfo is ITerrainInfoNotifyMapCreated notifyMapCreated)
				notifyMapCreated.MapCreated(Map);

			ReplaceInvalidTerrainTiles(Map);

			var dest = Path.GetFileNameWithoutExtension(args[1]) + ".oramap";

			Map.Save(ZipFileLoader.Create(dest));
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

			Exts.TryParseInt32Invariant(iniFormat, out var iniFormatVersion);

			return iniFormatVersion;
		}

		public abstract void ValidateMapFormat(int format);

		protected MiniYamlNodeBuilder GetWorldNodeBuilderFromRules()
		{
			var worldNode = Map.RuleDefinitions.NodeWithKeyOrDefault("World");
			var worldNodeBuilder = worldNode != null
				? new MiniYamlNodeBuilder(worldNode)
				: new MiniYamlNodeBuilder("World", new MiniYamlBuilder("", new List<MiniYamlNode>()));
			return worldNodeBuilder;
		}

		protected void SaveUpdatedWorldNodeToRules(MiniYamlNodeBuilder worldNodeBuilder)
		{
			var nodes = Map.RuleDefinitions.Nodes.ToList();
			var worldNodeIndex = nodes.FindIndex(n => n.Key == "World");
			if (worldNodeIndex != -1)
				nodes[worldNodeIndex] = worldNodeBuilder.Build();
			else
				nodes.Add(worldNodeBuilder.Build());
			Map.RuleDefinitions = Map.RuleDefinitions.WithNodes(nodes);
		}

		void LoadBriefing(IniFile file)
		{
			var briefingSection = file.GetSection("Briefing", true);
			if (briefingSection == null)
				return;

			var briefing = new StringBuilder();
			foreach (var s in briefingSection)
			{
				var line = s.Value.Replace("@", "\n");
				briefing.AppendLine(line);
			}

			if (briefing.Length == 0)
				return;

			var worldNodeBuilder = GetWorldNodeBuilderFromRules();

			var missionData = worldNodeBuilder.Value.NodeWithKeyOrDefault("MissionData");
			if (missionData == null)
			{
				missionData = new MiniYamlNodeBuilder("MissionData", new MiniYamlBuilder("", new List<MiniYamlNode>()));
				worldNodeBuilder.Value.Nodes.Add(missionData);
			}

			missionData.Value.Nodes.Add(new MiniYamlNodeBuilder("Briefing", briefing.Replace("\n", " ").ToString()));

			SaveUpdatedWorldNodeToRules(worldNodeBuilder);
		}

		static void ReplaceInvalidTerrainTiles(Map map)
		{
			var terrainInfo = map.Rules.TerrainInfo;
			foreach (var uv in map.AllCells.MapCoords)
			{
				if (!terrainInfo.TryGetTerrainInfo(map.Tiles[uv], out _))
				{
					map.Tiles[uv] = terrainInfo.DefaultTerrainTile;
					Console.WriteLine($"Replaced invalid terrain tile at {uv}");
				}
			}
		}

		static void SetBounds(Map map, IniSection mapSection)
		{
			var offsetX = Exts.ParseInt32Invariant(mapSection.GetValue("X", "0"));
			var offsetY = Exts.ParseInt32Invariant(mapSection.GetValue("Y", "0"));
			var width = Exts.ParseInt32Invariant(mapSection.GetValue("Width", "0"));
			var height = Exts.ParseInt32Invariant(mapSection.GetValue("Height", "0"));

			var tl = new PPos(offsetX, offsetY);
			var br = new PPos(offsetX + width - 1, offsetY + height - 1);
			map.SetBounds(tl, br);
		}

		public abstract void ReadPacks(IniFile file, string filename);

		void LoadVideos(IniFile file, string section)
		{
			var videos = new List<MiniYamlNodeBuilder>();
			foreach (var s in file.GetSection(section))
			{
				if (s.Value != "x" && s.Value != "X" && s.Value != "<none>")
				{
					switch (s.Key)
					{
						case "Intro":
							videos.Add(new MiniYamlNodeBuilder("BackgroundVideo", s.Value.ToLowerInvariant() + ".vqa"));
							break;
						case "Brief":
							videos.Add(new MiniYamlNodeBuilder("BriefingVideo", s.Value.ToLowerInvariant() + ".vqa"));
							break;
						case "Action":
							videos.Add(new MiniYamlNodeBuilder("StartVideo", s.Value.ToLowerInvariant() + ".vqa"));
							break;
						case "Win":
							videos.Add(new MiniYamlNodeBuilder("WinVideo", s.Value.ToLowerInvariant() + ".vqa"));
							break;
						case "Lose":
							videos.Add(new MiniYamlNodeBuilder("LossVideo", s.Value.ToLowerInvariant() + ".vqa"));
							break;
					}
				}
			}

			if (videos.Count > 0)
			{
				var worldNodeBuilder = GetWorldNodeBuilderFromRules();

				var missionData = worldNodeBuilder.Value.NodeWithKeyOrDefault("MissionData");
				if (missionData == null)
				{
					missionData = new MiniYamlNodeBuilder("MissionData", new MiniYamlBuilder("", new List<MiniYamlNode>()));
					worldNodeBuilder.Value.Nodes.Add(missionData);
				}

				missionData.Value.Nodes.AddRange(videos);

				SaveUpdatedWorldNodeToRules(worldNodeBuilder);
			}
		}

		public virtual void ReadActors(IniFile file)
		{
			LoadActors(file, "STRUCTURES", Players, Map);
			LoadActors(file, "UNITS", Players, Map);
			LoadActors(file, "INFANTRY", Players, Map);
		}

		public abstract void LoadPlayer(IniFile file, string section);

		static string Truncate(string s, int maxLength)
		{
			return s.Length <= maxLength ? s : s[..maxLength];
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
			var wps = waypointSection
				.Where(kv => Exts.ParseInt32Invariant(kv.Value) > 0)
				.Select(kv => (WaypointNumber: Exts.ParseInt32Invariant(kv.Key),
					Location: LocationFromMapOffset(Exts.ParseInt32Invariant(kv.Value), MapSize)));

			// Add waypoint actors skipping duplicate entries
			var nodes = new List<MiniYamlNode>();
			foreach (var (waypointNumber, location) in wps.DistinctBy(location => location.Location))
			{
				if (!singlePlayer && waypointNumber <= 7)
				{
					var ar = new ActorReference("mpspawn")
					{
						new LocationInit((CPos)location),
						new OwnerInit("Neutral")
					};

					nodes.Add(new MiniYamlNode("Actor" + (Map.ActorDefinitions.Count + nodes.Count), ar.Save()));
					spawnCount++;
				}
				else
				{
					var ar = new ActorReference("waypoint")
					{
						new LocationInit((CPos)location),
						new OwnerInit("Neutral")
					};

					nodes.Add(SaveWaypoint(waypointNumber, ar));
				}
			}

			Map.ActorDefinitions = Map.ActorDefinitions.Concat(nodes).ToArray();
		}

		public virtual MiniYamlNode SaveWaypoint(int waypointNumber, ActorReference waypointReference)
		{
			var waypointName = "waypoint" + waypointNumber;
			return new MiniYamlNode(waypointName, waypointReference.Save());
		}

		void LoadSmudges(IniFile file, string section)
		{
			var scorches = new List<MiniYamlNode>();
			var craters = new List<MiniYamlNode>();
			foreach (var s in file.GetSection(section, true))
			{
				// loc=type,loc,depth
				var parts = s.Value.Split(',');
				var loc = Exts.ParseInt32Invariant(parts[1]);
				var type = parts[0].ToLowerInvariant();
				var key = $"{loc % MapSize},{loc / MapSize}";
				var value = $"{type},{parts[2]}";
				var node = new MiniYamlNode(key, value);
				if (type.StartsWith("sc", StringComparison.Ordinal))
					scorches.Add(node);
				else if (type.StartsWith("cr", StringComparison.Ordinal))
					craters.Add(node);
			}

			var worldNodeBuilder = GetWorldNodeBuilderFromRules();

			if (scorches.Count > 0)
			{
				var initialScorches = new MiniYamlNode("InitialSmudges", new MiniYaml("", scorches));
				var smudgeLayer = new MiniYamlNodeBuilder("SmudgeLayer@SCORCH", new MiniYamlBuilder("", new List<MiniYamlNode>() { initialScorches }));
				worldNodeBuilder.Value.Nodes.Add(smudgeLayer);
			}

			if (craters.Count > 0)
			{
				var initialCraters = new MiniYamlNode("InitialSmudges", new MiniYaml("", craters));
				var smudgeLayer = new MiniYamlNodeBuilder("SmudgeLayer@CRATER", new MiniYamlBuilder("", new List<MiniYamlNode>() { initialCraters }));
				worldNodeBuilder.Value.Nodes.Add(smudgeLayer);
			}

			if (worldNodeBuilder.Value.Nodes.Count > 0)
				SaveUpdatedWorldNodeToRules(worldNodeBuilder);
		}

		// TODO: fix this -- will have bitrotted pretty badly.
		static readonly Dictionary<string, Color> NamedColorMapping = new()
		{
			{ "gold", Color.FromArgb(246, 214, 121) },
			{ "blue", Color.FromArgb(226, 230, 246) },
			{ "red", Color.FromArgb(255, 20, 0) },
			{ "neutral", Color.FromArgb(238, 238, 238) },
			{ "orange", Color.FromArgb(255, 230, 149) },
			{ "teal", Color.FromArgb(93, 194, 165) },
			{ "salmon", Color.FromArgb(210, 153, 125) },
			{ "green", Color.FromArgb(160, 240, 140) },
			{ "white", Color.FromArgb(255, 255, 255) },
			{ "black", Color.FromArgb(80, 80, 80) },
		};

		public static void SetMapPlayers(string section, string faction, string color, IniFile file, List<string> players, MapPlayers mapPlayers)
		{
			var pr = new PlayerReference
			{
				Name = section,
				OwnsWorld = section == "Neutral",
				NonCombatant = section == "Neutral",
				Faction = faction,
				Color = NamedColorMapping[color]
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
			mapPlayers.Players[section] = pr;
		}

		public virtual CPos ParseActorLocation(string input, int loc)
		{
			return new CPos(loc % MapSize, loc / MapSize);
		}

		public void LoadActors(IniFile file, string section, List<string> players, Map map)
		{
			var nodes = new List<MiniYamlNode>();
			foreach (var s in file.GetSection(section, true))
			{
				// Structures: num=owner,type,health,location,turret-facing,trigger
				// Units: num=owner,type,health,location,facing,action,trigger
				// Infantry: num=owner,type,health,location,subcell,action,facing,trigger
				try
				{
					var parts = s.Value.Split(',');
					if (string.IsNullOrEmpty(parts[0]))
						parts[0] = "Neutral";

					if (!players.Contains(parts[0]))
						players.Add(parts[0]);

					var loc = Exts.ParseInt32Invariant(parts[3]);
					var health = Exts.ParseInt32Invariant(parts[2]) * 100 / 256;
					var facing = (section == "INFANTRY") ? Exts.ParseInt32Invariant(parts[6]) : Exts.ParseInt32Invariant(parts[4]);

					var actorType = parts[1].ToLowerInvariant();

					var actor = new ActorReference(actorType)
					{
						new LocationInit(ParseActorLocation(actorType, loc)),
						new OwnerInit(parts[0]),
					};

					if (health != 100)
						actor.Add(new HealthInit(health));
					if (facing != 0)
						actor.Add(new FacingInit(new WAngle(1024 - 4 * facing)));

					if (section == "INFANTRY")
					{
						var subcell = 0;
						switch (Exts.ParseByteInvariant(parts[4]))
						{
							case 1: subcell = 1; break;
							case 2: subcell = 2; break;
							case 3: subcell = 4; break;
							case 4: subcell = 5; break;
						}

						if (subcell != 0)
							actor.Add(new SubCellInit((SubCell)subcell));
					}

					if (!map.Rules.Actors.ContainsKey(parts[1].ToLowerInvariant()))
						Console.WriteLine($"Ignoring unknown actor type: `{parts[1].ToLowerInvariant()}`");
					else
						nodes.Add(new MiniYamlNode("Actor" + (map.ActorDefinitions.Count + nodes.Count), actor.Save()));
				}
				catch (Exception)
				{
					Console.WriteLine($"Malformed actor definition: `{s}`");
				}
			}

			map.ActorDefinitions = map.ActorDefinitions.Concat(nodes).ToArray();
		}

		public abstract string ParseTreeActor(string input);

		void ReadTrees(IniFile file)
		{
			var terrain = file.GetSection("TERRAIN", true);
			if (terrain == null)
				return;

			var nodes = new List<MiniYamlNode>();
			foreach (var kv in terrain)
			{
				var loc = Exts.ParseInt32Invariant(kv.Key);
				var treeActor = ParseTreeActor(kv.Value);

				var ar = new ActorReference(treeActor)
				{
					new LocationInit(ParseActorLocation(treeActor, loc)),
					new OwnerInit("Neutral")
				};

				nodes.Add(new MiniYamlNode("Actor" + (Map.ActorDefinitions.Count + nodes.Count), ar.Save()));
			}

			Map.ActorDefinitions = Map.ActorDefinitions.Concat(nodes).ToArray();
		}
	}

#if !NET6_0_OR_GREATER
	public static class Extensions
	{
		/// <summary>
		/// Only used for Mono builds. .NET 6 added the exact same thing.
		/// </summary>
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			var knownKeys = new HashSet<TKey>();
			foreach (var element in source)
			{
				if (knownKeys.Add(keySelector(element)))
					yield return element;
			}
		}
	}
#endif
}
