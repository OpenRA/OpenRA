#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Terrain
{
	public interface ITemplatedTerrainInfo : ITerrainInfo
	{
		string[] EditorTemplateOrder { get; }
		IReadOnlyDictionary<ushort, TerrainTemplateInfo> Templates { get; }
	}

	public interface ITerrainInfoNotifyMapCreated : ITerrainInfo
	{
		void MapCreated(Map map);
	}

	public class TerrainTemplateInfo
	{
		public readonly ushort Id;
		public readonly int2 Size;
		public readonly bool PickAny;
		public readonly string[] Categories;

		readonly TerrainTileInfo[] tileInfo;

		public TerrainTemplateInfo(ITerrainInfo terrainInfo, MiniYaml my)
		{
			FieldLoader.Load(this, my);

			var nodes = my.ToDictionary()["Tiles"].Nodes;

			if (!PickAny)
			{
				tileInfo = new TerrainTileInfo[Size.X * Size.Y];
				foreach (var node in nodes)
				{
					if (!int.TryParse(node.Key, out var key))
						throw new YamlException("Tileset `{0}` template `{1}` defines a frame `{2}` that is not a valid integer.".F(terrainInfo.Id, Id, node.Key));

					if (key < 0 || key >= tileInfo.Length)
						throw new YamlException("Tileset `{0}` template `{1}` references frame {2}, but only [0..{3}] are valid for a {4}x{5} Size template.".F(terrainInfo.Id, Id, key, tileInfo.Length - 1, Size.X, Size.Y));

					tileInfo[key] = LoadTileInfo(terrainInfo, node.Value);
				}
			}
			else
			{
				tileInfo = new TerrainTileInfo[nodes.Count];

				var i = 0;
				foreach (var node in nodes)
				{
					if (!int.TryParse(node.Key, out var key))
						throw new YamlException("Tileset `{0}` template `{1}` defines a frame `{2}` that is not a valid integer.".F(terrainInfo.Id, Id, node.Key));

					if (key != i++)
						throw new YamlException("Tileset `{0}` template `{1}` is missing a definition for frame {2}.".F(terrainInfo.Id, Id, i - 1));

					tileInfo[key] = LoadTileInfo(terrainInfo, node.Value);
				}
			}
		}

		protected virtual TerrainTileInfo LoadTileInfo(ITerrainInfo terrainInfo, MiniYaml my)
		{
			var tile = new TerrainTileInfo();
			FieldLoader.Load(tile, my);

			// Terrain type must be converted from a string to an index
			tile.GetType().GetField(nameof(tile.TerrainType)).SetValue(tile, terrainInfo.GetTerrainIndex(my.Value));

			// Fall back to the terrain-type color if necessary
			var overrideColor = terrainInfo.TerrainTypes[tile.TerrainType].Color;
			if (tile.MinColor == default)
				tile.GetType().GetField(nameof(tile.MinColor)).SetValue(tile, overrideColor);

			if (tile.MaxColor == default)
				tile.GetType().GetField(nameof(tile.MaxColor)).SetValue(tile, overrideColor);

			return tile;
		}

		public TerrainTileInfo this[int index] => tileInfo[index];

		public bool Contains(int index)
		{
			return index >= 0 && index < tileInfo.Length;
		}

		public int TilesCount => tileInfo.Length;
	}
}
