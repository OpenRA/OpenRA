#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class SmudgeLayerInfo : ITraitInfo
	{
		public readonly string Type = "Scorch";
		public readonly string[] Types = {"sc1", "sc2", "sc3", "sc4", "sc5", "sc6"};
		public readonly int[] Depths = {1,1,1,1,1,1};
		public object Create(ActorInitializer init) { return new SmudgeLayer(this); }
	}

	public class SmudgeLayer: IRenderOverlay, ILoadWorldHook
	{		
		public SmudgeLayerInfo Info;
		SpriteRenderer spriteRenderer;
		TileReference<byte,byte>[,] tiles;
		Sprite[][] smudgeSprites;
		World world;

		public SmudgeLayer(SmudgeLayerInfo info)
		{
			spriteRenderer = Game.Renderer.SpriteRenderer;
			this.Info = info;
			smudgeSprites = Info.Types.Select(x => SpriteSheetBuilder.LoadAllSprites(x)).ToArray();
		}
		
		public void WorldLoaded(World w)
		{
			world = w;
			tiles = new TileReference<byte,byte>[w.Map.MapSize.X,w.Map.MapSize.Y];
			
			// Add map smudges
			foreach (var s in w.Map.Smudges.Where( s => Info.Types.Contains(s.Type )))
				tiles[s.Location.X,s.Location.Y] = new TileReference<byte,byte>((byte)Array.IndexOf(Info.Types,s.Type),
				                                                  (byte)s.Depth);
		}
		
		public void AddSmudge(int2 loc)
		{
			if (!world.GetTerrainInfo(loc).AcceptSmudge)
				return;

			// No smudge; create a new one
			if (tiles[loc.X, loc.Y].type == 0)
			{
				byte st = (byte)(1 + world.SharedRandom.Next(Info.Types.Length - 1));
				tiles[loc.X,loc.Y] = new TileReference<byte,byte>(st,(byte)0);
				return;
			}
			
			// Existing smudge; make it deeper
			int depth = Info.Depths[tiles[loc.X, loc.Y].type-1];
			if (tiles[loc.X, loc.Y].image < depth - 1)
				tiles[loc.X,loc.Y].image++;
		}
		
		public void Render()
		{
			var cliprect = Game.viewport.ShroudBounds().HasValue 
				? Rectangle.Intersect(Game.viewport.ShroudBounds().Value, world.Map.Bounds) : world.Map.Bounds;

			var minx = cliprect.Left;
			var maxx = cliprect.Right;

			var miny = cliprect.Top;
			var maxy = cliprect.Bottom;

			for (int x = minx; x < maxx; x++)
				for (int y = miny; y < maxy; y++)
				{
					var t = new int2(x, y);
					if (world.LocalPlayer != null && !world.LocalPlayer.Shroud.IsExplored(t) || tiles[x,y].type == 0) continue;
	
					spriteRenderer.DrawSprite(smudgeSprites[tiles[x,y].type- 1][tiles[x,y].image],
						Game.CellSize * t, "terrain");
				}
		}
	}
}
