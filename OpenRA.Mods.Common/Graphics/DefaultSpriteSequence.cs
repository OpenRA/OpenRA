#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		public DefaultSpriteSequenceLoader(ModData modData) { }

		public virtual ISpriteSequence CreateSequence(ModData modData, string tileSet, SpriteCache cache, string sequence, string animation, MiniYaml info)
		{
			return new DefaultSpriteSequence(modData, tileSet, cache, this, sequence, animation, info);
		}

		public IReadOnlyDictionary<string, ISpriteSequence> ParseSequences(ModData modData, string tileSet, SpriteCache cache, MiniYamlNode node)
		{
			var sequences = new Dictionary<string, ISpriteSequence>();
			var nodes = node.Value.ToDictionary();

			try
			{
				if (nodes.TryGetValue("Defaults", out var defaults))
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
				throw new InvalidDataException($"Error occurred while parsing {node.Key}", e);
			}

			foreach (var kvp in nodes)
			{
				using (new Support.PerfTimer($"new Sequence(\"{node.Key}\")", 20))
				{
					try
					{
						sequences.Add(kvp.Key, CreateSequence(modData, tileSet, cache, node.Key, kvp.Key, kvp.Value));
					}
					catch (FileNotFoundException ex)
					{
						// Defer exception until something tries to access the sequence
						// This allows the asset installer and OpenRA.Utility to load the game without having the actor assets
						sequences.Add(kvp.Key, new FileNotFoundSequence(ex));
					}
				}
			}

			return new ReadOnlyDictionary<string, ISpriteSequence>(sequences);
		}
	}

	public class FileNotFoundSequence : ISpriteSequence
	{
		readonly FileNotFoundException exception;

		public FileNotFoundSequence(FileNotFoundException exception)
		{
			this.exception = exception;
		}

		public string Filename => exception.FileName;

		string ISpriteSequence.Name => throw exception;
		int ISpriteSequence.Start => throw exception;
		int ISpriteSequence.Length => throw exception;
		int ISpriteSequence.Stride => throw exception;
		int ISpriteSequence.Facings => throw exception;
		int ISpriteSequence.Tick => throw exception;
		int ISpriteSequence.ZOffset => throw exception;
		int ISpriteSequence.ShadowStart => throw exception;
		int ISpriteSequence.ShadowZOffset => throw exception;
		int[] ISpriteSequence.Frames => throw exception;
		Rectangle ISpriteSequence.Bounds => throw exception;
		bool ISpriteSequence.IgnoreWorldTint => throw exception;
		float ISpriteSequence.Scale => throw exception;
		Sprite ISpriteSequence.GetSprite(int frame) { throw exception; }
		Sprite ISpriteSequence.GetSprite(int frame, WAngle facing) { throw exception; }
		Sprite ISpriteSequence.GetShadow(int frame, WAngle facing) { throw exception; }
		float ISpriteSequence.GetAlpha(int frame) { throw exception; }
	}

	public class DefaultSpriteSequence : ISpriteSequence
	{
		static readonly WDist DefaultShadowSpriteZOffset = new WDist(-5);
		protected Sprite[] sprites;
		readonly bool reverseFacings, transpose;
		readonly string sequence;
		readonly float[] alpha;

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
		public bool IgnoreWorldTint { get; private set; }
		public float Scale { get; private set; }

		public readonly uint[] EmbeddedPalette;

		protected virtual string GetSpriteSrc(ModData modData, string tileSet, string sequence, string animation, string sprite, Dictionary<string, MiniYaml> d)
		{
			return sprite ?? sequence;
		}

		protected static T LoadField<T>(Dictionary<string, MiniYaml> d, string key, T fallback)
		{
			if (d.TryGetValue(key, out var value))
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

		public DefaultSpriteSequence(ModData modData, string tileSet, SpriteCache cache, ISpriteSequenceLoader loader, string sequence, string animation, MiniYaml info)
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
				IgnoreWorldTint = LoadField(d, "IgnoreWorldTint", false);
				Scale = LoadField(d, "Scale", 1f);

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
					if (d.TryGetValue("Length", out var length) && length.Value == "*")
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
						throw new YamlException($"Sequence {sequence}.{animation}: Length must be <= stride");

					if (Frames != null && Length > Frames.Length)
						throw new YamlException($"Sequence {sequence}.{animation}: Length must be <= Frames.Length");

					var end = Start + (Facings - 1) * Stride + Length - 1;
					if (Frames != null)
					{
						foreach (var f in Frames)
							if (f < 0 || f >= frameCount)
								throw new YamlException($"Sequence {sequence}.{animation} defines a Frames override that references frame {f}, but only [{Start}..{end}] actually exist");

						if (Start < 0 || end >= Frames.Length)
							throw new YamlException($"Sequence {sequence}.{animation} uses indices [{Start}..{end}] of the Frames list, but only {Frames.Length} frames are defined");
					}
					else if (Start < 0 || end >= frameCount)
						throw new YamlException($"Sequence {sequence}.{animation} uses frames [{Start}..{end}], but only [0..{frameCount - 1}] actually exist");

					if (ShadowStart >= 0 && ShadowStart + (Facings - 1) * Stride + Length > frameCount)
						throw new YamlException($"Sequence {sequence}.{animation}'s shadow frames use frames [{ShadowStart}..{ShadowStart + (Facings - 1) * Stride + Length - 1}], but only [0..{frameCount - 1}] actually exist");

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

				if (d.TryGetValue("Combine", out var combine))
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
							if (sd.TryGetValue("Length", out var subLengthYaml) && subLengthYaml.Value == "*")
								subLength = subFrames != null ? subFrames.Length : subFrameCount - subStart;
							else
								subLength = LoadField(sd, "Length", 1);

							return subFrames != null ? subFrames.Skip(subStart).Take(subLength) : Enumerable.Range(subStart, subLength);
						};

						var subSrc = GetSpriteSrc(modData, tileSet, sequence, animation, sub.Key, sd);
						var subSprites = cache[subSrc, subGetUsedFrames].Select(s =>
						{
							if (s == null)
								return null;

							var bounds = FlipRectangle(s.Bounds, subFlipX, subFlipY);
							var dx = subOffset.X + offset.X + (subFlipX ? -s.Offset.X : s.Offset.X);
							var dy = subOffset.Y + offset.Y + (subFlipY ? -s.Offset.Y : s.Offset.Y);
							var dz = subOffset.Z + offset.Z + s.Offset.Z + ZRamp * dy;

							return new Sprite(s.Sheet, bounds, ZRamp, new float3(dx, dy, dz), s.Channel, blendMode);
						}).ToList();

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
					sprites = cache[src, getUsedFrames].Select(s =>
					{
						if (s == null)
							return null;

						var bounds = FlipRectangle(s.Bounds, flipX, flipY);
						var dx = offset.X + (flipX ? -s.Offset.X : s.Offset.X);
						var dy = offset.Y + (flipY ? -s.Offset.Y : s.Offset.Y);
						var dz = offset.Z + s.Offset.Z + ZRamp * dy;

						return new Sprite(s.Sheet, bounds, ZRamp, new float3(dx, dy, dz), s.Channel, blendMode);
					}).ToArray();
				}

				alpha = LoadField(d, "Alpha", (float[])null);
				if (alpha != null)
				{
					if (alpha.Length == 1)
						alpha = Exts.MakeArray(Length, _ => alpha[0]);
					else if (alpha.Length != Length)
						throw new YamlException($"Sequence {sequence}.{animation} must define either 1 or {Length} Alpha values.");
				}

				if (LoadField(d, "AlphaFade", false))
				{
					if (alpha != null)
						throw new YamlException($"Sequence {sequence}.{animation} cannot define both AlphaFade and Alpha.");

					alpha = Exts.MakeArray(Length, i => float2.Lerp(1f, 0f, i / (Length - 1f)));
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
						throw new YamlException($"Cannot export palettes from {src}: frame {i} does not define an embedded palette");
				}

				var boundSprites = SpriteBounds(sprites, Frames, Start, Facings, Length, Stride, transpose);
				if (ShadowStart > 0)
					boundSprites = boundSprites.Concat(SpriteBounds(sprites, Frames, ShadowStart, Facings, Length, Stride, transpose));

				Bounds = boundSprites.Union();
			}
			catch (FormatException f)
			{
				throw new FormatException($"Failed to parse sequences for {sequence}.{animation} at {info.Nodes[0].Location}:\n{f}");
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
				throw new InvalidOperationException($"Attempted to query unloaded sprite from {Name}.{sequence} start={start} frame={frame} facing={facing}");

			return sprites[j];
		}

		protected virtual int GetFacingFrameOffset(WAngle facing)
		{
			return Util.IndexFacing(facing, Facings);
		}

		public virtual float GetAlpha(int frame)
		{
			return alpha?[frame] ?? 1f;
		}
	}
}
