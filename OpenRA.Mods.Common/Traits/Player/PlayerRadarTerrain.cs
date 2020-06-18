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

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class PlayerRadarTerrainInfo : TraitInfo, Requires<ShroudInfo>
	{
		public override object Create(ActorInitializer init)
		{
			return new PlayerRadarTerrain(init.Self);
		}
	}

	public class PlayerRadarTerrain : IWorldLoaded
	{
		public bool IsInitialized { get; private set; }

		readonly World world;
		CellLayer<Pair<int, int>> terrainColor;
		readonly Shroud shroud;

		public event Action<MPos> CellTerrainColorChanged = null;

		public PlayerRadarTerrain(Actor self)
		{
			world = self.World;
			shroud = self.Trait<Shroud>();
			shroud.OnShroudChanged += UpdateShroudCell;
		}

		void UpdateShroudCell(PPos puv)
		{
			var uvs = world.Map.Unproject(puv);
			foreach (var uv in uvs)
				UpdateTerrainCell(uv);
		}

		void UpdateTerrainCell(MPos uv)
		{
			if (!world.Map.CustomTerrain.Contains(uv))
				return;

			if (shroud.IsVisible(uv))
				UpdateTerrainCellColor(uv);
		}

		void UpdateTerrainCellColor(MPos uv)
		{
			terrainColor[uv] = GetColor(world.Map, uv);

			if (CellTerrainColorChanged != null)
				CellTerrainColorChanged(uv);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			terrainColor = new CellLayer<Pair<int, int>>(w.Map);

			w.AddFrameEndTask(_ =>
			{
				// Set initial terrain data
				foreach (var uv in world.Map.AllCells.MapCoords)
					UpdateTerrainCellColor(uv);

				world.Map.Tiles.CellEntryChanged += cell => UpdateTerrainCell(cell.ToMPos(world.Map));
				world.Map.CustomTerrain.CellEntryChanged += cell => UpdateTerrainCell(cell.ToMPos(world.Map));

				IsInitialized = true;
			});
		}

		public Pair<int, int> this[MPos uv]
		{
			get { return terrainColor[uv]; }
		}

		public static Pair<int, int> GetColor(Map map, MPos uv)
		{
			var custom = map.CustomTerrain[uv];
			Color leftColor, rightColor;
			if (custom == byte.MaxValue)
			{
				var tileset = map.Rules.TileSet;
				var type = tileset.GetTileInfo(map.Tiles[uv]);
				if (type != null)
				{
					if (tileset.MinHeightColorBrightness != 1.0f || tileset.MaxHeightColorBrightness != 1.0f)
					{
						var left = Exts.ColorLerp(Game.CosmeticRandom.NextFloat(), type.MinColor, type.MaxColor);
						var right = Exts.ColorLerp(Game.CosmeticRandom.NextFloat(), type.MinColor, type.MaxColor);
						var scale = float2.Lerp(tileset.MinHeightColorBrightness, tileset.MaxHeightColorBrightness, map.Height[uv] * 1f / map.Grid.MaximumTerrainHeight);
						leftColor = Color.FromArgb((int)(scale * left.R).Clamp(0, 255), (int)(scale * left.G).Clamp(0, 255), (int)(scale * left.B).Clamp(0, 255));
						rightColor = Color.FromArgb((int)(scale * right.R).Clamp(0, 255), (int)(scale * right.G).Clamp(0, 255), (int)(scale * right.B).Clamp(0, 255));
					}
					else
						leftColor = rightColor = type.MinColor;
				}
				else
					leftColor = rightColor = Color.Black;
			}
			else
				leftColor = rightColor = map.Rules.TileSet[custom].Color;

			return Pair.New(leftColor.ToArgb(), rightColor.ToArgb());
		}
	}
}
