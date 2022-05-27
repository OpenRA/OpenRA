#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
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
		IRadarTerrainLayer[] radarTerrainLayers;
		CellLayer<(int, int)> terrainColor;
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
			if (shroud.IsVisible(uv))
				UpdateTerrainCellColor(uv);
		}

		void UpdateTerrainCellColor(MPos uv)
		{
			terrainColor[uv] = GetColor(world.Map, radarTerrainLayers, uv);

			CellTerrainColorChanged?.Invoke(uv);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			radarTerrainLayers = w.WorldActor.TraitsImplementing<IRadarTerrainLayer>().ToArray();
			terrainColor = new CellLayer<(int, int)>(w.Map);

			w.AddFrameEndTask(_ =>
			{
				// Set initial terrain data
				foreach (var uv in world.Map.AllCells.MapCoords)
					UpdateTerrainCellColor(uv);

				world.Map.Tiles.CellEntryChanged += cell => UpdateTerrainCell(cell.ToMPos(world.Map));
				foreach (var rtl in radarTerrainLayers)
					rtl.CellEntryChanged += cell => UpdateTerrainCell(cell.ToMPos(world.Map));

				IsInitialized = true;
			});
		}

		public (int Left, int Right) this[MPos uv] => terrainColor[uv];

		public static (int Left, int Right) GetColor(Map map, IRadarTerrainLayer[] radarTerrainLayers, MPos uv)
		{
			foreach (var rtl in radarTerrainLayers)
				if (rtl.TryGetTerrainColorPair(uv, out var c))
					return (c.Left.ToArgb(), c.Right.ToArgb());

			var tc = map.GetTerrainColorPair(uv);
			return (tc.Left.ToArgb(), tc.Right.ToArgb());
		}
	}
}
