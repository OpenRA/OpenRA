#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;

namespace OpenRA.Graphics
{
	public class RenderableComparer : IComparer<IRenderable>
	{
		WorldRenderer wr;
		public RenderableComparer(WorldRenderer wr)
		{
			this.wr = wr;
		}

		public int Compare(IRenderable x, IRenderable y)
		{
			var xOrder = wr.ScreenZPosition(x.Pos, x.ZOffset);
			var yOrder = wr.ScreenZPosition(y.Pos, y.ZOffset);
			return xOrder.CompareTo(yOrder);
		}
	}

	public interface IRenderable
	{
		WPos Pos { get; }
		float Scale { get; }
		PaletteReference Palette { get; }
		int ZOffset { get; }

		IRenderable WithScale(float newScale);
		IRenderable WithPalette(PaletteReference newPalette);
		IRenderable WithZOffset(int newOffset);
		IRenderable WithPos(WPos pos);
		void Render(WorldRenderer wr);
		WVec Size(WorldRenderer wr);
	}

	public struct SpriteRenderable : IRenderable
	{
		readonly Sprite sprite;
		readonly WPos pos;
		readonly int zOffset;
		readonly PaletteReference palette;
		readonly float scale;
		readonly float2 pxCenter;

		public SpriteRenderable(Sprite sprite, WPos pos, int zOffset, PaletteReference palette, float scale)
			: this(sprite, pos, zOffset, palette, scale, 0.5f*scale*sprite.size) {}

		public SpriteRenderable(Sprite sprite, WPos pos, int zOffset, PaletteReference palette, float scale, float2 pxCenter)
		{
			this.sprite = sprite;
			this.pos = pos;
			this.zOffset = zOffset;
			this.palette = palette;
			this.scale = scale;
			this.pxCenter = pxCenter;
		}

		// Provided for legacy support only - Don't use for new things!
		public SpriteRenderable(Sprite sprite, float2 pos, PaletteReference palette, int z)
			: this(sprite, new PPos((int)pos.X, (int)pos.Y).ToWPos(0), z, palette, 1f) { }

		public WPos Pos { get { return pos; } }
		public float Scale { get { return scale; } }
		public PaletteReference Palette { get { return palette; } }
		public int ZOffset { get { return zOffset; } }

		public IRenderable WithScale(float newScale) { return new SpriteRenderable(sprite, pos, zOffset, palette, newScale); }
		public IRenderable WithPalette(PaletteReference newPalette) { return new SpriteRenderable(sprite, pos, zOffset, newPalette, scale); }
		public IRenderable WithZOffset(int newOffset) { return new SpriteRenderable(sprite, pos, newOffset, palette, scale); }
		public IRenderable WithPos(WPos pos) { return new SpriteRenderable(sprite, pos, zOffset, palette, scale); }

		public void Render(WorldRenderer wr)
		{
			sprite.DrawAt(wr.ScreenPxPosition(pos) - pxCenter, palette.Index, scale);
		}

		public WVec Size(WorldRenderer wr)
		{
			var size = (scale*sprite.size).ToInt2();
			return new WVec(size.X, size.Y, size.Y);
		}
	}
}
