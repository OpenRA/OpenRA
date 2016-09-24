#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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
	public struct MapSmudge
	{
		public string Type;
		public int Depth;
	}

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

		[FieldLoader.LoadUsing("LoadInitialSmudges")]
		public readonly Dictionary<CPos, MapSmudge> InitialSmudges;

		public static object LoadInitialSmudges(MiniYaml yaml)
		{
			MiniYaml smudgeYaml;
			var nd = yaml.ToDictionary();
			var smudges = new Dictionary<CPos, MapSmudge>();
			if (nd.TryGetValue("InitialSmudges", out smudgeYaml))
			{
				foreach (var node in smudgeYaml.Nodes)
				{
					try
					{
						var cell = FieldLoader.GetValue<CPos>("key", node.Key);
						var parts = node.Value.Value.Split(',');
						var type = parts[0];
						var depth = FieldLoader.GetValue<int>("depth", parts[1]);
						smudges.Add(cell, new MapSmudge { Type = type, Depth = depth });
					}
					catch { }
				}
			}

			return smudges;
		}

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

			var sequenceProvider = world.Map.Rules.Sequences;
			var types = sequenceProvider.Sequences(Info.Sequence);
			foreach (var t in types)
			{
				var seq = sequenceProvider.GetSequence(Info.Sequence, t);
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
			foreach (var kv in Info.InitialSmudges)
			{
				var s = kv.Value;
				if (!smudges.ContainsKey(s.Type))
					continue;

				var smudge = new Smudge
				{
					Type = s.Type,
					Depth = s.Depth,
					Sprite = smudges[s.Type][s.Depth]
				};

				tiles.Add(kv.Key, smudge);
				render.Update(kv.Key, smudge.Sprite);
			}
		}

		public void AddSmudge(CPos loc)
		{
			if (Game.CosmeticRandom.Next(0, 100) <= Info.SmokePercentage)
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(world.Map.CenterOfCell(loc), w, Info.SmokeType, Info.SmokeSequence, Info.SmokePalette)));

			// A null Sprite indicates a deleted smudge.
			if ((!dirty.ContainsKey(loc) || dirty[loc].Sprite == null) && !tiles.ContainsKey(loc))
			{
				// No smudge; create a new one
				var st = smudges.Keys.Random(Game.CosmeticRandom);
				dirty[loc] = new Smudge { Type = st, Depth = 0, Sprite = smudges[st][0] };
			}
			else
			{
				// Existing smudge; make it deeper
				// A null Sprite indicates a deleted smudge.
				var tile = dirty.ContainsKey(loc) && dirty[loc].Sprite != null ? dirty[loc] : tiles[loc];
				var maxDepth = smudges[tile.Type].Length;
				if (tile.Depth < maxDepth - 1)
				{
					tile.Depth++;
					tile.Sprite = smudges[tile.Type][tile.Depth];
				}

				dirty[loc] = tile;
			}
		}

		public void RemoveSmudge(CPos loc)
		{
			var tile = dirty.ContainsKey(loc) ? dirty[loc] : new Smudge();

			// Setting Sprite to null to indicate a deleted smudge.
			tile.Sprite = null;
			dirty[loc] = tile;
		}

		public void TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var kv in dirty)
			{
				if (!self.World.FogObscures(kv.Key))
				{
					// A null Sprite indicates a deleted smudge.
					if (kv.Value.Sprite == null)
						tiles.Remove(kv.Key);
					else
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
