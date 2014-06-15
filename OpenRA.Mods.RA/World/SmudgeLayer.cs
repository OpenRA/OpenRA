#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SmudgeLayerInfo : ITraitInfo
	{
		public readonly string Type = "Scorch";
		public readonly string Sequence = "scorch";

		public readonly int SmokePercentage = 25;
		public readonly string SmokeType = "smoke_m";

		public object Create(ActorInitializer init) { return new SmudgeLayer(this); }
	}

	public class SmudgeLayer : IRenderOverlay, IWorldLoaded, ITickRender
	{
		struct Smudge
		{
			public string Type;
			public int Depth;
			public Sprite Sprite;
		}

		public SmudgeLayerInfo Info;
		Dictionary<CPos, Smudge> tiles;
		Dictionary<CPos, Smudge> dirty;
		Dictionary<string, Sprite[]> smudges;
		World world;

		public SmudgeLayer(SmudgeLayerInfo info)
		{
			this.Info = info;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			tiles = new Dictionary<CPos, Smudge>();
			dirty = new Dictionary<CPos, Smudge>();
			smudges = new Dictionary<string, Sprite[]>();

			var types = world.Map.SequenceProvider.Sequences(Info.Sequence);
			foreach (var t in types)
			{
				var seq = world.Map.SequenceProvider.GetSequence(Info.Sequence, t);
				var sprites = Exts.MakeArray(seq.Length, x => seq.GetSprite(x));
				smudges.Add(t, sprites);
			}

			// Add map smudges
			foreach (var s in w.Map.Smudges.Value.Where(s => smudges.Keys.Contains(s.Type)))
			{
				var smudge = new Smudge
				{
					Type = s.Type,
					Depth = s.Depth,
					Sprite = smudges[s.Type][s.Depth]
				};

				tiles.Add((CPos)s.Location, smudge);
			}
		}

		public void AddSmudge(CPos loc)
		{
			if (Game.CosmeticRandom.Next(0, 100) <= Info.SmokePercentage)
				world.AddFrameEndTask(w => w.Add(new Smoke(w, loc.CenterPosition, Info.SmokeType)));

			if (!dirty.ContainsKey(loc) && !tiles.ContainsKey(loc))
			{
				// No smudge; create a new one
				var st = smudges.Keys.Random(world.SharedRandom);
				dirty[loc] = new Smudge { Type = st, Depth = 0, Sprite = smudges[st][0] };
			}
			else
			{
				// Existing smudge; make it deeper
				var tile = dirty.ContainsKey(loc) ? dirty[loc] : tiles[loc];
				var maxDepth = smudges[tile.Type].Length;
				if (tile.Depth < maxDepth - 1)
				{
					tile.Depth++;
					tile.Sprite = smudges[tile.Type][tile.Depth];
				}

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
			var cliprect = wr.Viewport.CellBounds;
			var pal = wr.Palette("terrain");

			foreach (var kv in tiles)
			{
				if (!cliprect.Contains(kv.Key.X, kv.Key.Y))
					continue;

				if (world.ShroudObscures(kv.Key))
					continue;

				new SpriteRenderable(kv.Value.Sprite, kv.Key.CenterPosition,
					WVec.Zero, -511, pal, 1f, true).Render(wr);
			}
		}
	}
}
