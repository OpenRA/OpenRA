#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
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

		public UiOverlay(SpriteRenderer spriteRenderer)
		{
			this.spriteRenderer = spriteRenderer;

			buildOk = SynthesizeTile(0x0f);
			buildBlocked = SynthesizeTile(0x08);
			unitDebug = SynthesizeTile(0x04);
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
			if (Game.Settings.UnitDebug)
			{
				var uim = world.WorldActor.traits.Get<UnitInfluence>();
				
				for (var i = world.Map.Bounds.Left; i < world.Map.Bounds.Right; i++)
					for (var j = world.Map.Bounds.Top; j < world.Map.Bounds.Bottom; j++)	
						if (uim.GetUnitsAt(new int2(i, j)).Any())
							spriteRenderer.DrawSprite(unitDebug, Game.CellSize * new float2(i, j), "terrain");
			}
		}

		public void DrawBuildingGrid( World world, string name, BuildingInfo bi )
		{
			var position = Game.controller.MousePosition.ToInt2();
			var topLeft = position - Footprint.AdjustForBuildingSize( bi );
			
			// Linebuild for walls.
			// Assumes a 1x1 footprint; weird things will happen for other footprints
			if (Rules.Info[name].Traits.Contains<LineBuildInfo>())
			{
				foreach (var t in LineBuildUtils.GetLineBuildCells(world, topLeft, name, bi))
					spriteRenderer.DrawSprite(world.IsCloseEnoughToBase(world.LocalPlayer, name, bi, t)
						? buildOk : buildBlocked, Game.CellSize * t, "terrain");
			}
			else
			{
				var res = world.WorldActor.traits.Get<ResourceLayer>();
				var isCloseEnough = world.IsCloseEnoughToBase(world.LocalPlayer, name, bi, topLeft);
				foreach (var t in Footprint.Tiles(name, bi, topLeft))
					spriteRenderer.DrawSprite((isCloseEnough && world.IsCellBuildable(t, bi.WaterBound) && res.GetResource(t) == null)
						? buildOk : buildBlocked, Game.CellSize * t, "terrain");
			}
			
			spriteRenderer.Flush();
		}
	}

	static class LineBuildUtils
	{
		public static IEnumerable<int2> GetLineBuildCells(World world, int2 location, string name, BuildingInfo bi)
		{
			int range = Rules.Info[name].Traits.Get<LineBuildInfo>().Range;
			var topLeft = location;	// 1x1 assumption!

			if (world.IsCellBuildable(topLeft, bi.WaterBound))
				yield return topLeft;

			// Start at place location, search outwards
			// TODO: First make it work, then make it nice
			var vecs = new[] { new int2(1, 0), new int2(0, 1), new int2(-1, 0), new int2(0, -1) };
			int[] dirs = { 0, 0, 0, 0 };
			for (int d = 0; d < 4; d++)
			{
				for (int i = 1; i < range; i++)
				{
					if (dirs[d] != 0)
						continue;

					int2 cell = topLeft + i * vecs[d];
					if (world.IsCellBuildable(cell, bi.WaterBound))
						continue; // Cell is empty; continue search

					// Cell contains an actor. Is it the type we want?
					if (world.Queries.WithTrait<LineBuild>().Any(a => (a.Actor.Info.Name == name && a.Actor.Location.X == cell.X && a.Actor.Location.Y == cell.Y)))
						dirs[d] = i; // Cell contains actor of correct type
					else
						dirs[d] = -1; // Cell is blocked by another actor type
				}

				// Place intermediate-line sections
				if (dirs[d] > 0)
					for (int i = 1; i < dirs[d]; i++)
						yield return topLeft + i * vecs[d];
			}
		}
	}
}
