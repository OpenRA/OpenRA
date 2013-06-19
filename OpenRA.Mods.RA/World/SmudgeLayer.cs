#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA.Effects;

namespace OpenRA.Mods.RA
{
	public class SmudgeLayerInfo : ITraitInfo
	{
		public readonly string Type = "Scorch";
		public readonly string[] Types = {"sc1", "sc2", "sc3", "sc4", "sc5", "sc6"};
		public readonly int[] Depths = {1,1,1,1,1,1};
		public readonly int SmokePercentage = 25;
		public readonly string SmokeType = "smoke_m";
		public object Create(ActorInitializer init) { return new SmudgeLayer(this); }
	}

	public class SmudgeLayer: IRenderOverlay, IWorldLoaded
	{
		public SmudgeLayerInfo Info;
		Dictionary<CPos, TileReference<byte, byte>> tiles;
		Sprite[][] smudgeSprites;
		World world;

		public SmudgeLayer(SmudgeLayerInfo info)
		{
			this.Info = info;
			smudgeSprites = Info.Types.Select(x => Game.modData.SpriteLoader.LoadAllSprites(x)).ToArray();
		}

		public void WorldLoaded(World w)
		{
			world = w;
			tiles = new Dictionary<CPos, TileReference<byte, byte>>();

			// Add map smudges
			foreach (var s in w.Map.Smudges.Value.Where( s => Info.Types.Contains(s.Type )))
				tiles.Add((CPos)s.Location, new TileReference<byte, byte>((byte)Array.IndexOf(Info.Types, s.Type), (byte)s.Depth));
		}

		public void AddSmudge(CPos loc)
		{
			if (Game.CosmeticRandom.Next(0,100) <= Info.SmokePercentage)
				world.AddFrameEndTask(w => w.Add(new Smoke(w, loc.CenterPosition, Info.SmokeType)));

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
			if (tile.index < depth - 1)
			{
				tile.index++;
				tiles[loc] = tile;	// struct semantics.
			}
		}

		public void Render( WorldRenderer wr )
		{
			var cliprect = Game.viewport.WorldBounds(world);
			var pal = wr.Palette("terrain");

			foreach (var kv in tiles)
			{
				if (!cliprect.Contains(kv.Key.X,kv.Key.Y))
					continue;

				if (world.ShroudObscures(kv.Key))
					continue;

				smudgeSprites[kv.Value.type- 1][kv.Value.index].DrawAt(kv.Key.ToPPos().ToFloat2(), pal);
			}
		}
	}
}
