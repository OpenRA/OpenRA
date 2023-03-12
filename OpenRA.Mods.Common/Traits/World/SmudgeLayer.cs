#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public struct MapSmudge
	{
		public string Type;
		public int Depth;
	}

	[TraitLocation(SystemActors.World)]
	[Desc("Attach this to the world actor.", "Order of the layers defines the Z sorting.")]
	public class SmudgeLayerInfo : TraitInfo
	{
		public readonly string Type = "Scorch";

		[Desc("Sprite sequence name")]
		public readonly string Sequence = "scorch";

		[Desc("Chance of smoke rising from the ground")]
		public readonly int SmokeChance = 0;

		[Desc("Smoke sprite image name")]
		public readonly string SmokeImage = null;

		[SequenceReference(nameof(SmokeImage), allowNullImage: true)]
		[Desc("Smoke sprite sequences randomly chosen from")]
		public readonly string[] SmokeSequences = Array.Empty<string>();

		[PaletteReference]
		public readonly string SmokePalette = "effect";

		[PaletteReference]
		public readonly string Palette = TileSet.TerrainPaletteInternalName;

		[FieldLoader.LoadUsing(nameof(LoadInitialSmudges))]
		public readonly Dictionary<CPos, MapSmudge> InitialSmudges;

		public static object LoadInitialSmudges(MiniYaml yaml)
		{
			var nd = yaml.ToDictionary();
			var smudges = new Dictionary<CPos, MapSmudge>();
			if (nd.TryGetValue("InitialSmudges", out var smudgeYaml))
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

		public override object Create(ActorInitializer init) { return new SmudgeLayer(init.Self, this); }
	}

	public class SmudgeLayer : IRenderOverlay, IWorldLoaded, ITickRender, INotifyActorDisposing
	{
		struct Smudge
		{
			public string Type;
			public int Depth;
			public ISpriteSequence Sequence;
		}

		public readonly SmudgeLayerInfo Info;
		readonly Dictionary<CPos, Smudge> tiles = new();
		readonly Dictionary<CPos, Smudge> dirty = new();
		readonly Dictionary<string, ISpriteSequence> smudges = new();
		readonly World world;
		readonly bool hasSmoke;

		TerrainSpriteLayer render;
		PaletteReference paletteReference;
		bool disposed;

		public SmudgeLayer(Actor self, SmudgeLayerInfo info)
		{
			Info = info;
			world = self.World;
			hasSmoke = !string.IsNullOrEmpty(info.SmokeImage) && info.SmokeSequences.Length > 0;

			var sequences = world.Map.Sequences;
			var types = sequences.Sequences(Info.Sequence);
			foreach (var t in types)
				smudges.Add(t, sequences.GetSequence(Info.Sequence, t));
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var sprites = smudges.Values.SelectMany(v => Exts.MakeArray(v.Length, x => v.GetSprite(x))).ToList();
			var sheet = sprites[0].Sheet;
			var blendMode = sprites[0].BlendMode;
			var emptySprite = new Sprite(sheet, Rectangle.Empty, TextureChannel.Alpha);

			if (sprites.Any(s => s.BlendMode != blendMode))
				throw new InvalidDataException("Smudges specify different blend modes. "
					+ "Try using different smudge types for smudges that use different blend modes.");

			paletteReference = wr.Palette(Info.Palette);
			render = new TerrainSpriteLayer(w, wr, emptySprite, blendMode, w.Type != WorldType.Editor);

			// Add map smudges
			foreach (var kv in Info.InitialSmudges)
			{
				var s = kv.Value;
				if (!smudges.ContainsKey(s.Type))
					continue;

				var seq = smudges[s.Type];
				var smudge = new Smudge
				{
					Type = s.Type,
					Depth = s.Depth,
					Sequence = seq
				};

				tiles.Add(kv.Key, smudge);
				render.Update(kv.Key, seq, paletteReference, s.Depth);
			}
		}

		public void AddSmudge(CPos loc)
		{
			if (!world.Map.Contains(loc))
				return;

			if (hasSmoke && Game.CosmeticRandom.Next(0, 100) <= Info.SmokeChance)
				world.AddFrameEndTask(w => w.Add(new SpriteEffect(
					w.Map.CenterOfCell(loc), w, Info.SmokeImage, Info.SmokeSequences.Random(w.SharedRandom), Info.SmokePalette)));

			// A null Sequence indicates a deleted smudge.
			if ((!dirty.ContainsKey(loc) || dirty[loc].Sequence == null) && !tiles.ContainsKey(loc))
			{
				// No smudge; create a new one
				var st = smudges.Keys.Random(Game.CosmeticRandom);
				dirty[loc] = new Smudge { Type = st, Depth = 0, Sequence = smudges[st] };
			}
			else
			{
				// Existing smudge; make it deeper
				// A null Sequence indicates a deleted smudge.
				var tile = dirty.TryGetValue(loc, out var d) && d.Sequence != null ? d : tiles[loc];
				var maxDepth = smudges[tile.Type].Length;
				if (tile.Depth < maxDepth - 1)
					tile.Depth++;

				dirty[loc] = tile;
			}
		}

		public void RemoveSmudge(CPos loc)
		{
			if (!world.Map.Contains(loc))
				return;

			var tile = dirty.TryGetValue(loc, out var d) ? d : default;

			// Setting Sequence to null to indicate a deleted smudge.
			tile.Sequence = null;
			dirty[loc] = tile;
		}

		void ITickRender.TickRender(WorldRenderer wr, Actor self)
		{
			var remove = new List<CPos>();
			foreach (var kv in dirty)
			{
				if (!world.FogObscures(kv.Key))
				{
					// A null Sequence
					if (kv.Value.Sequence == null)
					{
						tiles.Remove(kv.Key);
						render.Clear(kv.Key);
					}
					else
					{
						var smudge = kv.Value;
						tiles[kv.Key] = smudge;
						render.Update(kv.Key, smudge.Sequence, paletteReference, smudge.Depth);
					}

					remove.Add(kv.Key);
				}
			}

			foreach (var r in remove)
				dirty.Remove(r);
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			render.Draw(wr.Viewport);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			render.Dispose();
			disposed = true;
		}
	}
}
