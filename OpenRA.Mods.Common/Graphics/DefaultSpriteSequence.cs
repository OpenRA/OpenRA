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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public class DefaultSpriteSequenceLoader : ISpriteSequenceLoader
	{
		static readonly MiniYaml NoData = new MiniYaml(null);

		public DefaultSpriteSequenceLoader(ModData modData) { }

		public virtual ISpriteSequence CreateSequence(ModData modData, string tileset, SpriteCache cache, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			return new DefaultSpriteSequence(modData, tileset, cache, this, image, sequence, data, defaults);
		}

		public IReadOnlyDictionary<string, ISpriteSequence> ParseSequences(ModData modData, string tileSet, SpriteCache cache, MiniYamlNode imageNode)
		{
			var sequences = new Dictionary<string, ISpriteSequence>();
			MiniYaml defaults;
			try
			{
				var node = imageNode.Value.Nodes.SingleOrDefault(n => n.Key == "Defaults");
				defaults = node?.Value ?? NoData;
				imageNode.Value.Nodes.Remove(node);
			}
			catch (Exception e)
			{
				throw new InvalidDataException($"Error occurred while parsing {imageNode.Key}", e);
			}

			foreach (var sequenceNode in imageNode.Value.Nodes)
			{
				using (new Support.PerfTimer($"new Sequence(\"{imageNode.Key}\")", 20))
				{
					try
					{
						var sequence = CreateSequence(modData, tileSet, cache, imageNode.Key, sequenceNode.Key, sequenceNode.Value, defaults);
						sequences.Add(sequenceNode.Key, sequence);
					}
					catch (FileNotFoundException ex)
					{
						// Defer exception until something tries to access the sequence
						// This allows the asset installer and OpenRA.Utility to load the game without having the actor assets
						sequences.Add(sequenceNode.Key, new FileNotFoundSequence(ex));
					}
					catch (FormatException f)
					{
						throw new FormatException($"Failed to parse sequences for {imageNode.Key}.{sequenceNode.Key} at {imageNode.Value.Nodes[0].Location}:\n{f}");
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
		int ISpriteSequence.InterpolatedFacings => throw exception;
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
		(Sprite, WAngle) ISpriteSequence.GetSpriteWithRotation(int frame, WAngle facing) { throw exception; }
		Sprite ISpriteSequence.GetShadow(int frame, WAngle facing) { throw exception; }
		float ISpriteSequence.GetAlpha(int frame) { throw exception; }
	}

	public struct SpriteSequenceField<T>
	{
		public string Key;
		public T DefaultValue;

		public SpriteSequenceField(string key, T defaultValue)
		{
			Key = key;
			DefaultValue = defaultValue;
		}
	}

	[Desc("Generic sprite sequence implementation, mostly unencumbered with game- or artwork-specific logic.")]
	public class DefaultSpriteSequence : ISpriteSequence
	{
		static readonly MiniYaml NoData = new MiniYaml(null);

		readonly string image;
		protected Sprite[] sprites;
		readonly bool reverseFacings, transpose;

		protected readonly ISpriteSequenceLoader Loader;

		public Rectangle Bounds { get; }

		public string Name { get; }

		[Desc("File name of the sprite to use for this sequence.")]
		static readonly SpriteSequenceField<string> Filename = new SpriteSequenceField<string>(nameof(Filename), null);

		[Desc("Frame index to start from.")]
		static readonly SpriteSequenceField<int> Start = new SpriteSequenceField<int>(nameof(Start), 0);
		int ISpriteSequence.Start => start;
		int start;

		[Desc("Number of frames to use. Does not have to be the total amount the sprite sheet has.")]
		static readonly SpriteSequenceField<int> Length = new SpriteSequenceField<int>(nameof(Length), 1);
		int ISpriteSequence.Length => length;
		int length;

		[Desc("Overrides Length if a different number of frames is defined between facings.")]
		static readonly SpriteSequenceField<int> Stride = new SpriteSequenceField<int>(nameof(Stride), -1);
		int ISpriteSequence.Stride => stride;
		int stride;

		[Desc("The amount of directions the unit faces. Use negative values to rotate counter-clockwise.")]
		static readonly SpriteSequenceField<int> Facings = new SpriteSequenceField<int>(nameof(Facings), 1);
		int ISpriteSequence.Facings => facings;
		protected int facings;

		[Desc("The amount of directions the unit faces. Use negative values to rotate counter-clockwise.")]
		static readonly SpriteSequenceField<int> InterpolatedFacings = new SpriteSequenceField<int>(nameof(InterpolatedFacings), 1);
		int ISpriteSequence.InterpolatedFacings => interpolatedFacings;
		protected int interpolatedFacings;

		[Desc("Time (in milliseconds at default game speed) to wait until playing the next frame in the animation.")]
		static readonly SpriteSequenceField<int> Tick = new SpriteSequenceField<int>(nameof(Tick), 40);
		int ISpriteSequence.Tick => tick;
		readonly int tick;

		[Desc("Value controlling the Z-order. A higher values means rendering on top of other sprites at the same position. " +
			"Use power of 2 values to avoid glitches.")]
		static readonly SpriteSequenceField<WDist> ZOffset = new SpriteSequenceField<WDist>(nameof(ZOffset), WDist.Zero);
		int ISpriteSequence.ZOffset => zOffset;
		readonly int zOffset;

		[Desc("Additional sprite depth Z offset to apply as a function of sprite Y (0: vertical, 1: flat on terrain)")]
		static readonly SpriteSequenceField<int> ZRamp = new SpriteSequenceField<int>(nameof(ZRamp), 0);

		[Desc("If the shadow is not part of the sprite, but baked into the same sprite sheet at a fixed offset, " +
			"set this to the frame index where it starts.")]
		static readonly SpriteSequenceField<int> ShadowStart = new SpriteSequenceField<int>(nameof(ShadowStart), -1);
		int ISpriteSequence.ShadowStart => shadowStart;
		readonly int shadowStart;

		[Desc("Set Z-Offset for the separate shadow. Used by the later Westwood 2.5D titles.")]
		static readonly SpriteSequenceField<WDist> ShadowZOffset = new SpriteSequenceField<WDist>(nameof(ShadowZOffset), new WDist(-5));
		int ISpriteSequence.ShadowZOffset => shadowZOffset;
		readonly int shadowZOffset;

		[Desc("The individual frames to play instead of going through them sequentially from the `Start`.")]
		static readonly SpriteSequenceField<int[]> Frames = new SpriteSequenceField<int[]>(nameof(Frames), null);
		int[] ISpriteSequence.Frames => frames;
		int[] frames;

		[Desc("Don't apply terrain lighting or colored overlays.")]
		static readonly SpriteSequenceField<bool> IgnoreWorldTint = new SpriteSequenceField<bool>(nameof(IgnoreWorldTint), false);
		bool ISpriteSequence.IgnoreWorldTint => ignoreWorldTint;
		readonly bool ignoreWorldTint;

		[Desc("Adjusts the rendered size of the sprite")]
		static readonly SpriteSequenceField<float> Scale = new SpriteSequenceField<float>(nameof(Scale), 1);
		float ISpriteSequence.Scale => scale;
		readonly float scale;

		[Desc("Play the sprite sequence back and forth.")]
		static readonly SpriteSequenceField<bool> Reverses = new SpriteSequenceField<bool>(nameof(Reverses), false);

		[Desc("Support a frame order where each animation step is split per each direction.")]
		static readonly SpriteSequenceField<bool> Transpose = new SpriteSequenceField<bool>(nameof(Transpose), false);

		[Desc("Mirror on the X axis.")]
		static readonly SpriteSequenceField<bool> FlipX = new SpriteSequenceField<bool>(nameof(FlipX), false);

		[Desc("Mirror on the Y axis.")]
		static readonly SpriteSequenceField<bool> FlipY = new SpriteSequenceField<bool>(nameof(FlipY), false);

		[Desc("Change the position in-game on X, Y, Z.")]
		static readonly SpriteSequenceField<float3> Offset = new SpriteSequenceField<float3>(nameof(Offset), float3.Zero);

		[Desc("Apply an OpenGL/Photoshop inspired blend mode.")]
		static readonly SpriteSequenceField<BlendMode> BlendMode = new SpriteSequenceField<BlendMode>(nameof(BlendMode), OpenRA.BlendMode.Alpha);

		[Desc("Allows to append multiple sequence definitions which are indented below this node " +
			"like when offsets differ per frame or a sequence is spread across individual files.")]
		static readonly SpriteSequenceField<MiniYaml> Combine = new SpriteSequenceField<MiniYaml>(nameof(Combine), null);

		[Desc("Sets transparency - use one value to set for all frames or provide a value for each frame.")]
		static readonly SpriteSequenceField<float[]> Alpha = new SpriteSequenceField<float[]>(nameof(Alpha), null);
		readonly float[] alpha;

		[Desc("Fade the animation from fully opaque on the first frame to fully transparent after the last frame.")]
		static readonly SpriteSequenceField<bool> AlphaFade = new SpriteSequenceField<bool>(nameof(AlphaFade), false);

		[Desc("Name of the file containing the depth data sprite.")]
		static readonly SpriteSequenceField<string> DepthSprite = new SpriteSequenceField<string>(nameof(DepthSprite), null);

		[Desc("Frame index containing the depth data.")]
		static readonly SpriteSequenceField<int> DepthSpriteFrame = new SpriteSequenceField<int>(nameof(DepthSpriteFrame), 0);

		[Desc("X, Y offset to apply to the depth sprite.")]
		static readonly SpriteSequenceField<float2> DepthSpriteOffset = new SpriteSequenceField<float2>(nameof(DepthSpriteOffset), float2.Zero);

		protected virtual string GetSpriteFilename(ModData modData, string tileset, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			return LoadField(Filename, data, defaults);
		}

		protected static T LoadField<T>(string key, T fallback, MiniYaml data, MiniYaml defaults)
		{
			var node = data.Nodes.FirstOrDefault(n => n.Key == key) ?? defaults.Nodes.FirstOrDefault(n => n.Key == key);
			if (node == null)
				return fallback;

			return FieldLoader.GetValue<T>(key, node.Value.Value);
		}

		protected static T LoadField<T>(SpriteSequenceField<T> field, MiniYaml data, MiniYaml defaults)
		{
			var node = data.Nodes.FirstOrDefault(n => n.Key == field.Key) ?? defaults.Nodes.FirstOrDefault(n => n.Key == field.Key);
			if (node == null)
				return field.DefaultValue;

			return FieldLoader.GetValue<T>(field.Key, node.Value.Value);
		}

		protected static Rectangle FlipRectangle(Rectangle rect, bool flipX, bool flipY)
		{
			var left = flipX ? rect.Right : rect.Left;
			var top = flipY ? rect.Bottom : rect.Top;
			var right = flipX ? rect.Left : rect.Right;
			var bottom = flipY ? rect.Top : rect.Bottom;

			return Rectangle.FromLTRB(left, top, right, bottom);
		}

		public DefaultSpriteSequence(ModData modData, string tileSet, SpriteCache cache, ISpriteSequenceLoader loader, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			this.image = image;
			Name = sequence;
			Loader = loader;

			start = LoadField(Start, data, defaults);
			shadowStart = LoadField(ShadowStart, data, defaults);
			shadowZOffset = LoadField(ShadowZOffset, data, defaults).Length;
			zOffset = LoadField(ZOffset, data, defaults).Length;
			tick = LoadField(Tick, data, defaults);
			transpose = LoadField(Transpose, data, defaults);
			frames = LoadField(Frames, data, defaults);
			ignoreWorldTint = LoadField(IgnoreWorldTint, data, defaults);
			scale = LoadField(Scale, data, defaults);

			var flipX = LoadField(FlipX, data, defaults);
			var flipY = LoadField(FlipY, data, defaults);
			var zRamp = LoadField(ZRamp, data, defaults);

			facings = LoadField(Facings, data, defaults);
			interpolatedFacings = LoadField(InterpolatedFacings.Key, -1, data, defaults);
			if (interpolatedFacings != -1 && (interpolatedFacings <= 1 || interpolatedFacings <= Math.Abs(facings) || interpolatedFacings > 1024
				|| !Exts.IsPowerOf2(interpolatedFacings)))
				throw new YamlException($"InterpolatedFacings must be greater than Facings, within the range of 2 to 1024, and a power of 2.");

			if (facings < 0)
			{
				reverseFacings = true;
				facings = -facings;
			}

			var offset = LoadField(Offset, data, defaults);
			var blendMode = LoadField(BlendMode, data, defaults);

			IEnumerable<int> GetUsedFrames(int frameCount)
			{
				if (LoadField(Length.Key, "", data, defaults) == "*")
					length = frames?.Length ?? frameCount - start;
				else
					length = LoadField(Length, data, defaults);

				// Plays the animation forwards, and then in reverse
				if (LoadField(Reverses, data, defaults))
				{
					var frames = this.frames != null ? this.frames.Skip(start).Take(length).ToArray() : Exts.MakeArray(length, i => start + i);
					this.frames = frames.Concat(frames.Skip(1).Take(length - 2).Reverse()).ToArray();
					length = 2 * length - 2;
					start = 0;
				}

				// Overrides Length with a custom stride
				stride = LoadField(Stride.Key, length, data, defaults);

				if (length > stride)
					throw new YamlException($"Sequence {image}.{sequence}: Length must be <= stride");

				if (frames != null && length > frames.Length)
					throw new YamlException($"Sequence {image}.{sequence}: Length must be <= Frames.Length");

				var end = start + (facings - 1) * stride + length - 1;
				if (frames != null)
				{
					foreach (var f in frames)
						if (f < 0 || f >= frameCount)
							throw new YamlException($"Sequence {image}.{sequence} defines a Frames override that references frame {f}, but only [{start}..{end}] actually exist");

					if (start < 0 || end >= frames.Length)
						throw new YamlException($"Sequence {image}.{sequence} uses indices [{start}..{end}] of the Frames list, but only {frames.Length} frames are defined");
				}
				else if (start < 0 || end >= frameCount)
					throw new YamlException($"Sequence {image}.{sequence} uses frames [{start}..{end}], but only [0..{frameCount - 1}] actually exist");

				if (shadowStart >= 0 && shadowStart + (facings - 1) * stride + length > frameCount)
					throw new YamlException($"Sequence {image}.{sequence}'s shadow frames use frames [{shadowStart}..{shadowStart + (facings - 1) * stride + length - 1}], but only [0..{frameCount - 1}] actually exist");

				var usedFrames = new List<int>();
				for (var facing = 0; facing < facings; facing++)
				{
					for (var frame = 0; frame < length; frame++)
					{
						var i = transpose ? frame % length * facings + facing :
							facing * stride + frame % length;

						usedFrames.Add(frames != null ? frames[i] : start + i);
					}
				}

				if (shadowStart >= 0)
					return usedFrames.Concat(usedFrames.Select(i => i + shadowStart - start));

				return usedFrames;
			}

			var combineNode = data.Nodes.FirstOrDefault(n => n.Key == Combine.Key);
			if (combineNode != null)
			{
				var combined = Enumerable.Empty<Sprite>();
				foreach (var combineSequenceNode in combineNode.Value.Nodes)
				{
					var combineData = combineSequenceNode.Value;

					// Allow per-sprite offset, flipping, start, and length
					// These shouldn't inherit Start/Offset/etc from the main definition
					var subStart = LoadField(Start, combineData, NoData);
					var subOffset = LoadField(Offset, combineData, NoData);
					var subFlipX = LoadField(FlipX, combineData, NoData);
					var subFlipY = LoadField(FlipY, combineData, NoData);
					var subFrames = LoadField(Frames, combineData, NoData);
					var subLength = 0;

					IEnumerable<int> SubGetUsedFrames(int subFrameCount)
					{
						var combineLengthNode = combineData.Nodes.FirstOrDefault(n => n.Key == Length.Key);
						if (combineLengthNode?.Value.Value == "*")
							subLength = subFrames?.Length ?? subFrameCount - subStart;
						else
							subLength = LoadField(Length, combineData, NoData);

						return subFrames != null ? subFrames.Skip(subStart).Take(subLength) : Enumerable.Range(subStart, subLength);
					}

					var subFilename = GetSpriteFilename(modData, tileSet, image, sequence, combineData, NoData);
					if (subFilename == null)
						throw new YamlException($"Sequence {image}.{sequence}.{combineSequenceNode.Key} does not define a filename.");

					var subSprites = cache[subFilename, SubGetUsedFrames].Select(s =>
					{
						if (s == null)
							return null;

						var bounds = FlipRectangle(s.Bounds, subFlipX, subFlipY);
						var dx = subOffset.X + offset.X + (subFlipX ? -s.Offset.X : s.Offset.X);
						var dy = subOffset.Y + offset.Y + (subFlipY ? -s.Offset.Y : s.Offset.Y);
						var dz = subOffset.Z + offset.Z + s.Offset.Z + zRamp * dy;

						return new Sprite(s.Sheet, bounds, zRamp, new float3(dx, dy, dz), s.Channel, blendMode);
					}).ToList();

					var frames = subFrames != null ? subFrames.Skip(subStart).Take(subLength).ToArray() : Exts.MakeArray(subLength, i => subStart + i);
					combined = combined.Concat(frames.Select(i => subSprites[i]));
				}

				sprites = combined.ToArray();
				GetUsedFrames(sprites.Length);
			}
			else
			{
				// Apply offset to each sprite in the sequence
				// Different sequences may apply different offsets to the same frame
				var filename = GetSpriteFilename(modData, tileSet, image, sequence, data, defaults);
				if (filename == null)
					throw new YamlException($"Sequence {image}.{sequence} does not define a filename.");

				sprites = cache[filename, GetUsedFrames].Select(s =>
				{
					if (s == null)
						return null;

					var bounds = FlipRectangle(s.Bounds, flipX, flipY);
					var dx = offset.X + (flipX ? -s.Offset.X : s.Offset.X);
					var dy = offset.Y + (flipY ? -s.Offset.Y : s.Offset.Y);
					var dz = offset.Z + s.Offset.Z + zRamp * dy;

					return new Sprite(s.Sheet, bounds, zRamp, new float3(dx, dy, dz), s.Channel, blendMode);
				}).ToArray();
			}

			alpha = LoadField(Alpha, data, defaults);
			if (alpha != null)
			{
				if (alpha.Length == 1)
					alpha = Exts.MakeArray(length, _ => alpha[0]);
				else if (alpha.Length != length)
					throw new YamlException($"Sequence {image}.{sequence} must define either 1 or {length} Alpha values.");
			}

			if (LoadField(AlphaFade, data, defaults))
			{
				if (alpha != null)
					throw new YamlException($"Sequence {image}.{sequence} cannot define both AlphaFade and Alpha.");

				alpha = Exts.MakeArray(length, i => float2.Lerp(1f, 0f, i / (length - 1f)));
			}

			var depthSprite = LoadField(DepthSprite, data, defaults);
			if (!string.IsNullOrEmpty(depthSprite))
			{
				var depthSpriteFrame = LoadField(DepthSpriteFrame, data, defaults);
				var depthOffset = LoadField(DepthSpriteOffset, data, defaults);
				IEnumerable<int> GetDepthFrame(int _) => new[] { depthSpriteFrame };
				var ds = cache[depthSprite, GetDepthFrame][depthSpriteFrame];

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

			var boundSprites = SpriteBounds(sprites, frames, start, facings, length, stride, transpose);
			if (shadowStart > 0)
				boundSprites = boundSprites.Concat(SpriteBounds(sprites, frames, shadowStart, facings, length, stride, transpose));

			Bounds = boundSprites.Union();
		}

		/// <summary>Returns the bounds of all of the sprites that can appear in this animation</summary>
		static IEnumerable<Rectangle> SpriteBounds(Sprite[] sprites, int[] frames, int start, int facings, int length, int stride, bool transpose)
		{
			for (var facing = 0; facing < facings; facing++)
			{
				for (var frame = 0; frame < length; frame++)
				{
					var i = transpose ? frame % length * facings + facing :
								facing * stride + frame % length;
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
			return GetSprite(start, frame, WAngle.Zero);
		}

		public Sprite GetSprite(int frame, WAngle facing)
		{
			return GetSprite(start, frame, facing);
		}

		public (Sprite, WAngle) GetSpriteWithRotation(int frame, WAngle facing)
		{
			var rotation = WAngle.Zero;

			// Note: Error checking is not done here as it is done on load
			if (interpolatedFacings != -1)
				rotation = Util.GetInterpolatedFacing(facing, Math.Abs(facings), interpolatedFacings);

			return (GetSprite(start, frame, facing), rotation);
		}

		public Sprite GetShadow(int frame, WAngle facing)
		{
			return shadowStart >= 0 ? GetSprite(shadowStart, frame, facing) : null;
		}

		protected virtual Sprite GetSprite(int start, int frame, WAngle facing)
		{
			var f = GetFacingFrameOffset(facing);
			if (reverseFacings)
				f = (facings - f) % facings;

			var i = transpose ? frame % length * facings + f :
				f * stride + frame % length;

			var j = frames != null ? frames[i] : start + i;
			if (sprites[j] == null)
				throw new InvalidOperationException($"Attempted to query unloaded sprite from {image}.{Name} start={start} frame={frame} facing={facing}");

			return sprites[j];
		}

		protected virtual int GetFacingFrameOffset(WAngle facing)
		{
			return Util.IndexFacing(facing, facings);
		}

		public virtual float GetAlpha(int frame)
		{
			return alpha?[frame] ?? 1f;
		}
	}
}
