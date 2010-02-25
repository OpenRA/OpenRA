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
using OpenRa.GameRules;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa
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
				for (var j = 0; j < 128; j++)
					for (var i = 0; i < 128; i++)
						if (world.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(new int2(i, j)).Any())
							spriteRenderer.DrawSprite(unitDebug, Game.CellSize * new float2(i, j), "terrain");
		}

		public void DrawBuildingGrid( World world, string name, BuildingInfo bi )
		{
			var position = Game.controller.MousePosition.ToInt2();
			var topLeft = position - Footprint.AdjustForBuildingSize( bi );
			var isCloseEnough = world.IsCloseEnoughToBase(world.LocalPlayer, name, bi, topLeft);

			foreach( var t in Footprint.Tiles( name, bi, topLeft ) )
				spriteRenderer.DrawSprite( ( isCloseEnough && world.IsCellBuildable( t, bi.WaterBound
					? UnitMovementType.Float : UnitMovementType.Wheel ) && !world.Map.ContainsResource( t ) )
					? buildOk : buildBlocked, Game.CellSize * t, "terrain" );

			spriteRenderer.Flush();
		}
	}
}
