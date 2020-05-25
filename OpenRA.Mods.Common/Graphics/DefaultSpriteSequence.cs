#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public class EmbeddedSpritePalette
	{
		readonly uint[] filePalette = null;
		readonly Dictionary<int, uint[]> framePalettes = null;

		public EmbeddedSpritePalette(uint[] filePalette = null, Dictionary<int, uint[]> framePalettes = null)
		{
			this.filePalette = filePalette;
			this.framePalettes = framePalettes;
		}

		public bool TryGetPaletteForFrame(int frame, out uint[] palette)
		{
			if (framePalettes == null || !framePalettes.TryGetValue(frame, out palette))
				palette = filePalette;

			return palette != null;
		}
	}

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
				throw new InvalidDataException("Error occurred while parsing {0}".F(node.Key), e);
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
		protected Sprite[] sprites;
		readonly bool reverseFacings, transpose;
		readonly string sequence;

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
		public Rectangle Bounds { get; private set; }

		public readonly uint[] EmbeddedPalette;

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
			this.sequence = sequence;
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

				var flipX = LoadField(d, "FlipX", false);
				var flipY = LoadField(d, "FlipY", false);

				Facings = LoadField(d, "Facings", 1);
				if (Facings < 0)
				{
					reverseFacings = true;
					Facings = -Facings;
				}

				var offset = LoadField(d, "Offset", float3.Zero);
				var blendMode = LoadField(d, "BlendMode", BlendMode.Alpha);

				Func<int, IEnumerable<int>> getUsedFrames = frameCount =>
				{
					MiniYaml length;
					if (d.TryGetValue("Length", out length) && length.Value == "*")
						Length = Frames != null ? Frames.Length : frameCount - Start;
					else
						Length = LoadField(d, "Length", 1);

					// Plays the animation forwards, and then in reverse
					if (LoadField(d, "Reverses", false))
					{
						var frames = Frames != null ? Frames.Skip(Start).Take(Length).ToArray() : Exts.MakeArray(Length, i => Start + i);
						Frames = frames.Concat(frames.Skip(1).Take(Length - 2).Reverse()).ToArray();
						Length = 2 * Length - 2;
						Start = 0;
					}

					Stride = LoadField(d, "Stride", Length);

					if (Length > Stride)
						throw new InvalidOperationException(
							"{0}: Sequence {1}.{2}: Length must be <= stride"
							.F(info.Nodes[0].Location, sequence, animation));

					if (Frames != null && Length > Frames.Length)
						throw new InvalidOperationException(
							"{0}: Sequence {1}.{2}: Length must be <= Frames.Length"
							.F(info.Nodes[0].Location, sequence, animation));

					var end = Start + (Facings - 1) * Stride + Length - 1;
					if (Frames != null)
					{
						foreach (var f in Frames)
							if (f < 0 || f >= frameCount)
								throw new InvalidOperationException(
									"{5}: Sequence {0}.{1} defines a Frames override that references frame {4}, but only [{2}..{3}] actually exist"
										.F(sequence, animation, Start, end, f, info.Nodes[0].Location));

						if (Start < 0 || end >= Frames.Length)
							throw new InvalidOperationException(
								"{5}: Sequence {0}.{1} uses indices [{2}..{3}] of the Frames list, but only {4} frames are defined"
									.F(sequence, animation, Start, end, Frames.Length, info.Nodes[0].Location));
					}
					else if (Start < 0 || end >= frameCount)
						throw new InvalidOperationException(
							"{5}: Sequence {0}.{1} uses frames [{2}..{3}], but only 0..{4} actually exist"
								.F(sequence, animation, Start, end, frameCount - 1, info.Nodes[0].Location));

					if (ShadowStart >= 0 && ShadowStart + (Facings - 1) * Stride + Length > frameCount)
						throw new InvalidOperationException(
							"{5}: Sequence {0}.{1}'s shadow frames use frames [{2}..{3}], but only [0..{4}] actually exist"
							.F(sequence, animation, ShadowStart, ShadowStart + (Facings - 1) * Stride + Length - 1, frameCount - 1,
								info.Nodes[0].Location));

					var usedFrames = new List<int>();
					for (var facing = 0; facing < Facings; facing++)
					{
						for (var frame = 0; frame < Length; frame++)
						{
							var i = transpose ? (frame % Length) * Facings + facing :
								(facing * Stride) + (frame % Length);

							usedFrames.Add(Frames != null ? Frames[i] : Start + i);
						}
					}

					if (ShadowStart >= 0)
						return usedFrames.Concat(usedFrames.Select(i => i + ShadowStart - Start));

					return usedFrames;
				};

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
						var subFrames = LoadField<int[]>(sd, "Frames", null);
						var subLength = 0;

						Func<int, IEnumerable<int>> subGetUsedFrames = subFrameCount =>
						{
							MiniYaml subLengthYaml;
							if (sd.TryGetValue("Length", out subLengthYaml) && subLengthYaml.Value == "*")
								subLength = subFrames != null ? subFrames.Length : subFrameCount - subStart;
							else
								subLength = LoadField(sd, "Length", 1);

							return subFrames != null ? subFrames.Skip(subStart).Take(subLength) : Enumerable.Range(subStart, subLength);
						};

						var subSrc = GetSpriteSrc(modData, tileSet, sequence, animation, sub.Key, sd);
						var subSprites = cache[subSrc, subGetUsedFrames].Select(
							s => s != null ? new Sprite(s.Sheet,
								FlipRectangle(s.Bounds, subFlipX, subFlipY), ZRamp,
								new float3(subFlipX ? -s.Offset.X : s.Offset.X, subFlipY ? -s.Offset.Y : s.Offset.Y, s.Offset.Z) + subOffset + offset,
								s.Channel, blendMode) : null).ToList();

						var frames = subFrames != null ? subFrames.Skip(subStart).Take(subLength).ToArray() : Exts.MakeArray(subLength, i => subStart + i);
						combined = combined.Concat(frames.Select(i => subSprites[i]));
					}

					sprites = combined.ToArray();
					getUsedFrames(sprites.Length);
				}
				else
				{
					// Apply offset to each sprite in the sequence
					// Different sequences may apply different offsets to the same frame
					var src = GetSpriteSrc(modData, tileSet, sequence, animation, info.Value, d);
					sprites = cache[src, getUsedFrames].Select(
						s => s != null ? new Sprite(s.Sheet,
							FlipRectangle(s.Bounds, flipX, flipY), ZRamp,
							new float3(flipX ? -s.Offset.X : s.Offset.X, flipY ? -s.Offset.Y : s.Offset.Y, s.Offset.Z) + offset,
							s.Channel, blendMode) : null).ToArray();
				}

				var depthSprite = LoadField<string>(d, "DepthSprite", null);
				if (!string.IsNullOrEmpty(depthSprite))
				{
					var depthSpriteFrame = LoadField(d, "DepthSpriteFrame", 0);
					var depthOffset = LoadField(d, "DepthSpriteOffset", float2.Zero);
					Func<int, IEnumerable<int>> getDepthFrame = _ => new int[] { depthSpriteFrame };
					var ds = cache[depthSprite, getDepthFrame][depthSpriteFrame];

					sprites = sprites.Select(s =>
					{
						if (s == null)
							return null;

						var cw = (ds.Bounds.Left + ds.Bounds.Right) / 2 + (int)(s.Offset.X + depthOffset.X);
						var ch = (ds.Bounds.Top + ds.Bounds.Bottom) / 2 + (int)(s.Offset.Y + depthOffset.Y);
						var w = s.Bounds.Width / 2;
						var h = s.Bounds.Height / 2;

						var r = Rectangle.FromLTRB(cw - w, ch - h, cw + w, ch + h);
						return new SpriteWithSecondaryData(s, ds.Sheet, r, ds.Channel);
					}).ToArray();
				}

				var exportPalette = LoadField<string>(d, "EmbeddedPalette", null);
				if (exportPalette != null)
				{
					var src = GetSpriteSrc(modData, tileSet, sequence, animation, info.Value, d);

					var metadata = cache.FrameMetadata(src);
					var i = Frames != null ? Frames[0] : Start;
					var palettes = metadata != null ? metadata.GetOrDefault<EmbeddedSpritePalette>() : null;
					if (palettes == null || !palettes.TryGetPaletteForFrame(i, out EmbeddedPalette))
						throw new YamlException("Cannot export palettes from {0}: frame {1} does not define an embedded palette".F(src, i));
				}

				var boundSprites = SpriteBounds(sprites, Frames, Start, Facings, Length, Stride, transpose);
				if (ShadowStart > 0)
					boundSprites = boundSprites.Concat(SpriteBounds(sprites, Frames, ShadowStart, Facings, Length, Stride, transpose));

				Bounds = boundSprites.Union();
			}
			catch (FormatException f)
			{
				throw new FormatException("Failed to parse sequences for {0}.{1} at {2}:\n{3}".F(sequence, animation, info.Nodes[0].Location, f));
			}
		}

		/// <summary>Returns the bounds of all of the sprites that can appear in this animation</summary>
		static IEnumerable<Rectangle> SpriteBounds(Sprite[] sprites, int[] frames, int start, int facings, int length, int stride, bool transpose)
		{
			for (var facing = 0; facing < facings; facing++)
			{
				for (var frame = 0; frame < length; frame++)
				{
					var i = transpose ? (frame % length) * facings + facing :
								(facing * stride) + (frame % length);
					var s = frames != null ? sprites[frames[i]] : sprites[start + i];
					if (!s.Bounds.IsEmpty)
						yield return new Rectangle(
							(int)(s.Offset.X - s.Size.X / 2),
							(int)(s.Offset.Y - s.Size.Y / 2),
							s.Bounds.Width, s.Bounds.Height);
				}
			}
		}

		public Sprite GetSprite(int frame)
		{
			return GetSprite(Start, frame, WAngle.Zero);
		}

		public Sprite GetSprite(int frame, WAngle facing)
		{
			return GetSprite(Start, frame, facing);
		}

		public Sprite GetShadow(int frame, WAngle facing)
		{
			return ShadowStart >= 0 ? GetSprite(ShadowStart, frame, facing) : null;
		}

		protected virtual Sprite GetSprite(int start, int frame, WAngle facing)
		{
			var f = GetFacingFrameOffset(facing);
			if (reverseFacings)
				f = (Facings - f) % Facings;

			var i = transpose ? (frame % Length) * Facings + f :
				(f * Stride) + (frame % Length);

			var j = Frames != null ? Frames[i] : start + i;
			if (sprites[j] == null)
				throw new InvalidOperationException("Attempted to query unloaded sprite from {0}.{1}".F(Name, sequence) +
					" start={0} frame={1} facing={2}".F(start, frame, facing));

			return sprites[j];
		}

		protected virtual int GetFacingFrameOffset(WAngle facing)
		{
			return Util.IndexFacing(facing, Facings);
		}
	}
}
