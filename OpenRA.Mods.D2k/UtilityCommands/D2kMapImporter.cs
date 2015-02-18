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
			{ 4, Pair.New("WALLA", "Atreides") },
			{ 5, Pair.New("PWRA", "Atreides") },
			{ 8, Pair.New("CONYARDA", "Atreides") },
			{ 11, Pair.New("BARRA", "Atreides") },
			{ 14, Pair.New("REFA", "Atreides") },
			{ 17, Pair.New("RADARA", "Atreides") },
			{ 63, Pair.New("LIGHTA", "Atreides") },
			{ 69, Pair.New("SILOA", "Atreides") },
			{ 72, Pair.New("HEAVYA", "Atreides") },
			{ 75, Pair.New("REPAIRA", "Atreides") },
			{ 78, Pair.New("GUNTOWERA", "Atreides") },
			{ 120, Pair.New("HIGHTECHA", "Atreides") },
			{ 123, Pair.New("ROCKETTOWERA", "Atreides") },
			{ 126, Pair.New("RESEARCHA", "Atreides") },
			{ 129, Pair.New("STARPORTA", "Atreides") },
			{ 132, Pair.New("PALACEA", "Atreides") },
			{ 180, Pair.New("RIFLE", "Atreides") },
			{ 181, Pair.New("BAZOOKA", "Atreides") },
			{ 182, Pair.New("FREMEN", "Atreides") },
			{ 183, Pair.New("SARDAUKAR", "Atreides") },
			{ 184, Pair.New("ENGINEER", "Atreides") },
			{ 185, Pair.New("HARVESTER", "Atreides") },
			{ 186, Pair.New("MCVA", "Atreides") },
			{ 187, Pair.New("TRIKE", "Atreides") },
			{ 188, Pair.New("QUAD", "Atreides") },
			{ 189, Pair.New("COMBATA", "Atreides") },
			{ 190, Pair.New("MISSILETANK", "Atreides") },
			{ 191, Pair.New("SIEGETANK", "Atreides") },
			{ 192, Pair.New("CARRYALLA", "Atreides") },
			{ 194, Pair.New("SONICTANK", "Atreides") },

			// Harkonnen:
			{ 204, Pair.New("WALLH", "Harkonnen") },
			{ 205, Pair.New("PWRH", "Harkonnen") },
			{ 208, Pair.New("CONYARDH", "Harkonnen") },
			{ 211, Pair.New("BARRH", "Harkonnen") },
			{ 214, Pair.New("REFH", "Harkonnen") },
			{ 217, Pair.New("RADARH", "Harkonnen") },
			{ 263, Pair.New("LIGHTH", "Harkonnen") },
			{ 269, Pair.New("SILOH", "Harkonnen") },
			{ 272, Pair.New("HEAVYH", "Harkonnen") },
			{ 275, Pair.New("REPAIRH", "Harkonnen") },
			{ 278, Pair.New("GUNTOWERH", "Harkonnen") },
			{ 320, Pair.New("HIGHTECHH", "Harkonnen") },
			{ 323, Pair.New("ROCKETTOWERH", "Harkonnen") },
			{ 326, Pair.New("RESEARCHH", "Harkonnen") },
			{ 329, Pair.New("STARPORTH", "Harkonnen") },
			{ 332, Pair.New("PALACEH", "Harkonnen") },
			{ 360, Pair.New("RIFLE", "Harkonnen") },
			{ 361, Pair.New("BAZOOKA", "Harkonnen") },
			{ 362, Pair.New("FREMEN", "Harkonnen") },
			{ 363, Pair.New("SARDAUKAR", "Harkonnen") },
			{ 364, Pair.New("ENGINEER", "Harkonnen") },
			{ 365, Pair.New("HARVESTER", "Harkonnen") },
			{ 366, Pair.New("MCVH", "Harkonnen") },
			{ 367, Pair.New("TRIKE", "Harkonnen") },
			{ 368, Pair.New("QUAD", "Harkonnen") },
			{ 369, Pair.New("COMBATH", "Harkonnen") },
			{ 370, Pair.New("MISSILETANK", "Harkonnen") },
			{ 371, Pair.New("SIEGETANK", "Harkonnen") },
			{ 372, Pair.New("CARRYALLH", "Harkonnen") },
			{ 374, Pair.New("DEVAST", "Harkonnen") },

			// Ordos:
			{ 404, Pair.New("WALLO", "Ordos") },
			{ 405, Pair.New("PWRO", "Ordos") },
			{ 408, Pair.New("CONYARDO", "Ordos") },
			{ 411, Pair.New("BARRO", "Ordos") },
			{ 414, Pair.New("REFO", "Ordos") },
			{ 417, Pair.New("RADARO", "Ordos") },
			{ 463, Pair.New("LIGHTO", "Ordos") },
			{ 469, Pair.New("SILOO", "Ordos") },
			{ 472, Pair.New("HEAVYO", "Ordos") },
			{ 475, Pair.New("REPAIRO", "Ordos") },
			{ 478, Pair.New("GUNTOWERO", "Ordos") },
			{ 520, Pair.New("HIGHTECHO", "Ordos") },
			{ 523, Pair.New("ROCKETTOWERO", "Ordos") },
			{ 526, Pair.New("RESEARCHO", "Ordos") },
			{ 529, Pair.New("STARPORTO", "Ordos") },
			{ 532, Pair.New("PALACEO", "Ordos") },
			{ 560, Pair.New("RIFLE", "Ordos") },
			{ 561, Pair.New("BAZOOKA", "Ordos") },
			{ 562, Pair.New("SABOTEUR", "Ordos") },
			{ 563, Pair.New("SARDAUKAR", "Ordos") },
			{ 564, Pair.New("ENGINEER", "Ordos") },
			{ 565, Pair.New("HARVESTER", "Ordos") },
			{ 566, Pair.New("MCVO", "Ordos") },
			{ 567, Pair.New("RAIDER", "Ordos") },
			{ 568, Pair.New("QUAD", "Ordos") },
			{ 569, Pair.New("COMBATO", "Ordos") },
			{ 570, Pair.New("MISSILETANK", "Ordos") },
			{ 571, Pair.New("SIEGETANK", "Ordos") },
			{ 572, Pair.New("CARRYALLO", "Ordos") },
			{ 574, Pair.New("DEVIATORTANK", "Ordos") },

			// Corrino:
			{ 580, Pair.New("WALLH", "Corrino") },
			{ 581, Pair.New("PWRH", "Corrino") },
			{ 582, Pair.New("CONYARDC", "Corrino") },
			{ 583, Pair.New("BARRH", "Corrino") },
			{ 584, Pair.New("REFH", "Corrino") },
			{ 585, Pair.New("RADARH", "Corrino") },
			{ 587, Pair.New("LIGHTH", "Corrino") },
			{ 588, Pair.New("PALACEC", "Corrino") },
			{ 589, Pair.New("SILOH", "Corrino") },
			{ 590, Pair.New("HEAVYC", "Corrino") },
			{ 591, Pair.New("REPAIRH", "Corrino") },
			{ 592, Pair.New("GUNTOWERH", "Corrino") },
			{ 593, Pair.New("HIGHTECHH", "Corrino") },
			{ 594, Pair.New("ROCKETTOWERH", "Corrino") },
			{ 595, Pair.New("RESEARCHH", "Corrino") },
			{ 596, Pair.New("STARPORTC", "Corrino") },
			{ 597, Pair.New("SIETCH", "Corrino") },
			{ 598, Pair.New("RIFLE", "Corrino") },
			{ 599, Pair.New("BAZOOKA", "Corrino") },
			{ 600, Pair.New("SARDAUKAR", "Corrino") },
			{ 601, Pair.New("FREMEN", "Corrino") },
			{ 602, Pair.New("ENGINEER", "Corrino") },
			{ 603, Pair.New("HARVESTER", "Corrino") },
			{ 604, Pair.New("MCVH", "Corrino") },
			{ 605, Pair.New("TRIKE", "Corrino") },
			{ 606, Pair.New("QUAD", "Corrino") },
			{ 607, Pair.New("COMBATH", "Corrino") },
			{ 608, Pair.New("MISSILETANK", "Corrino") },
			{ 609, Pair.New("SIEGETANK", "Corrino") },
			{ 610, Pair.New("CARRYALLH", "Corrino") },

			// Fremen:
			{ 620, Pair.New("WALLA", "Fremen") },
			{ 621, Pair.New("PWRA", "Fremen") },
			{ 622, Pair.New("CONYARDA", "Fremen") },
			{ 623, Pair.New("BARRA", "Fremen") },
			{ 624, Pair.New("REFA", "Fremen") },
			{ 625, Pair.New("RADARA", "Fremen") },
			{ 627, Pair.New("LIGHTA", "Fremen") },
			{ 628, Pair.New("PALACEC", "Fremen") },
			{ 629, Pair.New("SILOA", "Fremen") },
			{ 630, Pair.New("HEAVYA", "Fremen") },
			{ 631, Pair.New("REPAIRA", "Fremen") },
			{ 632, Pair.New("GUNTOWERA", "Fremen") },
			{ 633, Pair.New("HIGHTECHA", "Fremen") },
			{ 634, Pair.New("ROCKETTOWERA", "Fremen") },
			{ 635, Pair.New("RESEARCHA", "Fremen") },
			{ 636, Pair.New("STARPORTA", "Fremen") },
			{ 637, Pair.New("SIETCH", "Fremen") },
			{ 638, Pair.New("RIFLE", "Fremen") },
			{ 639, Pair.New("BAZOOKA", "Fremen") },
			{ 640, Pair.New("FREMEN", "Fremen") },
			////{ 641, Pair.New("", "Fremen") },// Fremen fremen non-stealth
			{ 642, Pair.New("ENGINEER", "Fremen") },
			{ 643, Pair.New("HARVESTER", "Fremen") },
			{ 644, Pair.New("MCVA", "Fremen") },
			{ 645, Pair.New("TRIKE", "Fremen") },
			{ 646, Pair.New("QUAD", "Fremen") },
			{ 647, Pair.New("COMBATA", "Fremen") },
			{ 648, Pair.New("MISSILETANK", "Fremen") },
			{ 649, Pair.New("SIEGETANK", "Fremen") },
			{ 650, Pair.New("CARRYALLA", "Fremen") },
			{ 652, Pair.New("SONICTANK", "Fremen") },

			// Smugglers:
			{ 660, Pair.New("WALLO", "Smugglers") },
			{ 661, Pair.New("PWRO", "Smugglers") },
			{ 662, Pair.New("CONYARDO", "Smugglers") },
			{ 663, Pair.New("BARRO", "Smugglers") },
			{ 664, Pair.New("REFO", "Smugglers") },
			{ 666, Pair.New("RADARO", "Smugglers") },
			{ 667, Pair.New("LIGHTO", "Smugglers") },
			{ 668, Pair.New("SILOO", "Smugglers") },
			{ 669, Pair.New("HEAVYO", "Smugglers") },
			{ 670, Pair.New("REPAIRO", "Smugglers") },
			{ 671, Pair.New("GUNTOWERO", "Smugglers") },
			{ 672, Pair.New("HIGHTECHO", "Smugglers") },
			{ 673, Pair.New("ROCKETTOWERO", "Smugglers") },
			{ 674, Pair.New("RESEARCHO", "Smugglers") },
			{ 675, Pair.New("STARPORTO", "Smugglers") },
			{ 676, Pair.New("PALACEO", "Smugglers") },
			{ 677, Pair.New("RIFLE", "Smugglers") },
			{ 678, Pair.New("BAZOOKA", "Smugglers") },
			{ 679, Pair.New("SABOTEUR", "Smugglers") },
			{ 680, Pair.New("ENGINEER", "Smugglers") },
			{ 681, Pair.New("HARVESTER", "Smugglers") },
			{ 682, Pair.New("MCVO", "Smugglers") },
			{ 683, Pair.New("TRIKE", "Smugglers") },
			{ 684, Pair.New("QUAD", "Smugglers") },
			{ 685, Pair.New("COMBATO", "Smugglers") },
			{ 686, Pair.New("MISSILETANK", "Smugglers") },
			{ 687, Pair.New("SIEGETANK", "Smugglers") },
			{ 688, Pair.New("CARRYALLO", "Smugglers") },

			// Mercenaries:
			{ 700, Pair.New("WALLO", "Mercenaries") },
			{ 701, Pair.New("PWRO", "Mercenaries") },
			{ 702, Pair.New("CONYARDO", "Mercenaries") },
			{ 703, Pair.New("BARRO", "Mercenaries") },
			{ 704, Pair.New("REFO", "Mercenaries") },
			{ 705, Pair.New("RADARO", "Mercenaries") },
			{ 707, Pair.New("LIGHTO", "Mercenaries") },
			{ 708, Pair.New("SILOO", "Mercenaries") },
			{ 709, Pair.New("HEAVYO", "Mercenaries") },
			{ 710, Pair.New("REPAIRO", "Mercenaries") },
			{ 711, Pair.New("GUNTOWERO", "Mercenaries") },
			{ 712, Pair.New("HIGHTECHO", "Mercenaries") },
			{ 713, Pair.New("ROCKETTOWERO", "Mercenaries") },
			{ 714, Pair.New("RESEARCHO", "Mercenaries") },
			{ 715, Pair.New("STARPORTO", "Mercenaries") },
			{ 716, Pair.New("PALACEO", "Mercenaries") },
			{ 717, Pair.New("RIFLE", "Mercenaries") },
			{ 718, Pair.New("BAZOOKA", "Mercenaries") },
			{ 719, Pair.New("SABOTEUR", "Mercenaries") },
			{ 720, Pair.New("HARVESTER", "Mercenaries") },
			{ 721, Pair.New("HARVESTER", "Mercenaries") },
			{ 722, Pair.New("MCVO", "Mercenaries") },
			{ 723, Pair.New("TRIKE", "Mercenaries") },
			{ 724, Pair.New("QUAD", "Mercenaries") },
			{ 725, Pair.New("COMBATO", "Mercenaries") },
			{ 726, Pair.New("MISSILETANK", "Mercenaries") },
			{ 727, Pair.New("SIEGETANK", "Mercenaries") },
			{ 728, Pair.New("CARRYALLO", "Mercenaries") },
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
			map.MakeDefaultPlayers();

			return map;
		}

		void Initialize(string mapFile)
		{
			mapSize = new Size(stream.ReadUInt16(), stream.ReadUInt16());

			tileSet = rules.TileSets["ARRAKIS"];
			map = Map.FromTileset(tileSet);
			map.Title = Path.GetFileNameWithoutExtension(mapFile);
			map.Author = "Westwood Studios";
			map.MapSize.X = mapSize.Width + 2 * MapCordonWidth;
			map.MapSize.Y = mapSize.Height + 2 * MapCordonWidth;
			map.Bounds = new Rectangle(MapCordonWidth, MapCordonWidth, mapSize.Width, mapSize.Height);

			map.Smudges = Exts.Lazy(() => new List<SmudgeReference>());
			map.Actors = Exts.Lazy(() => new Dictionary<string, ActorReference>());
			map.MapResources = Exts.Lazy(() => new CellLayer<ResourceTile>(TileShape.Rectangle, new Size(map.MapSize.X, map.MapSize.Y)));
			map.MapTiles = Exts.Lazy(() => new CellLayer<TerrainTile>(TileShape.Rectangle, new Size(map.MapSize.X, map.MapSize.Y)));

			map.Options = new MapOptions();

			// Get all templates from the tileset YAML file that have at least one frame and an Image property corresponding to the requested tileset
			// Each frame is a tile from the Dune 2000 tileset files, with the Frame ID being the index of the tile in the original file
			tileSetsFromYaml = tileSet.Templates.Where(t => t.Value.Frames != null
				&& t.Value.Image.ToLower() == tilesetName.ToLower()).Select(ts => ts.Value).ToList();
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
					map.Actors.Value.Add("Actor" + map.Actors.Value.Count, a);
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
