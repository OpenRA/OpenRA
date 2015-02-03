#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
	public interface IRenderable
	{
		WPos Pos { get; }
		PaletteReference Palette { get; }
		int ZOffset { get; }
		bool IsDecoration { get; }

		IRenderable WithPalette(PaletteReference newPalette);
		IRenderable WithZOffset(int newOffset);
		IRenderable OffsetBy(WVec offset);
		IRenderable AsDecoration();

		IFinalizedRenderable PrepareRender(WorldRenderer wr);
	}

	public interface IFinalizedRenderable
	{
		void Render(WorldRenderer wr);
		void RenderDebugGeometry(WorldRenderer wr);
	}

	public struct SpriteRenderable : IRenderable, IFinalizedRenderable
	{
		public static readonly IEnumerable<IRenderable> None = new IRenderable[0].AsEnumerable();

		readonly Sprite sprite;
		readonly WPos pos;
		readonly WVec offset;
		readonly int zOffset;
		readonly PaletteReference palette;
		readonly float scale;
		readonly bool isDecoration;

		public SpriteRenderable(Sprite sprite, WPos pos, WVec offset, int zOffset, PaletteReference palette, float scale, bool isDecoration)
		{
			this.sprite = sprite;
			this.pos = pos;
			this.offset = offset;
			this.zOffset = zOffset;
			this.palette = palette;
			this.scale = scale;
			this.isDecoration = isDecoration;
		}

		public WPos Pos { get { return pos + offset; } }
		public WVec Offset { get { return offset; } }
		public PaletteReference Palette { get { return palette; } }
		public int ZOffset { get { return zOffset; } }
		public bool IsDecoration { get { return isDecoration; } }

		public IRenderable WithPalette(PaletteReference newPalette) { return new SpriteRenderable(sprite, pos, offset, zOffset, newPalette, scale, isDecoration); }
		public IRenderable WithZOffset(int newOffset) { return new SpriteRenderable(sprite, pos, offset, newOffset, palette, scale, isDecoration); }
		public IRenderable OffsetBy(WVec vec) { return new SpriteRenderable(sprite, pos + vec, offset, zOffset, palette, scale, isDecoration); }
		public IRenderable AsDecoration() { return new SpriteRenderable(sprite, pos, offset, zOffset, palette, scale, true); }

		float2 ScreenPosition(WorldRenderer wr)
		{
			return wr.ScreenPxPosition(pos) + wr.ScreenPxOffset(offset) - (0.5f * scale * sprite.Size).ToInt2();
		}

		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }
		public void Render(WorldRenderer wr)
		{
			Game.Renderer.WorldSpriteRenderer.DrawSprite(sprite, ScreenPosition(wr), palette, sprite.Size * scale);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var offset = ScreenPosition(wr) + sprite.Offset;
			Game.Renderer.WorldLineRenderer.DrawRect(offset, offset + sprite.Size, Color.Red);
		}
	}
}
