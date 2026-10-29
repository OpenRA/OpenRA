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

using System.Linq;
using System.Numerics;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Graphics
{
	public class DepthSpriteSequenceLoader : DefaultSpriteSequenceLoader
	{
		public override ISpriteSequence CreateSequence(
			ModData modData, string tileset, SpriteCache cache, string image, string sequence, MiniYaml data, MiniYaml defaults)
		{
			return new DepthSpriteSequence(cache, this, image, sequence, data, defaults);
		}
	}

	[Desc("A sprite sequence that can sample per-pixel depth data from a separate sprite.")]
	public class DepthSpriteSequence : DefaultSpriteSequence
	{
		[Desc("Name of the file containing the depth data sprite.")]
		protected static readonly SpriteSequenceField<string> DepthSprite = new(nameof(DepthSprite), null);

		[Desc("Frame index containing the depth data.")]
		protected static readonly SpriteSequenceField<int> DepthSpriteFrame = new(nameof(DepthSpriteFrame), 0);

		[Desc("X, Y offset to apply to the depth sprite.")]
		protected static readonly SpriteSequenceField<Vector2> DepthSpriteOffset = new(nameof(DepthSpriteOffset), Vector2.Zero);

		protected int? depthSpriteReservation;
		protected Vector2 depthSpriteOffset;
		protected Sprite depthSprite;

		public DepthSpriteSequence(SpriteCache cache, ISpriteSequenceLoader loader, string image, string sequence, MiniYaml data, MiniYaml defaults)
			: base(cache, loader, image, sequence, data, defaults)
		{
			var depthSpriteFilename = LoadField(DepthSprite, data, defaults, out var depthSpriteLocation);
			if (!string.IsNullOrEmpty(depthSpriteFilename))
				depthSpriteReservation = cache.ReserveSprites(depthSpriteFilename, [LoadField(DepthSpriteFrame, data, defaults)], depthSpriteLocation);

			depthSpriteOffset = LoadField(DepthSpriteOffset, data, defaults);
		}

		public override void ResolveSprites(SpriteCache cache)
		{
			if (bounds != null)
				return;

			if (depthSpriteReservation != null)
				depthSprite = cache.ResolveSprites(depthSpriteReservation.Value).First(s => s != null);

			base.ResolveSprites(cache);
		}

		protected override Sprite CreateSprite(SpriteReservation r, Sprite s)
		{
			var sprite = base.CreateSprite(r, s);
			if (depthSprite == null)
				return sprite;

			var cw = (depthSprite.Bounds.Left + depthSprite.Bounds.Right) / 2 + (int)(s.Offset.X + depthSpriteOffset.X);
			var ch = (depthSprite.Bounds.Top + depthSprite.Bounds.Bottom) / 2 + (int)(s.Offset.Y + depthSpriteOffset.Y);
			var w = s.Bounds.Width / 2;
			var h = s.Bounds.Height / 2;

			return new SpriteWithSecondaryData(sprite, depthSprite.Sheet, Rectangle.FromLTRB(cw - w, ch - h, cw + w, ch + h), depthSprite.Channel);
		}
	}
}
