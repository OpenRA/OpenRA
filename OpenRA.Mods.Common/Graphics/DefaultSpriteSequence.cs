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

using System;
using System.Collections.Generic;
using System.Drawing;
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
			try
			{
				if (nodes.TryGetValue("Defaults", out defaults))
				{
					nodes.Remove("Defaults");
					foreach (var n in nodes)
					{
						n.Value.Nodes = MiniYaml.Merge(new[] { defaults.Nodes, n.Value.Nodes });
						n.Value.Value = n.Value.Value ?? defaults.Value;
					}
				}
			}
			catch (Exception e)
			{
				throw new Exception("Error occurred while parsing {0}".F(node.Key), e);
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
		static readonly WDist DefaultShadowSpriteZOffset = new WDist(-5);
		readonly Sprite[] sprites;
		readonly bool reverseFacings, transpose, useClassicFacingFudge;

		protected readonly ISpriteSequenceLoader Loader;

		public string Name { get; private set; }
		public int Start { get; private set; }
		public int Length { get; private set; }
		public int Stride { get; private set; }
		public int Facings { get; private set; }
		public int Tick { get; private set; }
		public int ZOffset { get; private set; }
		public float ZRamp { get; private set; }
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

		protected static Rectangle FlipRectangle(Rectangle rect, bool flipX, bool flipY)
		{
			var left = flipX ? rect.Right : rect.Left;
			var top = flipY ? rect.Bottom : rect.Top;
			var right = flipX ? rect.Left : rect.Right;
			var bottom = flipY ? rect.Top : rect.Bottom;

			return Rectangle.FromLTRB(left, top, right, bottom);
		}

		public DefaultSpriteSequence(ModData modData, TileSet tileSet, SpriteCache cache, ISpriteSequenceLoader loader, string sequence, string animation, MiniYaml info)
		{
			Name = animation;
			Loader = loader;
			var d = info.ToDictionary();

			try
			{
				Start = LoadField(d, "Start", 0);
				ShadowStart = LoadField(d, "ShadowStart", -1);
				ShadowZOffset = LoadField(d, "ShadowZOffset", DefaultShadowSpriteZOffset).Length;
				ZOffset = LoadField(d, "ZOffset", WDist.Zero).Length;
				ZRamp = LoadField(d, "ZRamp", 0);
				Tick = LoadField(d, "Tick", 40);
				transpose = LoadField(d, "Transpose", false);
				Frames = LoadField<int[]>(d, "Frames", null);
				useClassicFacingFudge = LoadField(d, "UseClassicFacingFudge", false);

				var flipX = LoadField(d, "FlipX", false);
				var flipY = LoadField(d, "FlipY", false);

				Facings = LoadField(d, "Facings", 1);
				if (Facings < 0)
				{
					reverseFacings = true;
					Facings = -Facings;
				}

				if (useClassicFacingFudge && Facings != 32)
					throw new InvalidOperationException(
						"{0}: Sequence {1}.{2}: UseClassicFacingFudge is only valid for 32 facings"
						.F(info.Nodes[0].Location, sequence, animation));

				var offset = LoadField(d, "Offset", float3.Zero);
				var blendMode = LoadField(d, "BlendMode", BlendMode.Alpha);

				MiniYaml combine;
				if (d.TryGetValue("Combine", out combine))
				{
					var combined = Enumerable.Empty<Sprite>();
					foreach (var sub in combine.Nodes)
					{
						var sd = sub.Value.ToDictionary();

						// Allow per-sprite offset, flipping, start, and length
						var subStart = LoadField(sd, "Start", 0);
						var subOffset = LoadField(sd, "Offset", float3.Zero);
						var subFlipX = LoadField(sd, "FlipX", false);
						var subFlipY = LoadField(sd, "FlipY", false);

						var subSrc = GetSpriteSrc(modData, tileSet, sequence, animation, sub.Key, sd);
						var subSprites = cache[subSrc].Select(
							s => new Sprite(s.Sheet,
								FlipRectangle(s.Bounds, subFlipX, subFlipY), ZRamp,
								new float3(subFlipX ? -s.Offset.X : s.Offset.X, subFlipY ? -s.Offset.Y : s.Offset.Y, s.Offset.Z) + subOffset + offset,
								s.Channel, blendMode));

						var subLength = 0;
						MiniYaml subLengthYaml;
						if (sd.TryGetValue("Length", out subLengthYaml) && subLengthYaml.Value == "*")
							subLength = subSprites.Count() - subStart;
						else
							subLength = LoadField(sd, "Length", 1);

						combined = combined.Concat(subSprites.Skip(subStart).Take(subLength));
					}

					sprites = combined.ToArray();
				}
				else
				{
					// Apply offset to each sprite in the sequence
					// Different sequences may apply different offsets to the same frame
					var src = GetSpriteSrc(modData, tileSet, sequence, animation, info.Value, d);
					sprites = cache[src].Select(
						s => new Sprite(s.Sheet,
							FlipRectangle(s.Bounds, flipX, flipY), ZRamp,
							new float3(flipX ? -s.Offset.X : s.Offset.X, flipY ? -s.Offset.Y : s.Offset.Y, s.Offset.Z) + offset,
							s.Channel, blendMode)).ToArray();
				}

				var depthSprite = LoadField<string>(d, "DepthSprite", null);
				if (!string.IsNullOrEmpty(depthSprite))
				{
					var depthSpriteFrame = LoadField(d, "DepthSpriteFrame", 0);
					var depthOffset = LoadField(d, "DepthSpriteOffset", float2.Zero);
					var depthSprites = cache.AllCached(depthSprite)
						.Select(s => s[depthSpriteFrame]);

					sprites = sprites.Select(s =>
					{
						// The depth sprite must live on the same sheet as the main sprite
						var ds = depthSprites.FirstOrDefault(dss => dss.Sheet == s.Sheet);
						if (ds == null)
						{
							// The sequence has probably overflowed onto a new sheet.
							// Allocating a new depth sprite on this sheet will almost certainly work
							ds = cache.Reload(depthSprite)[depthSpriteFrame];
							depthSprites = cache.AllCached(depthSprite)
								.Select(ss => ss[depthSpriteFrame]);

							// If that doesn't work then we may be referencing a cached sprite from an earlier sheet
							// TODO: We could try and reallocate the main sprite, but that requires more complicated code and a perf hit
							// We'll only cross that bridge if this becomes a problem in reality
							if (ds.Sheet != s.Sheet)
								throw new SheetOverflowException("Cross-sheet depth sprite reference: {0}.{1}: {2}");
						}

						var cw = (ds.Bounds.Left + ds.Bounds.Right) / 2 + (int)(s.Offset.X + depthOffset.X);
						var ch = (ds.Bounds.Top + ds.Bounds.Bottom) / 2 + (int)(s.Offset.Y + depthOffset.Y);
						var w = s.Bounds.Width / 2;
						var h = s.Bounds.Height / 2;

						var r = Rectangle.FromLTRB(cw - w, ch - h, cw + w, ch + h);
						return new SpriteWithSecondaryData(s, r, ds.Channel);
					}).ToArray();
				}

				MiniYaml length;
				if (d.TryGetValue("Length", out length) && length.Value == "*")
					Length = sprites.Length - Start;
				else
					Length = LoadField(d, "Length", 1);

				// Plays the animation forwards, and then in reverse
				if (LoadField(d, "Reverses", false))
				{
					var frames = Frames ?? Exts.MakeArray(Length, i => Start + i);
					Frames = frames.Concat(frames.Skip(1).Take(frames.Length - 2).Reverse()).ToArray();
					Length = 2 * Length - 2;
				}

				Stride = LoadField(d, "Stride", Length);

				if (Length > Stride)
					throw new InvalidOperationException(
						"{0}: Sequence {1}.{2}: Length must be <= stride"
						.F(info.Nodes[0].Location, sequence, animation));

				if (Start < 0 || Start + Facings * Stride > sprites.Length)
					throw new InvalidOperationException(
						"{5}: Sequence {0}.{1} uses frames [{2}..{3}], but only 0..{4} actually exist"
						.F(sequence, animation, Start, Start + Facings * Stride - 1, sprites.Length - 1,
							info.Nodes[0].Location));

				if (ShadowStart + Facings * Stride > sprites.Length)
					throw new InvalidOperationException(
						"{5}: Sequence {0}.{1}'s shadow frames use frames [{2}..{3}], but only [0..{4}] actually exist"
						.F(sequence, animation, ShadowStart, ShadowStart + Facings * Stride - 1, sprites.Length - 1,
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
			var f = Util.QuantizeFacing(facing, Facings, useClassicFacingFudge);
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
