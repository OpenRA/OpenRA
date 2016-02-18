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
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
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
		[SequenceReference("SmokeType")] public readonly string SmokeSequence = "idle";

		[PaletteReference] public readonly string SmokePalette = "effect";

		[PaletteReference] public readonly string Palette = TileSet.TerrainPaletteInternalName;

		public object Create(ActorInitializer init) { return new SmudgeLayer(init.Self, this); }
	}

	public class SmudgeLayer : IRenderOverlay, IWorldLoaded, ITickRender, INotifyActorDisposing
	{
		struct Smudge
		{
			public string Type;
			public int Depth;
			public Sprite Sprite;
		}

		public readonly SmudgeLayerInfo Info;
		readonly Dictionary<CPos, Smudge> tiles = new Dictionary<CPos, Smudge>();
		readonly Dictionary<CPos, Smudge> dirty = new Dictionary<CPos, Smudge>();
		readonly Dictionary<string, Sprite[]> smudges = new Dictionary<string, Sprite[]>();
		readonly World world;

		TerrainSpriteLayer render;

		public SmudgeLayer(Actor self, SmudgeLayerInfo info)
		{
			Info = info;
			world = self.World;

			var types = world.Map.SequenceProvider.Sequences(Info.Sequence);
			foreach (var t in types)
			{
				var seq = world.Map.SequenceProvider.GetSequence(Info.Sequence, t);
				var sprites = Exts.MakeArray(seq.Length, x => seq.GetSprite(x));
				smudges.Add(t, sprites);
			}
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var first = smudges.First().Value.First();
			var sheet = first.Sheet;
			if (smudges.Values.Any(sprites => sprites.Any(s => s.Sheet != sheet)))
				throw new InvalidDataException("Resource sprites span multiple sheets. Try loading their sequences earlier.");

			var blendMode = first.BlendMode;
			if (smudges.Values.Any(sprites => sprites.Any(s => s.BlendMode != blendMode)))
				throw new InvalidDataException("Smudges specify different blend modes. "
					+ "Try using different smudge types for smudges that use different blend modes.");

			render = new TerrainSpriteLayer(w, wr, sheet, blendMode, wr.Palette(Info.Palette), wr.World.Type != WorldType.Editor);

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
				render.Update(cell, smudge.Sprite);
			}
		}

		public void AddSmudge(CPos loc)
		{
			if (Game.CosmeticRandom.Next(0, 100) <= Info.SmokePercentage)
				world.AddFrameEndTask(w => w.Add(new Smoke(w, world.Map.CenterOfCell(loc), Info.SmokeType, Info.SmokePalette, Info.SmokeSequence)));

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
					render.Update(kv.Key, kv.Value.Sprite);

					remove.Add(kv.Key);
				}
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		public void Render(WorldRenderer wr)
		{
			render.Draw(wr.Viewport);
		}

		bool disposed;
		public void Disposing(Actor self)
		{
			if (disposed)
				return;

			render.Dispose();
			disposed = true;
		}
	}
}
