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
using OpenRA.Mods.Common.Terrain;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2k.UtilityCommands
{
	public class D2kMapImporter
	{
		const int MapCordonWidth = 2;

		// PlayerReference colors in D2k missions only affect chat text and minimap colors because actors use specific palette colors.
		// So using the colors from the original game's minimap.
		public static Dictionary<string, (string Faction, Color Color)> PlayerReferenceDataByPlayerName = new Dictionary<string, (string, Color)>
		{
			{ "Neutral", ("Random", Color.White) },
			{ "Atreides", ("atreides", Color.FromArgb(90, 115, 148)) },
			{ "Harkonnen", ("harkonnen", Color.FromArgb(214, 74, 66)) },
			{ "Ordos", ("ordos", Color.FromArgb(90, 148, 115)) },
			{ "Corrino", ("corrino", Color.FromArgb(115, 0, 123)) },
			{ "Fremen", ("fremen", Color.FromArgb(132, 132, 132)) },
			{ "Smugglers", ("smuggler", Color.FromArgb(123, 41, 16)) },
			{ "Mercenaries", ("mercenary", Color.FromArgb(156, 132, 8)) }
		};

		public static Dictionary<int, (string Actor, string Owner)> ActorDataByActorCode = new Dictionary<int, (string, string)>
		{
			{ 20, ("wormspawner", "Creeps") },
			{ 23, ("mpspawn", "Neutral") },
			{ 41, ("spicebloom.spawnpoint", "Neutral") },
			{ 42, ("spicebloom.spawnpoint", "Neutral") },
			{ 43, ("spicebloom.spawnpoint", "Neutral") },
			{ 44, ("spicebloom.spawnpoint", "Neutral") },
			{ 45, ("spicebloom.spawnpoint", "Neutral") },

			// Atreides:
			{ 4, ("wall", "Atreides") },
			{ 5, ("wind_trap", "Atreides") },
			{ 8, ("construction_yard", "Atreides") },
			{ 11, ("barracks", "Atreides") },
			{ 14, ("refinery", "Atreides") },
			{ 17, ("outpost", "Atreides") },
			{ 63, ("light_factory", "Atreides") },
			{ 69, ("silo", "Atreides") },
			{ 72, ("heavy_factory", "Atreides") },
			{ 75, ("repair_pad", "Atreides") },
			{ 78, ("medium_gun_turret", "Atreides") },
			{ 120, ("high_tech_factory", "Atreides") },
			{ 123, ("large_gun_turret", "Atreides") },
			{ 126, ("research_centre", "Atreides") },
			{ 129, ("starport", "Atreides") },
			{ 132, ("palace", "Atreides") },
			{ 180, ("light_inf", "Atreides") },
			{ 181, ("trooper", "Atreides") },
			{ 182, ("fremen", "Atreides") },
			{ 183, ("sardaukar", "Atreides") },
			{ 184, ("engineer", "Atreides") },
			{ 185, ("harvester", "Atreides") },
			{ 186, ("mcv", "Atreides") },
			{ 187, ("trike", "Atreides") },
			{ 188, ("quad", "Atreides") },
			{ 189, ("combat_tank_a", "Atreides") },
			{ 190, ("missile_tank", "Atreides") },
			{ 191, ("siege_tank", "Atreides") },
			{ 192, ("carryall", "Atreides") },
			{ 194, ("sonic_tank", "Atreides") },

			// Harkonnen:
			{ 204, ("wall", "Harkonnen") },
			{ 205, ("wind_trap", "Harkonnen") },
			{ 208, ("construction_yard", "Harkonnen") },
			{ 211, ("barracks", "Harkonnen") },
			{ 214, ("refinery", "Harkonnen") },
			{ 217, ("outpost", "Harkonnen") },
			{ 263, ("light_factory", "Harkonnen") },
			{ 269, ("silo", "Harkonnen") },
			{ 272, ("heavy_factory", "Harkonnen") },
			{ 275, ("repair_pad", "Harkonnen") },
			{ 278, ("medium_gun_turret", "Harkonnen") },
			{ 320, ("high_tech_factory", "Harkonnen") },
			{ 323, ("large_gun_turret", "Harkonnen") },
			{ 326, ("research_centre", "Harkonnen") },
			{ 329, ("starport", "Harkonnen") },
			{ 332, ("palace", "Harkonnen") },
			{ 360, ("light_inf", "Harkonnen") },
			{ 361, ("trooper", "Harkonnen") },
			{ 362, ("fremen", "Harkonnen") },
			{ 363, ("mpsardaukar", "Harkonnen") },
			{ 364, ("engineer", "Harkonnen") },
			{ 365, ("harvester", "Harkonnen") },
			{ 366, ("mcv", "Harkonnen") },
			{ 367, ("trike", "Harkonnen") },
			{ 368, ("quad", "Harkonnen") },
			{ 369, ("combat_tank_h", "Harkonnen") },
			{ 370, ("missile_tank", "Harkonnen") },
			{ 371, ("siege_tank", "Harkonnen") },
			{ 372, ("carryall", "Harkonnen") },
			{ 374, ("devastator", "Harkonnen") },

			// Ordos:
			{ 404, ("wall", "Ordos") },
			{ 405, ("wind_trap", "Ordos") },
			{ 408, ("construction_yard", "Ordos") },
			{ 411, ("barracks", "Ordos") },
			{ 414, ("refinery", "Ordos") },
			{ 417, ("outpost", "Ordos") },
			{ 463, ("light_factory", "Ordos") },
			{ 469, ("silo", "Ordos") },
			{ 472, ("heavy_factory", "Ordos") },
			{ 475, ("repair_pad", "Ordos") },
			{ 478, ("medium_gun_turret", "Ordos") },
			{ 520, ("high_tech_factory", "Ordos") },
			{ 523, ("large_gun_turret", "Ordos") },
			{ 526, ("research_centre", "Ordos") },
			{ 529, ("starport", "Ordos") },
			{ 532, ("palace", "Ordos") },
			{ 560, ("light_inf", "Ordos") },
			{ 561, ("trooper", "Ordos") },
			{ 562, ("saboteur", "Ordos") },
			{ 563, ("sardaukar", "Ordos") },
			{ 564, ("engineer", "Ordos") },
			{ 565, ("harvester", "Ordos") },
			{ 566, ("mcv", "Ordos") },
			{ 567, ("raider", "Ordos") },
			{ 568, ("quad", "Ordos") },
			{ 569, ("combat_tank_o", "Ordos") },
			{ 570, ("missile_tank", "Ordos") },
			{ 571, ("siege_tank", "Ordos") },
			{ 572, ("carryall", "Ordos") },
			{ 574, ("deviator", "Ordos") },

			// Corrino:
			{ 580, ("wall", "Corrino") },
			{ 581, ("wind_trap", "Corrino") },
			{ 582, ("construction_yard", "Corrino") },
			{ 583, ("barracks", "Corrino") },
			{ 584, ("refinery", "Corrino") },
			{ 585, ("outpost", "Corrino") },
			{ 587, ("light_factory", "Corrino") },
			{ 588, ("palace", "Corrino") },
			{ 589, ("silo", "Corrino") },
			{ 590, ("heavy_factory", "Corrino") },
			{ 591, ("repair_pad", "Corrino") },
			{ 592, ("medium_gun_turret", "Corrino") },
			{ 593, ("high_tech_factory", "Corrino") },
			{ 594, ("large_gun_turret", "Corrino") },
			{ 595, ("research_centre", "Corrino") },
			{ 596, ("starport", "Corrino") },
			{ 597, ("sietch", "Corrino") },
			{ 598, ("light_inf", "Corrino") },
			{ 599, ("trooper", "Corrino") },
			{ 600, ("sardaukar", "Corrino") },
			{ 601, ("fremen", "Corrino") },
			{ 602, ("engineer", "Corrino") },
			{ 603, ("harvester", "Corrino") },
			{ 604, ("mcv", "Corrino") },
			{ 605, ("trike", "Corrino") },
			{ 606, ("quad", "Corrino") },
			{ 607, ("combat_tank_h", "Corrino") },
			{ 608, ("missile_tank", "Corrino") },
			{ 609, ("siege_tank", "Corrino") },
			{ 610, ("carryall", "Corrino") },

			// Fremen:
			{ 620, ("wall", "Fremen") },
			{ 621, ("wind_trap", "Fremen") },
			{ 622, ("construction_yard", "Fremen") },
			{ 623, ("barracks", "Fremen") },
			{ 624, ("refinery", "Fremen") },
			{ 625, ("outpost", "Fremen") },
			{ 627, ("light_factory", "Fremen") },
			{ 628, ("palace", "Fremen") },
			{ 629, ("silo", "Fremen") },
			{ 630, ("heavy_factory", "Fremen") },
			{ 631, ("repair_pad", "Fremen") },
			{ 632, ("medium_gun_turret", "Fremen") },
			{ 633, ("high_tech_factory", "Fremen") },
			{ 634, ("large_gun_turret", "Fremen") },
			{ 635, ("research_centre", "Fremen") },
			{ 636, ("starport", "Fremen") },
			{ 637, ("sietch", "Fremen") },
			{ 638, ("light_inf", "Fremen") },
			{ 639, ("trooper", "Fremen") },
			{ 640, ("fremen", "Fremen") },
			{ 641, ("nsfremen", "Fremen") },
			{ 642, ("engineer", "Fremen") },
			{ 643, ("harvester", "Fremen") },
			{ 644, ("mcv", "Fremen") },
			{ 645, ("trike", "Fremen") },
			{ 646, ("quad", "Fremen") },
			{ 647, ("combat_tank_a", "Fremen") },
			{ 648, ("missile_tank", "Fremen") },
			{ 649, ("siege_tank", "Fremen") },
			{ 650, ("carryall", "Fremen") },
			{ 652, ("sonic_tank", "Fremen") },

			// Smugglers:
			{ 660, ("wall", "Smugglers") },
			{ 661, ("wind_trap", "Smugglers") },
			{ 662, ("construction_yard", "Smugglers") },
			{ 663, ("barracks", "Smugglers") },
			{ 664, ("refinery", "Smugglers") },
			{ 666, ("outpost", "Smugglers") },
			{ 667, ("light_factory", "Smugglers") },
			{ 668, ("silo", "Smugglers") },
			{ 669, ("heavy_factory", "Smugglers") },
			{ 670, ("repair_pad", "Smugglers") },
			{ 671, ("medium_gun_turret", "Smugglers") },
			{ 672, ("high_tech_factory", "Smugglers") },
			{ 673, ("large_gun_turret", "Smugglers") },
			{ 674, ("research_centre", "Smugglers") },
			{ 675, ("starport", "Smugglers") },
			{ 676, ("palace", "Smugglers") },
			{ 677, ("light_inf", "Smugglers") },
			{ 678, ("trooper", "Smugglers") },
			{ 679, ("saboteur", "Smugglers") },
			{ 680, ("engineer", "Smugglers") },
			{ 681, ("harvester", "Smugglers") },
			{ 682, ("mcv", "Smugglers") },
			{ 683, ("trike", "Smugglers") },
			{ 684, ("quad", "Smugglers") },
			{ 685, ("combat_tank_o", "Smugglers") },
			{ 686, ("missile_tank", "Smugglers") },
			{ 687, ("siege_tank", "Smugglers") },
			{ 688, ("carryall", "Smugglers") },

			// Mercenaries:
			{ 700, ("wall", "Mercenaries") },
			{ 701, ("wind_trap", "Mercenaries") },
			{ 702, ("construction_yard", "Mercenaries") },
			{ 703, ("barracks", "Mercenaries") },
			{ 704, ("refinery", "Mercenaries") },
			{ 705, ("outpost", "Mercenaries") },
			{ 707, ("light_factory", "Mercenaries") },
			{ 708, ("silo", "Mercenaries") },
			{ 709, ("heavy_factory", "Mercenaries") },
			{ 710, ("repair_pad", "Mercenaries") },
			{ 711, ("medium_gun_turret", "Mercenaries") },
			{ 712, ("high_tech_factory", "Mercenaries") },
			{ 713, ("large_gun_turret", "Mercenaries") },
			{ 714, ("research_centre", "Mercenaries") },
			{ 715, ("starport", "Mercenaries") },
			{ 716, ("palace", "Mercenaries") },
			{ 717, ("light_inf", "Mercenaries") },
			{ 718, ("trooper", "Mercenaries") },
			{ 719, ("saboteur", "Mercenaries") },
			{ 720, ("harvester", "Mercenaries") },
			{ 721, ("harvester", "Mercenaries") },
			{ 722, ("mcv", "Mercenaries") },
			{ 723, ("trike", "Mercenaries") },
			{ 724, ("quad", "Mercenaries") },
			{ 725, ("combat_tank_o", "Mercenaries") },
			{ 726, ("missile_tank", "Mercenaries") },
			{ 727, ("siege_tank", "Mercenaries") },
			{ 728, ("carryall", "Mercenaries") },
		};

		readonly Ruleset rules;
		readonly FileStream stream;
		readonly string tilesetName;
		readonly TerrainTile clearTile;

		Map map;
		Size mapSize;
		DefaultTerrain terrainInfo;
		List<TerrainTemplateInfo> tileSetsFromYaml;
		int playerCount;

		D2kMapImporter(string filename, string tileset, Ruleset rules)
		{
			tilesetName = tileset;
			this.rules = rules;

			try
			{
				clearTile = new TerrainTile(0, 0);
				stream = File.OpenRead(filename);

				if (stream.Length == 0 || stream.Length % 4 != 0)
					throw new ArgumentException("The map is in an unrecognized format!", nameof(filename));

				Initialize(filename);
				FillMap();
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				map = null;
			}
			finally
			{
				stream.Close();
			}
		}

		public static Map Import(string filename, string mod, string tileset, Ruleset rules)
		{
			var importer = new D2kMapImporter(filename, tileset, rules);
			var map = importer.map;
			if (map == null)
				return null;

			map.RequiresMod = mod;

			return map;
		}

		void Initialize(string mapFile)
		{
			mapSize = new Size(stream.ReadUInt16(), stream.ReadUInt16());
			terrainInfo = Game.ModData.DefaultTerrainInfo["ARRAKIS"] as DefaultTerrain;

			if (terrainInfo == null)
				throw new InvalidDataException("The D2k map importer requires the DefaultTerrain parser.");

			map = new Map(Game.ModData, terrainInfo, mapSize.Width + 2 * MapCordonWidth, mapSize.Height + 2 * MapCordonWidth)
			{
				Title = Path.GetFileNameWithoutExtension(mapFile),
				Author = "Westwood Studios"
			};

			var tl = new PPos(MapCordonWidth, MapCordonWidth);
			var br = new PPos(MapCordonWidth + mapSize.Width - 1, MapCordonWidth + mapSize.Height - 1);
			map.SetBounds(tl, br);

			// Get all templates from the tileset YAML file that have at least one frame and an Image property corresponding to the requested tileset
			// Each frame is a tile from the Dune 2000 tileset files, with the Frame ID being the index of the tile in the original file
			tileSetsFromYaml = terrainInfo.Templates.Where(t =>
			{
				var templateInfo = (DefaultTerrainTemplateInfo)t.Value;
				return templateInfo.Frames != null && string.Equals(templateInfo.Images[0], tilesetName, StringComparison.InvariantCultureIgnoreCase);
			}).Select(ts => ts.Value).ToList();

			var players = new MapPlayers(map.Rules, playerCount);
			map.PlayerDefinitions = players.ToMiniYaml();
		}

		void FillMap()
		{
			while (stream.Position < stream.Length)
			{
				var tileInfo = stream.ReadUInt16();
				var tileSpecialInfo = stream.ReadUInt16();
				var tile = GetTile(tileInfo);

				var locationOnMap = GetCurrentTilePositionOnMap();

				map.Tiles[locationOnMap] = tile;

				// Spice
				if (tileSpecialInfo == 1)
					map.Resources[locationOnMap] = new ResourceTile(1, 1);
				if (tileSpecialInfo == 2)
					map.Resources[locationOnMap] = new ResourceTile(1, 2);

				// Actors
				if (ActorDataByActorCode.ContainsKey(tileSpecialInfo))
				{
					var kvp = ActorDataByActorCode[tileSpecialInfo];
					if (!rules.Actors.ContainsKey(kvp.Actor.ToLowerInvariant()))
						Console.WriteLine($"Ignoring unknown actor type: `{kvp.Actor.ToLowerInvariant()}`");
					else
					{
						var a = new ActorReference(kvp.Actor)
						{
							new LocationInit(locationOnMap),
							new OwnerInit(kvp.Owner)
						};

						map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, a.Save()));

						if (map.PlayerDefinitions.All(x => x.Value.Nodes.Single(y => y.Key == "Name").Value.Value != kvp.Owner))
						{
							var playerInfo = PlayerReferenceDataByPlayerName[kvp.Owner];
							var playerReference = new PlayerReference
							{
								Name = kvp.Owner,
								OwnsWorld = kvp.Owner == "Neutral",
								NonCombatant = kvp.Owner == "Neutral",
								Faction = playerInfo.Faction,
								Color = playerInfo.Color
							};

							var node = new MiniYamlNode($"{nameof(PlayerReference)}@{kvp.Owner}", FieldSaver.SaveDifferences(playerReference, new PlayerReference()));
							map.PlayerDefinitions.Add(node);
						}

						if (kvp.Actor == "mpspawn")
							playerCount++;
					}
				}
			}
		}

		CPos GetCurrentTilePositionOnMap()
		{
			var tileIndex = (int)stream.Position / 4 - 2;

			var x = (tileIndex % mapSize.Width) + MapCordonWidth;
			var y = (tileIndex / mapSize.Width) + MapCordonWidth;

			return new CPos(x, y);
		}

		TerrainTile GetTile(int tileIndex)
		{
			// Some tiles are duplicates of other tiles, just on a different tileset
			if (string.Equals(tilesetName, "bloxbgbs.r8", StringComparison.InvariantCultureIgnoreCase))
			{
				if (tileIndex == 355)
					return new TerrainTile(441, 0);

				if (tileIndex == 375)
					return new TerrainTile(442, 0);
			}

			if (string.Equals(tilesetName, "bloxtree.r8", StringComparison.InvariantCultureIgnoreCase))
			{
				var indices = new[] { 683, 684, 685, 706, 703, 704, 705, 726, 723, 724, 725, 746, 743, 744, 745, 747 };
				for (var i = 0; i < 16; i++)
					if (tileIndex == indices[i])
						return new TerrainTile(474, (byte)i);

				indices = new[] { 369, 370, 389, 390 };
				for (var i = 0; i < 4; i++)
					if (tileIndex == indices[i])
						return new TerrainTile(117, (byte)i);

				indices = new[] { 661, 662, 681, 682 };
				for (var i = 0; i < 4; i++)
					if (tileIndex == indices[i])
						return new TerrainTile(251, (byte)i);

				if (tileIndex == 322)
					return new TerrainTile(215, 0);
			}

			if (string.Equals(tilesetName, "bloxwast.r8", StringComparison.InvariantCultureIgnoreCase))
			{
				if (tileIndex == 342)
					return new TerrainTile(250, 0);

				if (tileIndex == 383)
					return new TerrainTile(121, 1);

				if (tileIndex == 384)
					return new TerrainTile(1046, 0);

				if (tileIndex == 579)
					return new TerrainTile(80, 0);

				if (tileIndex == 597)
					return new TerrainTile(80, 0);

				if (tileIndex == 598)
					return new TerrainTile(470, 0);

				if (tileIndex == 599)
					return new TerrainTile(470, 1);

				if (tileIndex == 608)
					return new TerrainTile(58, 0);

				if (tileIndex == 627)
					return new TerrainTile(248, 0);

				if (tileIndex == 628)
					return new TerrainTile(248, 1);

				if (tileIndex == 719)
					return new TerrainTile(275, 0);

				var indices = new[] { 340, 341, 360, 361 };
				for (var i = 0; i < 4; i++)
					if (tileIndex == indices[i])
						return new TerrainTile(308, (byte)i);

				indices = new[] { 660, 661, 662, 680, 681, 682 };
				for (var i = 0; i < 6; i++)
					if (tileIndex == indices[i])
						return new TerrainTile(443, (byte)i);

				indices = new[] { 609, 610, 629, 630 };
				for (var i = 0; i < 4; i++)
					if (tileIndex == indices[i])
						return new TerrainTile(251, (byte)i);
			}

			// Get the first tileset template that contains the Frame ID of the original map's tile with the requested index
			var template = tileSetsFromYaml.FirstOrDefault(x => ((DefaultTerrainTemplateInfo)x).Frames.Contains(tileIndex));

			// HACK: The arrakis.yaml tileset file seems to be missing some tiles, so just get a replacement for them
			// Also used for duplicate tiles that are taken from only tileset
			if (template == null)
			{
				// Just get a template that contains a tile with the same ID as requested
				var templates = terrainInfo.Templates.Where(t =>
				{
					var templateInfo = (DefaultTerrainTemplateInfo)t.Value;
					return templateInfo.Frames != null && templateInfo.Frames.Contains(tileIndex);
				});
				if (templates.Any())
					template = templates.First().Value;
			}

			if (template == null)
			{
				var pos = GetCurrentTilePositionOnMap();
				Console.WriteLine($"Tile with index {tileIndex} could not be found in the tileset YAML file!");
				Console.WriteLine($"Defaulting to a \"clear\" tile for coordinates ({pos.X}, {pos.Y})!");
				return clearTile;
			}

			var templateIndex = template.Id;
			var frameIndex = Array.IndexOf(((DefaultTerrainTemplateInfo)template).Frames, tileIndex);

			return new TerrainTile(templateIndex, (byte)((frameIndex == -1) ? 0 : frameIndex));
		}
	}
}
