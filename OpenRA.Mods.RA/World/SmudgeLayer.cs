#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SmudgeLayerInfo : ITraitInfo
	{
		public readonly string Type = "Scorch";
		public readonly string[] Types = { "sc1", "sc2", "sc3", "sc4", "sc5", "sc6" };
		public readonly int[] Depths = { 1, 1, 1, 1, 1, 1 };
		public readonly int SmokePercentage = 25;
		public readonly string SmokeType = "smoke_m";
		public object Create(ActorInitializer init) { return new SmudgeLayer(this); }
	}

	public class SmudgeLayer : IRenderOverlay, IWorldLoaded, ITickRender
	{
		public SmudgeLayerInfo Info;
		Dictionary<CPos, TileReference<byte, byte>> tiles;
		Dictionary<CPos, TileReference<byte, byte>> dirty;
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
			dirty = new Dictionary<CPos, TileReference<byte, byte>>();

			// Add map smudges
			foreach (var s in w.Map.Smudges.Value.Where(s => Info.Types.Contains(s.Type)))
				tiles.Add((CPos)s.Location, new TileReference<byte, byte>((byte)Array.IndexOf(Info.Types, s.Type), (byte)s.Depth));
		}

		public void AddSmudge(CPos loc)
		{
			if (Game.CosmeticRandom.Next(0, 100) <= Info.SmokePercentage)
				world.AddFrameEndTask(w => w.Add(new Smoke(w, loc.CenterPosition, Info.SmokeType)));

			if (!dirty.ContainsKey(loc) && !tiles.ContainsKey(loc))
			{
				// No smudge; create a new one
				var st = (byte)(1 + world.SharedRandom.Next(Info.Types.Length - 1));
				dirty[loc] = new TileReference<byte, byte>(st, (byte)0);
			}
			else
			{
				// Existing smudge; make it deeper
				var tile = dirty.ContainsKey(loc) ? dirty[loc] : tiles[loc];
				var depth = Info.Depths[tile.Type - 1];
				if (tile.Index < depth - 1)
					tile.Index++;

				dirty[loc] = tile;
			}
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var kv in dirty)
			{
				if (!self.World.FogObscures(kv.Key))
				{
					tiles[kv.Key] = kv.Value;
					remove.Add(kv.Key);
				}
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		public void Render(WorldRenderer wr)
		{
			var cliprect = Game.viewport.WorldBounds(world);
			var pal = wr.Palette("terrain");

			foreach (var kv in tiles)
			{
				if (!cliprect.Contains(kv.Key.X, kv.Key.Y))
					continue;

				if (world.ShroudObscures(kv.Key))
					continue;

				smudgeSprites[kv.Value.Type - 1][kv.Value.Index].DrawAt(kv.Key.ToPPos().ToFloat2(), pal);
			}
		}
	}
}
