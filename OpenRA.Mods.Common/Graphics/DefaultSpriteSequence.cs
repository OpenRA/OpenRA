#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Graphics
{
	public class DefaultSpriteSequenceLoader : ISpriteSequenceLoader
	{
		public Action<string> OnMissingSpriteError { get; set; }
		public DefaultSpriteSequenceLoader(ModData modData) { }

		public virtual ISpriteSequence CreateSequence(ModData modData, TileSet tileSet, SpriteCache cache, string sequence, string animation, MiniYaml info)
		{
			return new DefaultSpriteSequence(modData, tileSet, cache, this, sequence, animation, info);
		}

		public IReadOnlyDictionary<string, ISpriteSequence> ParseSequences(ModData modData, TileSet tileSet, SpriteCache cache, MiniYamlNode node)
		{
			var sequences = new Dictionary<string, ISpriteSequence>();
			var nodes = node.Value.ToDictionary();

			MiniYaml defaults;
			if (nodes.TryGetValue("Defaults", out defaults))
			{
				nodes.Remove("Defaults");
				nodes = nodes.ToDictionary(kv => kv.Key, kv => MiniYaml.MergeStrict(kv.Value, defaults));

				foreach (var n in nodes)
					n.Value.Value = n.Value.Value ?? defaults.Value;
			}

			foreach (var kvp in nodes)
			{
				using (new Support.PerfTimer("new Sequence(\"{0}\")".F(node.Key), 20))
				{
					try
					{
						sequences.Add(kvp.Key, CreateSequence(modData, tileSet, cache, node.Key, kvp.Key, kvp.Value));
					}
					catch (FileNotFoundException ex)
					{
						// Eat the FileNotFound exceptions from missing sprites
						OnMissingSpriteError(ex.Message);
					}
				}
			}

			return new ReadOnlyDictionary<string, ISpriteSequence>(sequences);
		}
	}

	public class DefaultSpriteSequence : ISpriteSequence
	{
		static readonly WRange DefaultShadowSpriteZOffset = new WRange(-5);
		readonly Sprite[] sprites;
		readonly bool reverseFacings, transpose;

		protected readonly ISpriteSequenceLoader Loader;

		public string Name { get; private set; }
		public int Start { get; private set; }
		public int Length { get; private set; }
		public int Stride { get; private set; }
		public int Facings { get; private set; }
		public int Tick { get; private set; }
		public int ZOffset { get; private set; }
		public int ShadowStart { get; private set; }
		public int ShadowZOffset { get; private set; }
		public int[] Frames { get; private set; }

		protected virtual string GetSpriteSrc(ModData modData, TileSet tileSet, string sequence, string animation, string sprite, Dictionary<string, MiniYaml> d)
		{
			return sprite ?? sequence;
		}

		protected static T LoadField<T>(Dictionary<string, MiniYaml> d, string key, T fallback)
		{
			MiniYaml value;
			if (d.TryGetValue(key, out value))
				return FieldLoader.GetValue<T>(key, value.Value);

			return fallback;
		}

		public DefaultSpriteSequence(ModData modData, TileSet tileSet, SpriteCache cache, ISpriteSequenceLoader loader, string sequence, string animation, MiniYaml info)
		{
			Name = animation;
			Loader = loader;
			var d = info.ToDictionary();

			try
			{
				Start = LoadField<int>(d, "Start", 0);
				ShadowStart = LoadField<int>(d, "ShadowStart", -1);
				ShadowZOffset = LoadField<WRange>(d, "ShadowZOffset", DefaultShadowSpriteZOffset).Range;
				ZOffset = LoadField<WRange>(d, "ZOffset", WRange.Zero).Range;
				Tick = LoadField<int>(d, "Tick", 40);
				transpose = LoadField<bool>(d, "Transpose", false);
				Frames = LoadField<int[]>(d, "Frames", null);

				Facings = LoadField<int>(d, "Facings", 1);
				if (Facings < 0)
				{
					reverseFacings = true;
					Facings = -Facings;
				}

				var offset = LoadField<float2>(d, "Offset", float2.Zero);
				var blendMode = LoadField<BlendMode>(d, "BlendMode", BlendMode.Alpha);

				// Apply offset to each sprite in the sequence
				// Different sequences may apply different offsets to the same frame
				var src = GetSpriteSrc(modData, tileSet, sequence, animation, info.Value, d);
				sprites = cache[src].Select(
					s => new Sprite(s.Sheet, s.Bounds, s.Offset + offset, s.Channel, blendMode)).ToArray();

				MiniYaml length;
				if (d.TryGetValue("Length", out length) && length.Value == "*")
					Length = sprites.Length - Start;
				else
					Length = LoadField<int>(d, "Length", 1);

				// Plays the animation forwards, and then in reverse
				if (LoadField<bool>(d, "Reverses", false))
				{
					var frames = Frames ?? Exts.MakeArray(Length, i => Start + i);
					Frames = frames.Concat(frames.Skip(1).Take(frames.Length - 2).Reverse()).ToArray();
					Length = 2 * Length - 2;
				}

				Stride = LoadField<int>(d, "Stride", Length);

				if (Length > Stride)
					throw new InvalidOperationException(
						"{0}: Sequence {1}.{2}: Length must be <= stride"
						.F(info.Nodes[0].Location, sequence, animation));

				if (Start < 0 || Start + Facings * Stride > sprites.Length)
					throw new InvalidOperationException(
						"{6}: Sequence {0}.{1} uses frames [{2}..{3}] of SHP `{4}`, but only 0..{5} actually exist"
						.F(sequence, animation, Start, Start + Facings * Stride - 1, src, sprites.Length - 1,
							info.Nodes[0].Location));

				if (ShadowStart + Facings * Stride > sprites.Length)
					throw new InvalidOperationException(
						"{6}: Sequence {0}.{1}'s shadow frames use frames [{2}..{3}] of SHP `{4}`, but only [0..{5}] actually exist"
						.F(sequence, animation, ShadowStart, ShadowStart + Facings * Stride - 1, src, sprites.Length - 1,
							info.Nodes[0].Location));
			}
			catch (FormatException f)
			{
				throw new FormatException("Failed to parse sequences for {0}.{1} at {2}:\n{3}".F(sequence, animation, info.Nodes[0].Location, f));
			}
		}

		public Sprite GetSprite(int frame)
		{
			return GetSprite(Start, frame, 0);
		}

		public Sprite GetSprite(int frame, int facing)
		{
			return GetSprite(Start, frame, facing);
		}

		public Sprite GetShadow(int frame, int facing)
		{
			return ShadowStart >= 0 ? GetSprite(ShadowStart, frame, facing) : null;
		}

		protected virtual Sprite GetSprite(int start, int frame, int facing)
		{
			var f = OpenRA.Traits.Util.QuantizeFacing(facing, Facings);

			if (reverseFacings)
				f = (Facings - f) % Facings;

			var i = transpose ? (frame % Length) * Facings + f :
				(f * Stride) + (frame % Length);

			if (Frames != null)
				return sprites[Frames[i]];

			return sprites[start + i];
		}
	}
}
