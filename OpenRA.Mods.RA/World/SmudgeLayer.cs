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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SmudgeLayerInfo : ITraitInfo
	{
		public readonly string Type = "Scorch";
		public readonly string[] Types = {"sc1", "sc2", "sc3", "sc4", "sc5", "sc6"};
		public readonly int[] Depths = {1,1,1,1,1,1};
		public object Create(ActorInitializer init) { return new SmudgeLayer(this); }
	}

	public class SmudgeLayer: IRenderOverlay, IWorldLoaded
	{		
		public SmudgeLayerInfo Info;
		Dictionary<int2,TileReference<byte,byte>> tiles;
		Sprite[][] smudgeSprites;
		World world;

		public SmudgeLayer(SmudgeLayerInfo info)
		{
			this.Info = info;
			smudgeSprites = Info.Types.Select(x => SpriteSheetBuilder.LoadAllSprites(x)).ToArray();
		}
		
		public void WorldLoaded(World w)
		{
			world = w;
			tiles = new Dictionary<int2,TileReference<byte,byte>>();
			
			// Add map smudges
			foreach (var s in w.Map.Smudges.Where( s => Info.Types.Contains(s.Type )))
				tiles.Add(s.Location,new TileReference<byte,byte>((byte)Array.IndexOf(Info.Types,s.Type),
				                                                  (byte)s.Depth));
		}
		
		public void AddSmudge(int2 loc)
		{
			if (!world.GetTerrainInfo(loc).AcceptSmudge)
				return;

			// No smudge; create a new one
			if (!tiles.ContainsKey(loc))
			{
				byte st = (byte)(1 + world.SharedRandom.Next(Info.Types.Length - 1));
				tiles.Add(loc, new TileReference<byte,byte>(st,(byte)0));
				return;
			}
			
			var tile = tiles[loc];
			// Existing smudge; make it deeper
			int depth = Info.Depths[tile.type-1];
			if (tile.image < depth - 1)
			{
				tile.image++;
				tiles[loc] = tile;	// struct semantics.
			}
		}
		
		public void Render( WorldRenderer wr )
		{
			var cliprect = Game.viewport.ShroudBounds( world );
			cliprect = Rectangle.Intersect(Game.viewport.ViewBounds(), cliprect);
			var localPlayer = world.LocalPlayer;
			foreach (var kv in tiles)
			{
				if (!cliprect.Contains(kv.Key.X,kv.Key.Y))
					continue;
				if (localPlayer != null && !localPlayer.Shroud.IsExplored(kv.Key))
					continue;

				smudgeSprites[kv.Value.type- 1][kv.Value.image].DrawAt( wr,
						Game.CellSize * kv.Key, "terrain");
			}
		}
	}
}
