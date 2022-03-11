#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "IDE0060:Remove unused parameter", Justification = "Load game API")]
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

	[Desc("Generic sprite sequence implementation, mostly unencumbered with game- or artwork-specific logic.")]
	public class DefaultSpriteSequence : ISpriteSequence
	{
		static readonly WDist DefaultShadowSpriteZOffset = new WDist(-5);
		protected Sprite[] sprites;
		readonly bool reverseFacings, transpose;
		readonly string sequence;

		protected readonly ISpriteSequenceLoader Loader;

		public string Name { get; }

		[Desc("Frame index to start from.")]
		public int Start { get; private set; }

		[Desc("Number of frames to use. Does not have to be the total amount the sprite sheet has.")]
		public int Length { get; private set; } = 1;

		[Desc("Multiplier for the number of facings.")]
		public int Stride { get; private set; }

		[Desc("The amount of directions the unit faces. Use negative values to rotate counter-clockwise.")]
		public int Facings { get; } = 1;

		[Desc("Time (in milliseconds) to wait until playing the next frame in the animation.")]
		public int Tick { get; } = 40;

		[Desc("Value controlling the Z-order. A higher values means rendering on top of other sprites at the same position. " +
		      "Use power of 2 values to avoid glitches.")]
		public int ZOffset { get; }

		[Desc("")]
		public float ZRamp { get; }

		[Desc("If the shadow is not part of the sprite, but baked into the same sprite sheet at a fixed offset, " +
		      "set this to the frame index where it starts.")]
		public int ShadowStart { get; } = -1;

		[Desc("Set Z-Offset for the separate shadow. Used by the later Westwood 2.5D titles. Defined in WDist units!")]
		public int ShadowZOffset { get; }

		[Desc("The individual frames to play instead of going through them sequentially from the `Start`.")]
		public int[] Frames { get; private set; }

		public Rectangle Bounds { get; }

		[Desc("Don't apply terrain lighting or colored overlays.")]
		public bool IgnoreWorldTint { get; }

		[Desc("")]
		public float Scale { get; } = 1f;

		// These need to be public properties for the documentation generation to work.
		[Desc("Play the sprite sequence back and forth.")]
		public static bool Reverses => false;

		[Desc("Support a frame order where each animation step is split per each direction.")]
		public static bool Transpose => false;

		[Desc("Mirror on the X axis.")]
		public bool FlipX { get; }

		[Desc("Mirror on the Y axis.")]
		public bool FlipY { get; }

		[Desc("Change the position in-game on X, Y, Z.")]
		public float3 Offset { get; } = float3.Zero;

		[Desc("Apply an OpenGL/Photoshop inspired blend mode.")]
		public BlendMode BlendMode { get; } = BlendMode.Alpha;

		[Desc("Allows to append multiple sequence definitions which are indented below this node " +
		      "like when offsets differ per frame or a sequence is spread across individual files.")]
		public static object Combine => null;

		[Desc("Sets transparency - use one value to set for all frames or provide a value for each frame.")]
		public float[] Alpha { get; }

		[Desc("Plays a fade out effect.")]
		public static bool AlphaFade => false;

		[Desc("Name of the file containing the depth data sprite.")]
		public string DepthSprite { get; }

		[Desc("Frame index containing the depth data.")]
		public static int DepthSpriteFrame => 0;

		[Desc("")]
		public static float2 DepthSpriteOffset => float2.Zero;

		[Desc("Use the palette embedded in the defined sprite. (Note: The name given here is actually irrelevant)")]
		public static string EmbeddedPalette => null;

		public readonly uint[] EmbeddedPaletteData;

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
				Start = LoadField(d, nameof(Start), 0);
				ShadowStart = LoadField(d, nameof(ShadowStart), ShadowStart);
				ShadowZOffset = LoadField(d, nameof(ShadowZOffset), DefaultShadowSpriteZOffset).Length;
				ZOffset = LoadField(d, nameof(ZOffset), WDist.Zero).Length;
				ZRamp = LoadField(d, nameof(ZRamp), 0f);
				Tick = LoadField(d, nameof(Tick), Tick);
				transpose = LoadField(d, nameof(Transpose), false);
				Frames = LoadField<int[]>(d, nameof(Frames), null);
				IgnoreWorldTint = LoadField(d, nameof(IgnoreWorldTint), false);
				Scale = LoadField(d, nameof(Scale), Scale);

				FlipX = LoadField(d, nameof(FlipX), false);
				FlipY = LoadField(d, nameof(FlipY), false);

				Facings = LoadField(d, nameof(Facings), Facings);
				if (Facings < 0)
				{
					reverseFacings = true;
					Facings = -Facings;
				}

				Offset = LoadField(d, nameof(Offset), Offset);
				BlendMode = LoadField(d, nameof(BlendMode), BlendMode);

				Func<int, IEnumerable<int>> getUsedFrames = frameCount =>
				{
					if (d.TryGetValue(nameof(Length), out var length) && length.Value == "*")
						Length = Frames?.Length ?? frameCount - Start;
					else
						Length = LoadField(d, nameof(Length), Length);

					// Plays the animation forwards, and then in reverse
					if (LoadField(d, nameof(Reverses), false))
					{
						var frames = Frames != null ? Frames.Skip(Start).Take(Length).ToArray() : Exts.MakeArray(Length, i => Start + i);
						Frames = frames.Concat(frames.Skip(1).Take(Length - 2).Reverse()).ToArray();
						Length = 2 * Length - 2;
						Start = 0;
					}

					Stride = LoadField(d, nameof(Stride), Length);

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

				if (d.TryGetValue(nameof(Combine), out var combine))
				{
					var combined = Enumerable.Empty<Sprite>();
					foreach (var sub in combine.Nodes)
					{
						var sd = sub.Value.ToDictionary();

						// Allow per-sprite offset, flipping, start, and length
						var subStart = LoadField(sd, nameof(Start), 0);
						var subOffset = LoadField(sd, nameof(Offset), Offset);
						var subFlipX = LoadField(sd, nameof(FlipX), false);
						var subFlipY = LoadField(sd, nameof(FlipY), false);
						var subFrames = LoadField<int[]>(sd, nameof(Frames), null);
						var subLength = 0;

						Func<int, IEnumerable<int>> subGetUsedFrames = subFrameCount =>
						{
							if (sd.TryGetValue(nameof(Length), out var subLengthYaml) && subLengthYaml.Value == "*")
								subLength = subFrames != null ? subFrames.Length : subFrameCount - subStart;
							else
								subLength = LoadField(sd, nameof(Length), Length);

							return subFrames != null ? subFrames.Skip(subStart).Take(subLength) : Enumerable.Range(subStart, subLength);
						};

						var subSrc = GetSpriteSrc(modData, tileSet, sequence, animation, sub.Key, sd);
						var subSprites = cache[subSrc, subGetUsedFrames].Select(s =>
						{
							if (s == null)
								return null;

							var bounds = FlipRectangle(s.Bounds, subFlipX, subFlipY);
							var dx = subOffset.X + Offset.X + (subFlipX ? -s.Offset.X : s.Offset.X);
							var dy = subOffset.Y + Offset.Y + (subFlipY ? -s.Offset.Y : s.Offset.Y);
							var dz = subOffset.Z + Offset.Z + s.Offset.Z + ZRamp * dy;

							return new Sprite(s.Sheet, bounds, ZRamp, new float3(dx, dy, dz), s.Channel, BlendMode);
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

						var bounds = FlipRectangle(s.Bounds, FlipX, FlipY);
						var dx = Offset.X + (FlipX ? -s.Offset.X : s.Offset.X);
						var dy = Offset.Y + (FlipY ? -s.Offset.Y : s.Offset.Y);
						var dz = Offset.Z + s.Offset.Z + ZRamp * dy;

						return new Sprite(s.Sheet, bounds, ZRamp, new float3(dx, dy, dz), s.Channel, BlendMode);
					}).ToArray();
				}

				Alpha = LoadField(d, nameof(Alpha), (float[])null);
				if (Alpha != null)
				{
					if (Alpha.Length == 1)
						Alpha = Exts.MakeArray(Length, _ => Alpha[0]);
					else if (Alpha.Length != Length)
						throw new YamlException($"Sequence {sequence}.{animation} must define either 1 or {Length} Alpha values.");
				}

				if (LoadField(d, nameof(AlphaFade), false))
				{
					if (Alpha != null)
						throw new YamlException($"Sequence {sequence}.{animation} cannot define both AlphaFade and Alpha.");

					Alpha = Exts.MakeArray(Length, i => float2.Lerp(1f, 0f, i / (Length - 1f)));
				}

				DepthSprite = LoadField<string>(d, nameof(DepthSprite), null);
				if (!string.IsNullOrEmpty(DepthSprite))
				{
					var depthSpriteFrame = LoadField(d, nameof(DepthSpriteFrame), 0);
					var depthOffset = LoadField(d, nameof(DepthSpriteOffset), DepthSpriteOffset);
					IEnumerable<int> GetDepthFrame(int _) => new[] { depthSpriteFrame };
					var ds = cache[DepthSprite, GetDepthFrame][depthSpriteFrame];

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

				var exportPalette = LoadField<string>(d, nameof(EmbeddedPalette), null);
				if (exportPalette != null)
				{
					var src = GetSpriteSrc(modData, tileSet, sequence, animation, info.Value, d);

					var metadata = cache.FrameMetadata(src);
					var i = Frames != null ? Frames[0] : Start;
					var palettes = metadata?.GetOrDefault<EmbeddedSpritePalette>();
					if (palettes == null || !palettes.TryGetPaletteForFrame(i, out EmbeddedPaletteData))
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
			return Alpha?[frame] ?? 1f;
		}
	}
}
