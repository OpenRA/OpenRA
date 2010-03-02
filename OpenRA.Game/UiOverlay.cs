#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA
{
	class UiOverlay
	{
		SpriteRenderer spriteRenderer;
		Sprite buildOk, buildBlocked, unitDebug;

		public static bool ShowUnitDebug = false;

		public UiOverlay(SpriteRenderer spriteRenderer)
		{
			this.spriteRenderer = spriteRenderer;

			buildOk = SynthesizeTile(0x80);
			buildBlocked = SynthesizeTile(0xe6);
			unitDebug = SynthesizeTile(0x7c);
		}

		static Sprite SynthesizeTile(byte paletteIndex)
		{
			byte[] data = new byte[Game.CellSize * Game.CellSize];

			for (int i = 0; i < Game.CellSize; i++)
				for (int j = 0; j < Game.CellSize; j++)
					data[i * Game.CellSize + j] = ((i + j) % 4 < 2) ? (byte)0 : paletteIndex;

			return SheetBuilder.SharedInstance.Add(data, new Size(Game.CellSize, Game.CellSize));
		}

		public void Draw( World world )
		{
			if (ShowUnitDebug)
				for (var j = 0; j < world.Map.MapSize; j++)
					for (var i = 0; i < world.Map.MapSize; i++)
						if (world.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(new int2(i, j)).Any())
							spriteRenderer.DrawSprite(unitDebug, Game.CellSize * new float2(i, j), "terrain");
		}

		public void DrawBuildingGrid( World world, string name, BuildingInfo bi )
		{
			var position = Game.controller.MousePosition.ToInt2();
			var topLeft = position - Footprint.AdjustForBuildingSize( bi );
			var isCloseEnough = world.IsCloseEnoughToBase(world.LocalPlayer, name, bi, topLeft);
			var res = world.WorldActor.traits.Get<ResourceLayer>();

			foreach( var t in Footprint.Tiles( name, bi, topLeft ) )
				spriteRenderer.DrawSprite( ( isCloseEnough && world.IsCellBuildable( t, bi.WaterBound
					? UnitMovementType.Float : UnitMovementType.Wheel ) && res.GetResource(t) == null )
					? buildOk : buildBlocked, Game.CellSize * t, "terrain" );
			
			// Linebuild for walls.
			// Assumes a 1x1 footprint; weird things will happen for other footprints
			if (Rules.Info[ name ].Traits.Contains<LineBuildInfo>())
			{
				int range = Rules.Info[ name ].Traits.Get<LineBuildInfo>().Range;
				
				// Start at place location, search outwards
				// TODO: First make it work, then make it nice
				int[] dirs = {0,0,0,0};
				for (int d = 0; d < 4; d++)
				{
					for (int i = 1; i < range; i++)
					{
						if (dirs[d] != 0)
							continue;
						
						int2 cell = world.OffsetCell(topLeft,i,d);
						
						if (world.IsCellBuildable(cell, bi.WaterBound ? UnitMovementType.Float : UnitMovementType.Wheel,null))
							continue; // Cell is empty; continue search

						// Cell contains an actor. Is it the type we want?
						if (Game.world.Queries.WithTrait<LineBuild>().Any(a => (a.Actor.Info.Name == name && a.Actor.Location.X == cell.X && a.Actor.Location.Y == cell.Y)))
							dirs[d] = i; // Cell contains actor of correct type
						else
							dirs[d] = -1; // Cell is blocked by another actor type
					}
					
					// Place intermediate-line sections
					if (dirs[d] > 0)
					{
						for (int i = 1; i < dirs[d]; i++)
						{
							int2 cell = world.OffsetCell(topLeft,i,d);
							spriteRenderer.DrawSprite( world.IsCloseEnoughToBase(world.LocalPlayer, name, bi, cell) ? buildOk : buildBlocked, Game.CellSize * cell, "terrain" );
						}
					}
				}
			}
			spriteRenderer.Flush();
		}
	}
}
