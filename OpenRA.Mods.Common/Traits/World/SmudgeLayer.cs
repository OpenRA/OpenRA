#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the world actor.", "Order of the layers defines the Z sorting.")]
	public class SmudgeLayerInfo : ITraitInfo
	{
		public readonly string Type = "Scorch";

		[Desc("Sprite sequence name")]
		public readonly string Sequence = "scorch";

		public readonly int SmokePercentage = 25;

		[Desc("Sprite sequence name")]
		public readonly string SmokeType = "smoke_m";

		public readonly string SmokePalette = "effect";

		public readonly string Palette = "terrain";

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

		public readonly SmudgeLayerInfo Info;
		readonly Dictionary<CPos, Smudge> tiles = new Dictionary<CPos, Smudge>();
		readonly Dictionary<CPos, Smudge> tilesDirty = new Dictionary<CPos, Smudge>();
		readonly Dictionary<string, Sprite[]> smudges = new Dictionary<string, Sprite[]>();

		World world;
		VertexCache vertexCache;

		public SmudgeLayer(SmudgeLayerInfo info)
		{
			this.Info = info;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			vertexCache = new VertexCache(world.Map);

			var types = world.Map.SequenceProvider.Sequences(Info.Sequence);
			foreach (var t in types)
			{
				var seq = world.Map.SequenceProvider.GetSequence(Info.Sequence, t);
				var sprites = Exts.MakeArray(seq.Length, x => seq.GetSprite(x));
				smudges.Add(t, sprites);
			}

			// Add map smudges
			foreach (var s in w.Map.SmudgeDefinitions)
			{
				var name = s.Key;
				var vals = name.Split(' ');
				var type = vals[0];

				if (!smudges.ContainsKey(type))
					continue;

				var loc = vals[1].Split(',');
				var cell = new CPos(Exts.ParseIntegerInvariant(loc[0]), Exts.ParseIntegerInvariant(loc[1]));
				var depth = Exts.ParseIntegerInvariant(vals[2]);

				var smudge = new Smudge
				{
					Type = type,
					Depth = depth,
					Sprite = smudges[type][depth]
				};

				tiles.Add(cell, smudge);
			}
		}

		public void AddSmudge(CPos loc)
		{
			if (Game.CosmeticRandom.Next(0, 100) <= Info.SmokePercentage)
				world.AddFrameEndTask(w => w.Add(new Smoke(w, world.Map.CenterOfCell(loc), Info.SmokeType, Info.SmokePalette)));

			if (!tilesDirty.ContainsKey(loc) && !tiles.ContainsKey(loc))
			{
				// No smudge; create a new one
				var st = smudges.Keys.Random(world.SharedRandom);
				tilesDirty[loc] = new Smudge { Type = st, Depth = 0, Sprite = smudges[st][0] };
			}
			else
			{
				// Existing smudge; make it deeper
				var tile = tilesDirty.ContainsKey(loc) ? tilesDirty[loc] : tiles[loc];
				var maxDepth = smudges[tile.Type].Length;
				if (tile.Depth < maxDepth - 1)
				{
					tile.Depth++;
					tile.Sprite = smudges[tile.Type][tile.Depth];
				}

				tilesDirty[loc] = tile;
			}
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var kv in tilesDirty)
			{
				var cell = kv.Key;
				if (!self.World.FogObscures(cell))
				{
					tiles[cell] = kv.Value;
					vertexCache.Invalidate(cell);
					remove.Add(cell);
				}
			}

			foreach (var r in remove)
				tilesDirty.Remove(r);
		}

		public void Render(WorldRenderer wr)
		{
			var pal = wr.Palette(Info.Palette);
			var visibleCells = wr.Viewport.VisibleCells;
			var shroudObscured = world.ShroudObscuresTest(visibleCells);
			foreach (var kv in tiles)
			{
				var uv = kv.Key.ToMPos(world.Map);
				if (!visibleCells.Contains(uv) || shroudObscured(uv))
					continue;
				vertexCache.RenderCenteredOverCell(wr, kv.Value.Sprite, pal, uv);
			}
		}
	}
}
