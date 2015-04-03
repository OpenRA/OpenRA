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
using System.IO;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2k.UtilityCommands
{
	class D2kMapImporter
	{
		const int MapCordonWidth = 2;

		readonly Dictionary<int, Pair<string, string>> actorDataByActorCode = new Dictionary<int, Pair<string, string>>
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
			{ 5, Pair.New("power", "Atreides") },
			{ 8, Pair.New("conyard", "Atreides") },
			{ 11, Pair.New("barracks", "Atreides") },
			{ 14, Pair.New("refinery", "Atreides") },
			{ 17, Pair.New("radar", "Atreides") },
			{ 63, Pair.New("light", "Atreides") },
			{ 69, Pair.New("silo", "Atreides") },
			{ 72, Pair.New("heavy", "Atreides") },
			{ 75, Pair.New("repair", "Atreides") },
			{ 78, Pair.New("guntower", "Atreides") },
			{ 120, Pair.New("hightech", "Atreides") },
			{ 123, Pair.New("rockettower", "Atreides") },
			{ 126, Pair.New("research", "Atreides") },
			{ 129, Pair.New("starport", "Atreides") },
			{ 132, Pair.New("palace", "Atreides") },
			{ 180, Pair.New("rifle", "Atreides") },
			{ 181, Pair.New("bazooka", "Atreides") },
			{ 182, Pair.New("fremen", "Atreides") },
			{ 183, Pair.New("sardaukar", "Atreides") },
			{ 184, Pair.New("engineer", "Atreides") },
			{ 185, Pair.New("harvester", "Atreides") },
			{ 186, Pair.New("mcv", "Atreides") },
			{ 187, Pair.New("trike", "Atreides") },
			{ 188, Pair.New("quad", "Atreides") },
			{ 189, Pair.New("combata", "Atreides") },
			{ 190, Pair.New("missiletank", "Atreides") },
			{ 191, Pair.New("siegetank", "Atreides") },
			{ 192, Pair.New("carryall", "Atreides") },
			{ 194, Pair.New("sonictank", "Atreides") },

			// Harkonnen:
			{ 204, Pair.New("wall", "Harkonnen") },
			{ 205, Pair.New("power", "Harkonnen") },
			{ 208, Pair.New("conyard", "Harkonnen") },
			{ 211, Pair.New("barracks", "Harkonnen") },
			{ 214, Pair.New("refinery", "Harkonnen") },
			{ 217, Pair.New("radar", "Harkonnen") },
			{ 263, Pair.New("light", "Harkonnen") },
			{ 269, Pair.New("silo", "Harkonnen") },
			{ 272, Pair.New("heavy", "Harkonnen") },
			{ 275, Pair.New("repair", "Harkonnen") },
			{ 278, Pair.New("guntower", "Harkonnen") },
			{ 320, Pair.New("hightech", "Harkonnen") },
			{ 323, Pair.New("rockettower", "Harkonnen") },
			{ 326, Pair.New("research", "Harkonnen") },
			{ 329, Pair.New("starport", "Harkonnen") },
			{ 332, Pair.New("palace", "Harkonnen") },
			{ 360, Pair.New("rifle", "Harkonnen") },
			{ 361, Pair.New("bazooka", "Harkonnen") },
			{ 362, Pair.New("fremen", "Harkonnen") },
			{ 363, Pair.New("sardaukar", "Harkonnen") },
			{ 364, Pair.New("engineer", "Harkonnen") },
			{ 365, Pair.New("harvester", "Harkonnen") },
			{ 366, Pair.New("mcv", "Harkonnen") },
			{ 367, Pair.New("trike", "Harkonnen") },
			{ 368, Pair.New("quad", "Harkonnen") },
			{ 369, Pair.New("combath", "Harkonnen") },
			{ 370, Pair.New("missiletank", "Harkonnen") },
			{ 371, Pair.New("siegetank", "Harkonnen") },
			{ 372, Pair.New("carryall", "Harkonnen") },
			{ 374, Pair.New("devast", "Harkonnen") },

			// Ordos:
			{ 404, Pair.New("wall", "Ordos") },
			{ 405, Pair.New("power", "Ordos") },
			{ 408, Pair.New("conyard", "Ordos") },
			{ 411, Pair.New("barracks", "Ordos") },
			{ 414, Pair.New("refinery", "Ordos") },
			{ 417, Pair.New("radar", "Ordos") },
			{ 463, Pair.New("light", "Ordos") },
			{ 469, Pair.New("silo", "Ordos") },
			{ 472, Pair.New("heavy", "Ordos") },
			{ 475, Pair.New("repair", "Ordos") },
			{ 478, Pair.New("guntower", "Ordos") },
			{ 520, Pair.New("hightech", "Ordos") },
			{ 523, Pair.New("rockettower", "Ordos") },
			{ 526, Pair.New("research", "Ordos") },
			{ 529, Pair.New("starport", "Ordos") },
			{ 532, Pair.New("palace", "Ordos") },
			{ 560, Pair.New("rifle", "Ordos") },
			{ 561, Pair.New("bazooka", "Ordos") },
			{ 562, Pair.New("saboteur", "Ordos") },
			{ 563, Pair.New("sardaukar", "Ordos") },
			{ 564, Pair.New("engineer", "Ordos") },
			{ 565, Pair.New("harvester", "Ordos") },
			{ 566, Pair.New("mcv", "Ordos") },
			{ 567, Pair.New("raider", "Ordos") },
			{ 568, Pair.New("quad", "Ordos") },
			{ 569, Pair.New("combato", "Ordos") },
			{ 570, Pair.New("missiletank", "Ordos") },
			{ 571, Pair.New("siegetank", "Ordos") },
			{ 572, Pair.New("carryall", "Ordos") },
			{ 574, Pair.New("deviatortank", "Ordos") },

			// Corrino:
			{ 580, Pair.New("wall", "Corrino") },
			{ 581, Pair.New("power", "Corrino") },
			{ 582, Pair.New("conyard", "Corrino") },
			{ 583, Pair.New("barracks", "Corrino") },
			{ 584, Pair.New("refinery", "Corrino") },
			{ 585, Pair.New("radar", "Corrino") },
			{ 587, Pair.New("light", "Corrino") },
			{ 588, Pair.New("palace", "Corrino") },
			{ 589, Pair.New("silo", "Corrino") },
			{ 590, Pair.New("heavy", "Corrino") },
			{ 591, Pair.New("repair", "Corrino") },
			{ 592, Pair.New("guntower", "Corrino") },
			{ 593, Pair.New("hightech", "Corrino") },
			{ 594, Pair.New("rockettower", "Corrino") },
			{ 595, Pair.New("research", "Corrino") },
			{ 596, Pair.New("starport", "Corrino") },
			{ 597, Pair.New("sietch", "Corrino") },
			{ 598, Pair.New("rifle", "Corrino") },
			{ 599, Pair.New("bazooka", "Corrino") },
			{ 600, Pair.New("sardaukar", "Corrino") },
			{ 601, Pair.New("fremen", "Corrino") },
			{ 602, Pair.New("engineer", "Corrino") },
			{ 603, Pair.New("harvester", "Corrino") },
			{ 604, Pair.New("mcv", "Corrino") },
			{ 605, Pair.New("trike", "Corrino") },
			{ 606, Pair.New("quad", "Corrino") },
			{ 607, Pair.New("combath", "Corrino") },
			{ 608, Pair.New("missiletank", "Corrino") },
			{ 609, Pair.New("siegetank", "Corrino") },
			{ 610, Pair.New("carryall", "Corrino") },

			// Fremen:
			{ 620, Pair.New("wall", "Fremen") },
			{ 621, Pair.New("power", "Fremen") },
			{ 622, Pair.New("conyard", "Fremen") },
			{ 623, Pair.New("barracks", "Fremen") },
			{ 624, Pair.New("refinery", "Fremen") },
			{ 625, Pair.New("radar", "Fremen") },
			{ 627, Pair.New("light", "Fremen") },
			{ 628, Pair.New("palacec", "Fremen") },
			{ 629, Pair.New("silo", "Fremen") },
			{ 630, Pair.New("heavy", "Fremen") },
			{ 631, Pair.New("repair", "Fremen") },
			{ 632, Pair.New("guntower", "Fremen") },
			{ 633, Pair.New("hightech", "Fremen") },
			{ 634, Pair.New("rockettower", "Fremen") },
			{ 635, Pair.New("research", "Fremen") },
			{ 636, Pair.New("starport", "Fremen") },
			{ 637, Pair.New("sietch", "Fremen") },
			{ 638, Pair.New("rifle", "Fremen") },
			{ 639, Pair.New("bazooka", "Fremen") },
			{ 640, Pair.New("fremen", "Fremen") },
			////{ 641, Pair.new("", "Fremen") },// Fremen fremen non-stealth
			{ 642, Pair.New("engineer", "Fremen") },
			{ 643, Pair.New("harvester", "Fremen") },
			{ 644, Pair.New("mcv", "Fremen") },
			{ 645, Pair.New("trike", "Fremen") },
			{ 646, Pair.New("quad", "Fremen") },
			{ 647, Pair.New("combata", "Fremen") },
			{ 648, Pair.New("missiletank", "Fremen") },
			{ 649, Pair.New("siegetank", "Fremen") },
			{ 650, Pair.New("carryall", "Fremen") },
			{ 652, Pair.New("sonictank", "Fremen") },

			// Smugglers:
			{ 660, Pair.New("wall", "Smugglers") },
			{ 661, Pair.New("power", "Smugglers") },
			{ 662, Pair.New("conyard", "Smugglers") },
			{ 663, Pair.New("barracks", "Smugglers") },
			{ 664, Pair.New("refinery", "Smugglers") },
			{ 666, Pair.New("radar", "Smugglers") },
			{ 667, Pair.New("light", "Smugglers") },
			{ 668, Pair.New("silo", "Smugglers") },
			{ 669, Pair.New("heavy", "Smugglers") },
			{ 670, Pair.New("repair", "Smugglers") },
			{ 671, Pair.New("guntower", "Smugglers") },
			{ 672, Pair.New("hightech", "Smugglers") },
			{ 673, Pair.New("rockettower", "Smugglers") },
			{ 674, Pair.New("research", "Smugglers") },
			{ 675, Pair.New("starport", "Smugglers") },
			{ 676, Pair.New("palace", "Smugglers") },
			{ 677, Pair.New("rifle", "Smugglers") },
			{ 678, Pair.New("bazooka", "Smugglers") },
			{ 679, Pair.New("saboteur", "Smugglers") },
			{ 680, Pair.New("engineer", "Smugglers") },
			{ 681, Pair.New("harvester", "Smugglers") },
			{ 682, Pair.New("mcv", "Smugglers") },
			{ 683, Pair.New("trike", "Smugglers") },
			{ 684, Pair.New("quad", "Smugglers") },
			{ 685, Pair.New("combato", "Smugglers") },
			{ 686, Pair.New("missiletank", "Smugglers") },
			{ 687, Pair.New("siegetank", "Smugglers") },
			{ 688, Pair.New("carryall", "Smugglers") },

			// Mercenaries:
			{ 700, Pair.New("wall", "Mercenaries") },
			{ 701, Pair.New("power", "Mercenaries") },
			{ 702, Pair.New("conyard", "Mercenaries") },
			{ 703, Pair.New("barracks", "Mercenaries") },
			{ 704, Pair.New("refinery", "Mercenaries") },
			{ 705, Pair.New("radar", "Mercenaries") },
			{ 707, Pair.New("light", "Mercenaries") },
			{ 708, Pair.New("silo", "Mercenaries") },
			{ 709, Pair.New("heavy", "Mercenaries") },
			{ 710, Pair.New("repair", "Mercenaries") },
			{ 711, Pair.New("guntower", "Mercenaries") },
			{ 712, Pair.New("hightech", "Mercenaries") },
			{ 713, Pair.New("rockettower", "Mercenaries") },
			{ 714, Pair.New("research", "Mercenaries") },
			{ 715, Pair.New("starport", "Mercenaries") },
			{ 716, Pair.New("palace", "Mercenaries") },
			{ 717, Pair.New("rifle", "Mercenaries") },
			{ 718, Pair.New("bazooka", "Mercenaries") },
			{ 719, Pair.New("saboteur", "Mercenaries") },
			{ 720, Pair.New("harvester", "Mercenaries") },
			{ 721, Pair.New("harvester", "Mercenaries") },
			{ 722, Pair.New("mcv", "Mercenaries") },
			{ 723, Pair.New("trike", "Mercenaries") },
			{ 724, Pair.New("quad", "Mercenaries") },
			{ 725, Pair.New("combato", "Mercenaries") },
			{ 726, Pair.New("missiletank", "Mercenaries") },
			{ 727, Pair.New("siegetank", "Mercenaries") },
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
			var map = new D2kMapImporter(filename, tileset, rules).map;
			if (map == null)
				return null;

			map.RequiresMod = mod;
			var players = new MapPlayers(map.Rules, map.SpawnPoints.Value.Length);
			map.PlayerDefinitions = players.ToMiniYaml();

			return map;
		}

		void Initialize(string mapFile)
		{
			mapSize = new Size(stream.ReadUInt16(), stream.ReadUInt16());

			tileSet = rules.TileSets["ARRAKIS"];
			map = Map.FromTileset(tileSet);
			map.Title = Path.GetFileNameWithoutExtension(mapFile);
			map.Author = "Westwood Studios";
			map.MapSize = new int2(mapSize.Width + 2 * MapCordonWidth, mapSize.Height + 2 * MapCordonWidth);
			map.Bounds = new Rectangle(MapCordonWidth, MapCordonWidth, mapSize.Width, mapSize.Height);

			map.MapResources = Exts.Lazy(() => new CellLayer<ResourceTile>(TileShape.Rectangle, new Size(map.MapSize.X, map.MapSize.Y)));
			map.MapTiles = Exts.Lazy(() => new CellLayer<TerrainTile>(TileShape.Rectangle, new Size(map.MapSize.X, map.MapSize.Y)));

			map.Options = new MapOptions();

			// Get all templates from the tileset YAML file that have at least one frame and an Image property corresponding to the requested tileset
			// Each frame is a tile from the Dune 2000 tileset files, with the Frame ID being the index of the tile in the original file
			tileSetsFromYaml = tileSet.Templates.Where(t => t.Value.Frames != null
				&& t.Value.Images[0].ToLower() == tilesetName.ToLower()).Select(ts => ts.Value).ToList();
		}

		void FillMap()
		{
			while (stream.Position < stream.Length)
			{
				var tileInfo = stream.ReadUInt16();
				var tileSpecialInfo = stream.ReadUInt16();
				var tile = GetTile(tileInfo);

				var locationOnMap = GetCurrentTilePositionOnMap();

				map.MapTiles.Value[locationOnMap] = tile;

				// Spice
				if (tileSpecialInfo == 1)
					map.MapResources.Value[locationOnMap] = new ResourceTile(1, 1);
				if (tileSpecialInfo == 2)
					map.MapResources.Value[locationOnMap] = new ResourceTile(1, 2);

				// Actors
				if (actorDataByActorCode.ContainsKey(tileSpecialInfo))
				{
					var kvp = actorDataByActorCode[tileSpecialInfo];
					if (!rules.Actors.ContainsKey(kvp.First.ToLower()))
						throw new InvalidOperationException("Actor with name {0} could not be found in the rules YAML file!".F(kvp.First));

					var a = new ActorReference(kvp.First)
					{
						new LocationInit(locationOnMap),
						new OwnerInit(kvp.Second)
					};
					map.ActorDefinitions.Add(new MiniYamlNode("Actor" + map.ActorDefinitions.Count, a.Save()));
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
