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
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2k.UtilityCommands
{
	public class D2kMapImporter
	{
		const int MapCordonWidth = 2;

		public static Dictionary<int, Pair<string, string>> ActorDataByActorCode = new Dictionary<int, Pair<string, string>>
		{
			{ 20, Pair.New("wormspawner", "Creeps") },
			{ 23, Pair.New("mpspawn", "Neutral") },
			{ 41, Pair.New("spicebloom", "Neutral") },
			{ 42, Pair.New("spicebloom", "Neutral") },
			{ 43, Pair.New("spicebloom", "Neutral") },
			{ 44, Pair.New("spicebloom", "Neutral") },
			{ 45, Pair.New("spicebloom", "Neutral") },

			// Atreides:
			{ 4, Pair.New("wall", "Atreides") },
			{ 5, Pair.New("wind_trap", "Atreides") },
			{ 8, Pair.New("construction_yard", "Atreides") },
			{ 11, Pair.New("barracks", "Atreides") },
			{ 14, Pair.New("refinery", "Atreides") },
			{ 17, Pair.New("outpost", "Atreides") },
			{ 63, Pair.New("light_factory", "Atreides") },
			{ 69, Pair.New("silo", "Atreides") },
			{ 72, Pair.New("heavy_factory", "Atreides") },
			{ 75, Pair.New("repair_pad", "Atreides") },
			{ 78, Pair.New("medium_gun_turret", "Atreides") },
			{ 120, Pair.New("high_tech_factory", "Atreides") },
			{ 123, Pair.New("large_gun_turret", "Atreides") },
			{ 126, Pair.New("research_centre", "Atreides") },
			{ 129, Pair.New("starport", "Atreides") },
			{ 132, Pair.New("palace", "Atreides") },
			{ 180, Pair.New("light_inf", "Atreides") },
			{ 181, Pair.New("trooper", "Atreides") },
			{ 182, Pair.New("fremen", "Atreides") },
			{ 183, Pair.New("sardaukar", "Atreides") },
			{ 184, Pair.New("engineer", "Atreides") },
			{ 185, Pair.New("harvester", "Atreides") },
			{ 186, Pair.New("mcv", "Atreides") },
			{ 187, Pair.New("trike", "Atreides") },
			{ 188, Pair.New("quad", "Atreides") },
			{ 189, Pair.New("combat_tank_a", "Atreides") },
			{ 190, Pair.New("missile_tank", "Atreides") },
			{ 191, Pair.New("siege_tank", "Atreides") },
			{ 192, Pair.New("carryall", "Atreides") },
			{ 194, Pair.New("sonic_tank", "Atreides") },

			// Harkonnen:
			{ 204, Pair.New("wall", "Harkonnen") },
			{ 205, Pair.New("wind_trap", "Harkonnen") },
			{ 208, Pair.New("construction_yard", "Harkonnen") },
			{ 211, Pair.New("barracks", "Harkonnen") },
			{ 214, Pair.New("refinery", "Harkonnen") },
			{ 217, Pair.New("outpost", "Harkonnen") },
			{ 263, Pair.New("light_factory", "Harkonnen") },
			{ 269, Pair.New("silo", "Harkonnen") },
			{ 272, Pair.New("heavy_factory", "Harkonnen") },
			{ 275, Pair.New("repair_pad", "Harkonnen") },
			{ 278, Pair.New("medium_gun_turret", "Harkonnen") },
			{ 320, Pair.New("high_tech_factory", "Harkonnen") },
			{ 323, Pair.New("large_gun_turret", "Harkonnen") },
			{ 326, Pair.New("research_centre", "Harkonnen") },
			{ 329, Pair.New("starport", "Harkonnen") },
			{ 332, Pair.New("palace", "Harkonnen") },
			{ 360, Pair.New("light_inf", "Harkonnen") },
			{ 361, Pair.New("trooper", "Harkonnen") },
			{ 362, Pair.New("fremen", "Harkonnen") },
			{ 363, Pair.New("sardaukar", "Harkonnen") },
			{ 364, Pair.New("engineer", "Harkonnen") },
			{ 365, Pair.New("harvester", "Harkonnen") },
			{ 366, Pair.New("mcv", "Harkonnen") },
			{ 367, Pair.New("trike", "Harkonnen") },
			{ 368, Pair.New("quad", "Harkonnen") },
			{ 369, Pair.New("combat_tank_h", "Harkonnen") },
			{ 370, Pair.New("missile_tank", "Harkonnen") },
			{ 371, Pair.New("siege_tank", "Harkonnen") },
			{ 372, Pair.New("carryall", "Harkonnen") },
			{ 374, Pair.New("devastator", "Harkonnen") },

			// Ordos:
			{ 404, Pair.New("wall", "Ordos") },
			{ 405, Pair.New("wind_trap", "Ordos") },
			{ 408, Pair.New("construction_yard", "Ordos") },
			{ 411, Pair.New("barracks", "Ordos") },
			{ 414, Pair.New("refinery", "Ordos") },
			{ 417, Pair.New("outpost", "Ordos") },
			{ 463, Pair.New("light_factory", "Ordos") },
			{ 469, Pair.New("silo", "Ordos") },
			{ 472, Pair.New("heavy_factory", "Ordos") },
			{ 475, Pair.New("repair_pad", "Ordos") },
			{ 478, Pair.New("medium_gun_turret", "Ordos") },
			{ 520, Pair.New("high_tech_factory", "Ordos") },
			{ 523, Pair.New("large_gun_turret", "Ordos") },
			{ 526, Pair.New("research_centre", "Ordos") },
			{ 529, Pair.New("starport", "Ordos") },
			{ 532, Pair.New("palace", "Ordos") },
			{ 560, Pair.New("light_inf", "Ordos") },
			{ 561, Pair.New("trooper", "Ordos") },
			{ 562, Pair.New("saboteur", "Ordos") },
			{ 563, Pair.New("sardaukar", "Ordos") },
			{ 564, Pair.New("engineer", "Ordos") },
			{ 565, Pair.New("harvester", "Ordos") },
			{ 566, Pair.New("mcv", "Ordos") },
			{ 567, Pair.New("raider", "Ordos") },
			{ 568, Pair.New("quad", "Ordos") },
			{ 569, Pair.New("combat_tank_o", "Ordos") },
			{ 570, Pair.New("missile_tank", "Ordos") },
			{ 571, Pair.New("siege_tank", "Ordos") },
			{ 572, Pair.New("carryall", "Ordos") },
			{ 574, Pair.New("deviator", "Ordos") },

			// Corrino:
			{ 580, Pair.New("wall", "Corrino") },
			{ 581, Pair.New("wind_trap", "Corrino") },
			{ 582, Pair.New("construction_yard", "Corrino") },
			{ 583, Pair.New("barracks", "Corrino") },
			{ 584, Pair.New("refinery", "Corrino") },
			{ 585, Pair.New("outpost", "Corrino") },
			{ 587, Pair.New("light_factory", "Corrino") },
			{ 588, Pair.New("palace", "Corrino") },
			{ 589, Pair.New("silo", "Corrino") },
			{ 590, Pair.New("heavy_factory", "Corrino") },
			{ 591, Pair.New("repair_pad", "Corrino") },
			{ 592, Pair.New("medium_gun_turret", "Corrino") },
			{ 593, Pair.New("high_tech_factory", "Corrino") },
			{ 594, Pair.New("large_gun_turret", "Corrino") },
			{ 595, Pair.New("research_centre", "Corrino") },
			{ 596, Pair.New("starport", "Corrino") },
			{ 597, Pair.New("sietch", "Corrino") },
			{ 598, Pair.New("light_inf", "Corrino") },
			{ 599, Pair.New("trooper", "Corrino") },
			{ 600, Pair.New("sardaukar", "Corrino") },
			{ 601, Pair.New("fremen", "Corrino") },
			{ 602, Pair.New("engineer", "Corrino") },
			{ 603, Pair.New("harvester", "Corrino") },
			{ 604, Pair.New("mcv", "Corrino") },
			{ 605, Pair.New("trike", "Corrino") },
			{ 606, Pair.New("quad", "Corrino") },
			{ 607, Pair.New("combat_tank_h", "Corrino") },
			{ 608, Pair.New("missile_tank", "Corrino") },
			{ 609, Pair.New("siege_tank", "Corrino") },
			{ 610, Pair.New("carryall", "Corrino") },

			// Fremen:
			{ 620, Pair.New("wall", "Fremen") },
			{ 621, Pair.New("wind_trap", "Fremen") },
			{ 622, Pair.New("construction_yard", "Fremen") },
			{ 623, Pair.New("barracks", "Fremen") },
			{ 624, Pair.New("refinery", "Fremen") },
			{ 625, Pair.New("outpost", "Fremen") },
			{ 627, Pair.New("light_factory", "Fremen") },
			{ 628, Pair.New("palace", "Fremen") },
			{ 629, Pair.New("silo", "Fremen") },
			{ 630, Pair.New("heavy_factory", "Fremen") },
			{ 631, Pair.New("repair_pad", "Fremen") },
			{ 632, Pair.New("medium_gun_turret", "Fremen") },
			{ 633, Pair.New("high_tech_factory", "Fremen") },
			{ 634, Pair.New("large_gun_turret", "Fremen") },
			{ 635, Pair.New("research_centre", "Fremen") },
			{ 636, Pair.New("starport", "Fremen") },
			{ 637, Pair.New("sietch", "Fremen") },
			{ 638, Pair.New("light_inf", "Fremen") },
			{ 639, Pair.New("trooper", "Fremen") },
			{ 640, Pair.New("fremen", "Fremen") },
			{ 641, Pair.New("nsfremen", "Fremen") },
			{ 642, Pair.New("engineer", "Fremen") },
			{ 643, Pair.New("harvester", "Fremen") },
			{ 644, Pair.New("mcv", "Fremen") },
			{ 645, Pair.New("trike", "Fremen") },
			{ 646, Pair.New("quad", "Fremen") },
			{ 647, Pair.New("combat_tank_a", "Fremen") },
			{ 648, Pair.New("missile_tank", "Fremen") },
			{ 649, Pair.New("siege_tank", "Fremen") },
			{ 650, Pair.New("carryall", "Fremen") },
			{ 652, Pair.New("sonic_tank", "Fremen") },

			// Smugglers:
			{ 660, Pair.New("wall", "Smugglers") },
			{ 661, Pair.New("wind_trap", "Smugglers") },
			{ 662, Pair.New("construction_yard", "Smugglers") },
			{ 663, Pair.New("barracks", "Smugglers") },
			{ 664, Pair.New("refinery", "Smugglers") },
			{ 666, Pair.New("outpost", "Smugglers") },
			{ 667, Pair.New("light_factory", "Smugglers") },
			{ 668, Pair.New("silo", "Smugglers") },
			{ 669, Pair.New("heavy_factory", "Smugglers") },
			{ 670, Pair.New("repair_pad", "Smugglers") },
			{ 671, Pair.New("medium_gun_turret", "Smugglers") },
			{ 672, Pair.New("high_tech_factory", "Smugglers") },
			{ 673, Pair.New("large_gun_turret", "Smugglers") },
			{ 674, Pair.New("research_centre", "Smugglers") },
			{ 675, Pair.New("starport", "Smugglers") },
			{ 676, Pair.New("palace", "Smugglers") },
			{ 677, Pair.New("light_inf", "Smugglers") },
			{ 678, Pair.New("trooper", "Smugglers") },
			{ 679, Pair.New("saboteur", "Smugglers") },
			{ 680, Pair.New("engineer", "Smugglers") },
			{ 681, Pair.New("harvester", "Smugglers") },
			{ 682, Pair.New("mcv", "Smugglers") },
			{ 683, Pair.New("trike", "Smugglers") },
			{ 684, Pair.New("quad", "Smugglers") },
			{ 685, Pair.New("combat_tank_o", "Smugglers") },
			{ 686, Pair.New("missile_tank", "Smugglers") },
			{ 687, Pair.New("siege_tank", "Smugglers") },
			{ 688, Pair.New("carryall", "Smugglers") },

			// Mercenaries:
			{ 700, Pair.New("wall", "Mercenaries") },
			{ 701, Pair.New("wind_trap", "Mercenaries") },
			{ 702, Pair.New("construction_yard", "Mercenaries") },
			{ 703, Pair.New("barracks", "Mercenaries") },
			{ 704, Pair.New("refinery", "Mercenaries") },
			{ 705, Pair.New("outpost", "Mercenaries") },
			{ 707, Pair.New("light_factory", "Mercenaries") },
			{ 708, Pair.New("silo", "Mercenaries") },
			{ 709, Pair.New("heavy_factory", "Mercenaries") },
			{ 710, Pair.New("repair_pad", "Mercenaries") },
			{ 711, Pair.New("medium_gun_turret", "Mercenaries") },
			{ 712, Pair.New("high_tech_factory", "Mercenaries") },
			{ 713, Pair.New("large_gun_turret", "Mercenaries") },
			{ 714, Pair.New("research_centre", "Mercenaries") },
			{ 715, Pair.New("starport", "Mercenaries") },
			{ 716, Pair.New("palace", "Mercenaries") },
			{ 717, Pair.New("light_inf", "Mercenaries") },
			{ 718, Pair.New("trooper", "Mercenaries") },
			{ 719, Pair.New("saboteur", "Mercenaries") },
			{ 720, Pair.New("harvester", "Mercenaries") },
			{ 721, Pair.New("harvester", "Mercenaries") },
			{ 722, Pair.New("mcv", "Mercenaries") },
			{ 723, Pair.New("trike", "Mercenaries") },
			{ 724, Pair.New("quad", "Mercenaries") },
			{ 725, Pair.New("combat_tank_o", "Mercenaries") },
			{ 726, Pair.New("missile_tank", "Mercenaries") },
			{ 727, Pair.New("siege_tank", "Mercenaries") },
			{ 728, Pair.New("carryall", "Mercenaries") },
		};

		readonly Ruleset rules;
		readonly FileStream stream;
		readonly string tilesetName;
		readonly TerrainTile clearTile;

		Map map;
		Size mapSize;
		TileSet tileSet;
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
					throw new Exception("The map is in an unrecognized format!");

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

			tileSet = Game.ModData.DefaultTileSets["ARRAKIS"];

			map = new Map(Game.ModData, tileSet, mapSize.Width + 2 * MapCordonWidth, mapSize.Height + 2 * MapCordonWidth)
			{
				Title = Path.GetFileNameWithoutExtension(mapFile),
				Author = "Westwood Studios"
			};

			var tl = new PPos(MapCordonWidth, MapCordonWidth);
			var br = new PPos(MapCordonWidth + mapSize.Width - 1, MapCordonWidth + mapSize.Height - 1);
			map.SetBounds(tl, br);

			// Get all templates from the tileset YAML file that have at least one frame and an Image property corresponding to the requested tileset
			// Each frame is a tile from the Dune 2000 tileset files, with the Frame ID being the index of the tile in the original file
			tileSetsFromYaml = tileSet.Templates.Where(t => t.Value.Frames != null
				&& t.Value.Images[0].ToLower() == tilesetName.ToLower()).Select(ts => ts.Value).ToList();

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
					if (!rules.Actors.ContainsKey(kvp.First.ToLower()))
						throw new InvalidOperationException("Actor with name {0} could not be found in the rules YAML file!".F(kvp.First));

					var a = new ActorReference(kvp.First)
					{
						new LocationInit(locationOnMap),
						new OwnerInit(kvp.Second)
					};

					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, a.Save()));

					if (kvp.First == "mpspawn")
						playerCount++;
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
			// Get the first tileset template that contains the Frame ID of the original map's tile with the requested index
			var template = tileSetsFromYaml.FirstOrDefault(x => x.Frames.Contains(tileIndex));

			// HACK: The arrakis.yaml tileset file seems to be missing some tiles, so just get a replacement for them
			// Also used for duplicate tiles that are taken from only tileset
			if (template == null)
			{
				// Just get a template that contains a tile with the same ID as requested
				var templates = tileSet.Templates.Where(t => t.Value.Frames != null && t.Value.Frames.Contains(tileIndex));
				if (templates.Any())
					template = templates.First().Value;
			}

			if (template == null)
			{
				var pos = GetCurrentTilePositionOnMap();
				Console.WriteLine("Tile with index {0} could not be found in the tileset YAML file!".F(tileIndex));
				Console.WriteLine("Defaulting to a \"clear\" tile for coordinates ({0}, {1})!".F(pos.X, pos.Y));
				return clearTile;
			}

			var templateIndex = template.Id;
			var frameIndex = Array.IndexOf(template.Frames, tileIndex);

			return new TerrainTile(templateIndex, (byte)((frameIndex == -1) ? 0 : frameIndex));
		}
	}
}
