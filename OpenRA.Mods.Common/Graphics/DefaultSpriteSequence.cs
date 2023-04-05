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
		public readonly int BgraSheetSize = 2048;
		public readonly int IndexedSheetSize = 2048;

		static readonly MiniYaml NoData = new(null);

		public DefaultSpriteSequenceLoader(ModData modData)
		{
			var metadata = modData.Manifest.Get<SpriteSequenceFormat>().Metadata;
			if (metadata.TryGetValue("BgraSheetSize", out var yaml))
				BgraSheetSize = FieldLoader.GetValue<int>("BgraSheetSize", yaml.Value);

			if (metadata.TryGetValue("IndexedSheetSize", out yaml))
				IndexedSheetSize = FieldLoader.GetValue<int>("IndexedSheetSize", yaml.Value);
		}

		public virtual ISpriteSequence CreateSequence(ModData modData, string tileset, SpriteCache cache, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			return new DefaultSpriteSequence(cache, this, image, sequence, data, defaults);
		}

		int ISpriteSequenceLoader.BgraSheetSize => BgraSheetSize;
		int ISpriteSequenceLoader.IndexedSheetSize => IndexedSheetSize;

		IReadOnlyDictionary<string, ISpriteSequence> ISpriteSequenceLoader.ParseSequences(ModData modData, string tileset, SpriteCache cache, MiniYamlNode imageNode)
		{
			var sequences = new Dictionary<string, ISpriteSequence>();
			var node = imageNode.Value.Nodes.SingleOrDefault(n => n.Key == "Defaults");
			var defaults = node?.Value ?? NoData;
			imageNode.Value.Nodes.Remove(node);

			foreach (var sequenceNode in imageNode.Value.Nodes)
			{
				using (new Support.PerfTimer($"new Sequence(\"{imageNode.Key}\")", 20))
				{
					try
					{
						var sequence = CreateSequence(modData, tileset, cache, imageNode.Key, sequenceNode.Key, sequenceNode.Value, defaults);
						((DefaultSpriteSequence)sequence).ReserveSprites(modData, tileset, cache, sequenceNode.Value, defaults);
						sequences.Add(sequenceNode.Key, sequence);
					}
					catch (Exception e)
					{
						throw new InvalidDataException($"Failed to parse sequences for {imageNode.Key}.{sequenceNode.Key} at {imageNode.Value.Nodes[0].Location}:\n{e}");
					}
				}
			}

			return new ReadOnlyDictionary<string, ISpriteSequence>(sequences);
		}
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
		protected class SpriteReservation
		{
			public int Token;
			public float3 Offset;
			public bool FlipX;
			public bool FlipY;
			public float ZRamp;
			public BlendMode BlendMode;
			public int[] Frames;
		}

		protected readonly struct ReservationInfo
		{
			public readonly string Filename;
			public readonly List<int> LoadFrames;
			public readonly int[] Frames;
			public readonly MiniYamlNode.SourceLocation Location;

			public ReservationInfo(string filename, int[] loadFrames, int[] frames, MiniYamlNode.SourceLocation location)
				: this(filename, loadFrames?.ToList(), frames, location) { }

			public ReservationInfo(string filename, List<int> loadFrames, int[] frames, MiniYamlNode.SourceLocation location)
			{
				Filename = filename;
				LoadFrames = loadFrames;
				Frames = frames;
				Location = location;
			}
		}

		[Desc("File name of the sprite to use for this sequence.")]
		protected static readonly SpriteSequenceField<string> Filename = new(nameof(Filename), null);

		[Desc("Frame index to start from.")]
		protected static readonly SpriteSequenceField<int> Start = new(nameof(Start), 0);

		[Desc("Number of frames to use. Does not have to be the total amount the sprite sheet has.")]
		protected static readonly SpriteSequenceField<int> Length = new(nameof(Length), 1);

		[Desc("Overrides Length if a different number of frames is defined between facings.")]
		protected static readonly SpriteSequenceField<int> Stride = new(nameof(Stride), -1);

		[Desc("The number of facings that are provided by sprite frames. Use negative values to rotate counter-clockwise.")]
		protected static readonly SpriteSequenceField<int> Facings = new(nameof(Facings), 1);

		[Desc("The total number of facings for the sequence. If >Facings, the closest facing sprite will be rotated to match. Use negative values to rotate counter-clockwise.")]
		protected static readonly SpriteSequenceField<int?> InterpolatedFacings = new(nameof(InterpolatedFacings), null);

		[Desc("Time (in milliseconds at default game speed) to wait until playing the next frame in the animation.")]
		protected static readonly SpriteSequenceField<int> Tick = new(nameof(Tick), 40);

		[Desc("Value controlling the Z-order. A higher values means rendering on top of other sprites at the same position. " +
		      "Use power of 2 values to avoid glitches.")]
		protected static readonly SpriteSequenceField<WDist> ZOffset = new(nameof(ZOffset), WDist.Zero);

		[Desc("Additional sprite depth Z offset to apply as a function of sprite Y (0: vertical, 1: flat on terrain)")]
		protected static readonly SpriteSequenceField<int> ZRamp = new(nameof(ZRamp), 0);

		[Desc("If the shadow is not part of the sprite, but baked into the same sprite sheet at a fixed offset, " +
			"set this to the frame index where it starts.")]
		protected static readonly SpriteSequenceField<int> ShadowStart = new(nameof(ShadowStart), -1);

		[Desc("Set Z-Offset for the separate shadow. Used by the later Westwood 2.5D titles.")]
		protected static readonly SpriteSequenceField<WDist> ShadowZOffset = new(nameof(ShadowZOffset), new WDist(-5));

		[Desc("The individual frames to play instead of going through them sequentially from the `Start`.")]
		protected static readonly SpriteSequenceField<int[]> Frames = new(nameof(Frames), null);

		[Desc("Don't apply terrain lighting or colored overlays.")]
		protected static readonly SpriteSequenceField<bool> IgnoreWorldTint = new(nameof(IgnoreWorldTint), false);

		[Desc("Adjusts the rendered size of the sprite")]
		protected static readonly SpriteSequenceField<float> Scale = new(nameof(Scale), 1);

		[Desc("Play the sprite sequence back and forth.")]
		protected static readonly SpriteSequenceField<bool> Reverses = new(nameof(Reverses), false);

		[Desc("Support a frame order where each animation step is split per each direction.")]
		protected static readonly SpriteSequenceField<bool> Transpose = new(nameof(Transpose), false);

		[Desc("Mirror on the X axis.")]
		protected static readonly SpriteSequenceField<bool> FlipX = new(nameof(FlipX), false);

		[Desc("Mirror on the Y axis.")]
		protected static readonly SpriteSequenceField<bool> FlipY = new(nameof(FlipY), false);

		[Desc("Change the position in-game on X, Y, Z.")]
		protected static readonly SpriteSequenceField<float3> Offset = new(nameof(Offset), float3.Zero);

		[Desc("Apply an OpenGL/Photoshop inspired blend mode.")]
		protected static readonly SpriteSequenceField<BlendMode> BlendMode = new(nameof(BlendMode), OpenRA.BlendMode.Alpha);

		[Desc("Create a virtual sprite file by concatenating one or more frames from multiple files, with optional transformations applied. " +
			"All defined frames will be loaded into memory, even if unused, so use this property with care.")]
		protected static readonly SpriteSequenceField<MiniYaml> Combine = new(nameof(Combine), null);

		[Desc("Sets transparency - use one value to set for all frames or provide a value for each frame.")]
		protected static readonly SpriteSequenceField<float[]> Alpha = new(nameof(Alpha), null);

		[Desc("Fade the animation from fully opaque on the first frame to fully transparent after the last frame.")]
		protected static readonly SpriteSequenceField<bool> AlphaFade = new(nameof(AlphaFade), false);

		[Desc("Name of the file containing the depth data sprite.")]
		protected static readonly SpriteSequenceField<string> DepthSprite = new(nameof(DepthSprite), null);

		[Desc("Frame index containing the depth data.")]
		protected static readonly SpriteSequenceField<int> DepthSpriteFrame = new(nameof(DepthSpriteFrame), 0);

		[Desc("X, Y offset to apply to the depth sprite.")]
		protected static readonly SpriteSequenceField<float2> DepthSpriteOffset = new(nameof(DepthSpriteOffset), float2.Zero);

		protected static readonly MiniYaml NoData = new(null);
		protected readonly ISpriteSequenceLoader Loader;

		protected string image;
		protected List<SpriteReservation> spritesToLoad = new();
		protected Sprite[] sprites;
		protected Sprite[] shadowSprites;
		protected bool reverseFacings;
		protected bool reverses;

		protected int start;
		protected int shadowStart;
		protected int? length;
		protected int? stride;
		protected bool transpose;

		protected int facings;
		protected int? interpolatedFacings;
		protected int tick;
		protected int zOffset;
		protected int shadowZOffset;
		protected bool ignoreWorldTint;
		protected float scale;
		protected float[] alpha;
		protected bool alphaFade;
		protected Rectangle? bounds;

		protected int? depthSpriteReservation;
		protected float2 depthSpriteOffset;

		protected void ThrowIfUnresolved()
		{
			if (bounds == null)
				throw new InvalidOperationException($"Unable to query unresolved sequence {image}.{Name}.");
		}

		int ISpriteSequence.Length
		{
			get
			{
				ThrowIfUnresolved();
				return length.Value;
			}
		}

		int ISpriteSequence.Facings => interpolatedFacings ?? facings;
		int ISpriteSequence.Tick => tick;
		int ISpriteSequence.ZOffset => zOffset;
		int ISpriteSequence.ShadowZOffset => shadowZOffset;
		bool ISpriteSequence.IgnoreWorldTint => ignoreWorldTint;
		float ISpriteSequence.Scale => GetScale();
		Rectangle ISpriteSequence.Bounds
		{
			get
			{
				ThrowIfUnresolved();
				return bounds.Value;
			}
		}

		public string Name { get; }

		protected static T LoadField<T>(string key, T fallback, MiniYaml data, MiniYaml defaults = null)
		{
			var node = data.Nodes.FirstOrDefault(n => n.Key == key) ?? defaults?.Nodes.FirstOrDefault(n => n.Key == key);
			if (node == null)
				return fallback;

			return FieldLoader.GetValue<T>(key, node.Value.Value);
		}

		protected static T LoadField<T>(SpriteSequenceField<T> field, MiniYaml data, MiniYaml defaults = null)
		{
			return LoadField(field, data, defaults, out _);
		}

		protected static T LoadField<T>(SpriteSequenceField<T> field, MiniYaml data, MiniYaml defaults, out MiniYamlNode.SourceLocation location)
		{
			var node = data.Nodes.FirstOrDefault(n => n.Key == field.Key) ?? defaults?.Nodes.FirstOrDefault(n => n.Key == field.Key);
			if (node == null)
			{
				location = default;
				return field.DefaultValue;
			}

			location = node.Location;
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

		protected static List<int> CalculateFrameIndices(int start, int? length, int stride, int facings, int[] frames, bool transpose, bool reverseFacings, int shadowStart)
		{
			// Request all frames
			if (length == null)
				return null;

			// Only request the subset of frames that we actually need
			var usedFrames = new List<int>();
			for (var facing = 0; facing < facings; facing++)
			{
				var facingInner = reverseFacings ? (facings - facing) % facings : facing;
				for (var frame = 0; frame < length.Value; frame++)
				{
					var i = transpose ? frame % length.Value * facings + facingInner :
						facingInner * stride + frame % length.Value;

					usedFrames.Add(frames?[i] ?? start + i);
				}
			}

			if (shadowStart >= 0)
				usedFrames.AddRange(usedFrames.ToList().Select(i => i + shadowStart - start));

			return usedFrames;
		}

		protected virtual IEnumerable<ReservationInfo> ParseFilenames(ModData modData, string tileset, int[] frames, MiniYaml data, MiniYaml defaults)
		{
			var filename = LoadField(Filename, data, defaults, out var location);

			var loadFrames = CalculateFrameIndices(start, length, stride ?? length ?? 0, facings, frames, transpose, reverseFacings, shadowStart);
			yield return new ReservationInfo(filename, loadFrames, frames, location);
		}

		protected virtual IEnumerable<ReservationInfo> ParseCombineFilenames(ModData modData, string tileset, int[] frames, MiniYaml data)
		{
			var filename = LoadField(Filename, data, null, out var location);
			if (frames == null)
			{
				if (LoadField<string>(Length.Key, null, data) != "*")
				{
					var subStart = LoadField("Start", 0, data);
					var subLength = LoadField("Length", 1, data);
					frames = Exts.MakeArray(subLength, i => subStart + i);
				}
			}

			yield return new ReservationInfo(filename, frames, frames, location);
		}

		public DefaultSpriteSequence(SpriteCache cache, ISpriteSequenceLoader loader, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			this.image = image;
			Name = sequence;
			Loader = loader;

			start = LoadField(Start, data, defaults);

			length = null;
			var lengthLocation = default(MiniYamlNode.SourceLocation);
			if (LoadField<string>(Length.Key, null, data, defaults) != "*")
				length = LoadField(Length, data, defaults, out lengthLocation);

			stride = LoadField(Stride.Key, length, data, defaults);
			facings = LoadField(Facings, data, defaults, out var facingsLocation);
			interpolatedFacings = LoadField(InterpolatedFacings, data, defaults, out var interpolatedFacingsLocation);

			tick = LoadField(Tick, data, defaults);
			zOffset = LoadField(ZOffset, data, defaults).Length;

			shadowStart = LoadField(ShadowStart, data, defaults);
			shadowZOffset = LoadField(ShadowZOffset, data, defaults).Length;

			ignoreWorldTint = LoadField(IgnoreWorldTint, data, defaults);
			scale = LoadField(Scale, data, defaults);

			reverses = LoadField(Reverses, data, defaults);
			transpose = LoadField(Transpose, data, defaults);
			alpha = LoadField(Alpha, data, defaults);
			alphaFade = LoadField(AlphaFade, data, defaults, out var alphaFadeLocation);

			var depthSprite = LoadField(DepthSprite, data, defaults, out var depthSpriteLocation);
			if (!string.IsNullOrEmpty(depthSprite))
				depthSpriteReservation = cache.ReserveSprites(depthSprite, new[] { LoadField(DepthSpriteFrame, data, defaults) }, depthSpriteLocation);

			depthSpriteOffset = LoadField(DepthSpriteOffset, data, defaults);

			if (facings < 0)
			{
				reverseFacings = true;
				facings = -facings;
			}

			if (interpolatedFacings != null && (interpolatedFacings < 2 || interpolatedFacings <= facings || interpolatedFacings > 1024 || !Exts.IsPowerOf2(interpolatedFacings.Value)))
				throw new YamlException($"{interpolatedFacingsLocation}: {InterpolatedFacings.Key} must be greater than {Facings.Key}, within the range of 2 to 1024, and a power of 2.");

			if (length != null && length <= 0)
				throw new YamlException($"{lengthLocation}: {Length.Key} must be positive.");

			if (length == null && facings > 1)
				throw new YamlException($"{facingsLocation}: {Facings.Key} cannot be used with {Length.Key}: *.");

			if (alphaFade && alpha != null)
				throw new YamlException($"{alphaFadeLocation}: {AlphaFade.Key} cannot be used with {Alpha.Key}.");
		}

		public virtual void ReserveSprites(ModData modData, string tileset, SpriteCache cache, MiniYaml data, MiniYaml defaults)
		{
			var frames = LoadField(Frames, data, defaults);
			var flipX = LoadField(FlipX, data, defaults);
			var flipY = LoadField(FlipY, data, defaults);
			var zRamp = LoadField(ZRamp, data, defaults);
			var offset = LoadField(Offset, data, defaults);
			var blendMode = LoadField(BlendMode, data, defaults);

			var combineNode = data.Nodes.FirstOrDefault(n => n.Key == Combine.Key);
			if (combineNode != null)
			{
				for (var i = 0; i < combineNode.Value.Nodes.Count; i++)
				{
					var subData = combineNode.Value.Nodes[i].Value;
					var subOffset = LoadField(Offset, subData, NoData);
					var subFlipX = LoadField(FlipX, subData, NoData);
					var subFlipY = LoadField(FlipY, subData, NoData);
					var subFrames = LoadField(Frames, data);

					foreach (var f in ParseCombineFilenames(modData, tileset, subFrames, subData))
					{
						spritesToLoad.Add(new SpriteReservation
						{
							Token = cache.ReserveSprites(f.Filename, f.LoadFrames, f.Location),
							Offset = subOffset + offset,
							FlipX = subFlipX ^ flipX,
							FlipY = subFlipY ^ flipY,
							BlendMode = blendMode,
							ZRamp = zRamp,
							Frames = f.Frames
						});
					}
				}
			}
			else
			{
				foreach (var f in ParseFilenames(modData, tileset, frames, data, defaults))
				{
					spritesToLoad.Add(new SpriteReservation
					{
						Token = cache.ReserveSprites(f.Filename, f.LoadFrames, f.Location),
						Offset = offset,
						FlipX = flipX,
						FlipY = flipY,
						BlendMode = blendMode,
						ZRamp = zRamp,
						Frames = f.Frames,
					});
				}
			}
		}

		public virtual void ResolveSprites(SpriteCache cache)
		{
			if (bounds != null)
				return;

			Sprite depthSprite = null;
			if (depthSpriteReservation != null)
				depthSprite = cache.ResolveSprites(depthSpriteReservation.Value).First(s => s != null);

			var allSprites = spritesToLoad.SelectMany(r =>
			{
				var resolved = cache.ResolveSprites(r.Token);
				if (r.Frames != null)
					resolved = r.Frames.Select(f => resolved[f]).ToArray();

				return resolved.Select(s =>
				{
					if (s == null)
						return null;

					var dx = r.Offset.X + (r.FlipX ? -s.Offset.X : s.Offset.X);
					var dy = r.Offset.Y + (r.FlipY ? -s.Offset.Y : s.Offset.Y);
					var dz = r.Offset.Z + s.Offset.Z + r.ZRamp * dy;
					var sprite = new Sprite(s.Sheet, FlipRectangle(s.Bounds, r.FlipX, r.FlipY), r.ZRamp, new float3(dx, dy, dz), s.Channel, r.BlendMode);
					if (depthSprite == null)
						return sprite;

					var cw = (depthSprite.Bounds.Left + depthSprite.Bounds.Right) / 2 + (int)(s.Offset.X + depthSpriteOffset.X);
					var ch = (depthSprite.Bounds.Top + depthSprite.Bounds.Bottom) / 2 + (int)(s.Offset.Y + depthSpriteOffset.Y);
					var w = s.Bounds.Width / 2;
					var h = s.Bounds.Height / 2;

					return new SpriteWithSecondaryData(sprite, depthSprite.Sheet, Rectangle.FromLTRB(cw - w, ch - h, cw + w, ch + h), depthSprite.Channel);
				});
			}).ToArray();

			length ??= allSprites.Length - start;

			if (alpha != null)
			{
				if (alpha.Length == 1)
					alpha = Exts.MakeArray(length.Value, _ => alpha[0]);
				else if (alpha.Length != length.Value)
					throw new YamlException($"Sequence {image}.{Name} must define either 1 or {length.Value} Alpha values.");
			}
			else if (alphaFade)
				alpha = Exts.MakeArray(length.Value, i => float2.Lerp(1f, 0f, i / (length.Value - 1f)));

			// Reindex sprites to order facings anti-clockwise and remove unused frames
			var index = CalculateFrameIndices(start, length.Value, stride ?? length.Value, facings, null, transpose, reverseFacings, -1);
			if (reverses)
			{
				index.AddRange(index.Skip(1).Take(length.Value - 2).Reverse());
				length = 2 * length - 2;
			}

			if (!index.Any())
				throw new YamlException($"Sequence {image}.{Name} does not define any frames.");

			var minIndex = index.Min();
			var maxIndex = index.Max();
			if (minIndex < 0 || maxIndex >= allSprites.Length)
				throw new YamlException($"Sequence {image}.{Name} uses frames between {minIndex}..{maxIndex}, but only 0..{allSprites.Length - 1} exist.");

			sprites = index.Select(f => allSprites[f]).ToArray();
			if (shadowStart >= 0)
				shadowSprites = index.Select(f => allSprites[f - start + shadowStart]).ToArray();

			bounds = sprites.Concat(shadowSprites ?? Enumerable.Empty<Sprite>()).Select(OffsetSpriteBounds).Union();
		}

		protected static Rectangle OffsetSpriteBounds(Sprite sprite)
		{
			if (sprite == null || sprite.Bounds.IsEmpty)
				return Rectangle.Empty;

			return new Rectangle(
				(int)(sprite.Offset.X - sprite.Size.X / 2),
				(int)(sprite.Offset.Y - sprite.Size.Y / 2),
				sprite.Bounds.Width, sprite.Bounds.Height);
		}

		public Sprite GetSprite(int frame)
		{
			return GetSprite(frame, WAngle.Zero);
		}

		public (Sprite, WAngle) GetSpriteWithRotation(int frame, WAngle facing)
		{
			var rotation = WAngle.Zero;
			if (interpolatedFacings != null)
				rotation = Util.GetInterpolatedFacingRotation(facing, Math.Abs(facings), interpolatedFacings.Value);

			return (GetSprite(frame, facing), rotation);
		}

		public Sprite GetShadow(int frame, WAngle facing)
		{
			if (shadowSprites == null)
				return null;

			var index = GetFacingFrameOffset(facing) * length.Value + frame % length.Value;
			var sprite = shadowSprites[index];
			if (sprite == null)
				throw new InvalidOperationException($"Attempted to query unloaded shadow sprite from {image}.{Name} frame={frame} facing={facing}.");

			return sprite;
		}

		public virtual Sprite GetSprite(int frame, WAngle facing)
		{
			ThrowIfUnresolved();
			var index = GetFacingFrameOffset(facing) * length.Value + frame % length.Value;
			var sprite = sprites[index];
			if (sprite == null)
				throw new InvalidOperationException($"Attempted to query unloaded sprite from {image}.{Name} frame={frame} facing={facing}.");

			return sprite;
		}

		protected virtual int GetFacingFrameOffset(WAngle facing)
		{
			return Util.IndexFacing(facing, facings);
		}

		public virtual float GetAlpha(int frame)
		{
			return alpha?[frame] ?? 1f;
		}

		protected virtual float GetScale()
		{
			return scale;
		}
	}
}
