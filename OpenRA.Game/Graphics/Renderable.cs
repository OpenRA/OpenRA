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
using System.Linq;

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
		IRenderable OffsetBy(WVec offset);
		void BeforeRender(WorldRenderer wr);
		void Render(WorldRenderer wr);
		void RenderDebugGeometry(WorldRenderer wr);
	}

	public struct SpriteRenderable : IRenderable
	{
		public static readonly IEnumerable<IRenderable> None = new IRenderable[0].AsEnumerable();

		readonly Sprite sprite;
		readonly WPos pos;
		readonly WVec offset;
		readonly int zOffset;
		readonly PaletteReference palette;
		readonly float scale;

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale)
		{
			this.sprite = sprite;
			this.pos = pos;
			this.offset = offset;
			this.zOffset = zOffset;
			this.palette = palette;
			this.scale = scale;
		}

		public SpriteRenderable(Sprite sprite, WPos pos, int zOffset, PaletteReference palette, float scale)
			: this(sprite, pos, WVec.Zero, zOffset, palette, scale) { }

		// Provided for legacy support only - Don't use for new things!
		public SpriteRenderable(Sprite sprite, float2 pos, PaletteReference palette, int z)
			: this(sprite, new PPos((int)pos.X, (int)pos.Y).ToWPos(0), z, palette, 1f) { }

		public WPos Pos { get { return pos + offset; } }
		public WVec Offset { get { return offset; } }
		public float Scale { get { return scale; } }
		public PaletteReference Palette { get { return palette; } }
		public int ZOffset { get { return zOffset; } }

		public IRenderable WithScale(float newScale) { return new SpriteRenderable(sprite, pos, offset, zOffset, palette, newScale); }
		public IRenderable WithPalette(PaletteReference newPalette) { return new SpriteRenderable(sprite, pos, offset, zOffset, newPalette, scale); }
		public IRenderable WithZOffset(int newOffset) { return new SpriteRenderable(sprite, pos, offset, newOffset, palette, scale); }
		public IRenderable OffsetBy(WVec vec) { return new SpriteRenderable(sprite, pos + vec, offset, zOffset, palette, scale); }

		float2 ScreenPosition(WorldRenderer wr)
		{
			return wr.ScreenPxPosition(pos) + wr.ScreenPxOffset(offset) - (0.5f*scale*sprite.size).ToInt2();
		}

		public void BeforeRender(WorldRenderer wr) {}
		public void Render(WorldRenderer wr)
		{
			sprite.DrawAt(ScreenPosition(wr), palette, scale);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var offset = ScreenPosition(wr) + sprite.offset;
			Game.Renderer.WorldLineRenderer.DrawRect(offset, offset + sprite.size, Color.Red);
		}
	}
}
