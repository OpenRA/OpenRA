#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
	public struct UISpriteRenderable : IRenderable
	{
		readonly Sprite sprite;
		readonly int2 screenPos;
		readonly int zOffset;
		readonly PaletteReference palette;
		readonly float scale;

		public UISpriteRenderable(Sprite sprite, int2 screenPos, int zOffset, PaletteReference palette, float scale)
		{
			this.sprite = sprite;
			this.screenPos = screenPos;
			this.zOffset = zOffset;
			this.palette = palette;
			this.scale = scale;
		}

		// Does not exist in the world, so a world positions don't make sense
		public WPos Pos { get { return WPos.Zero; } }
		public WVec Offset { get { return WVec.Zero; } }
		public bool IsDecoration { get { return true; } }

		public float Scale { get { return scale; } }
		public PaletteReference Palette { get { return palette; } }
		public int ZOffset { get { return zOffset; } }

		public IRenderable WithScale(float newScale) { return new UISpriteRenderable(sprite, screenPos, zOffset, palette, newScale); }
		public IRenderable WithPalette(PaletteReference newPalette) { return new UISpriteRenderable(sprite, screenPos, zOffset, newPalette, scale); }
		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(WVec vec) { return this; }
		public IRenderable AsDecoration() { return this; }

		public void BeforeRender(WorldRenderer wr) {}
		public void Render(WorldRenderer wr)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(sprite, screenPos, palette, sprite.size * scale);
		}

		public void RenderDebugGeometry(WorldRenderer wr)
		{
			var offset = screenPos + sprite.offset;
			Game.Renderer.LineRenderer.DrawRect(offset, offset + sprite.size, Color.Red);
		}
	}
}
