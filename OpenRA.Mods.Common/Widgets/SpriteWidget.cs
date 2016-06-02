#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class SpriteWidget : Widget
	{
		public Func<float> GetScale = () => 1f;
		public string Palette = "chrome";
		public Func<string> GetPalette;
		public Func<Sprite> GetSprite;

		protected readonly WorldRenderer WorldRenderer;

		[ObjectCreator.UseCtor]
		public SpriteWidget(WorldRenderer worldRenderer)
		{
			GetPalette = () => Palette;

			WorldRenderer = worldRenderer;
		}

		protected SpriteWidget(SpriteWidget other)
			: base(other)
		{
			Palette = other.Palette;
			GetPalette = other.GetPalette;
			GetSprite = other.GetSprite;

			WorldRenderer = other.WorldRenderer;
		}

		public override Widget Clone() { return new SpriteWidget(this); }

		Sprite cachedSprite = null;
		string cachedPalette = null;
		float cachedScale;
		PaletteReference pr;
		float2 offset = float2.Zero;

		public override void Draw()
		{
			var sprite = GetSprite();
			var palette = GetPalette();
			var scale = GetScale();

			if (sprite == null || palette == null)
				return;

			if (sprite != cachedSprite)
			{
				offset = 0.5f * (new float2(RenderBounds.Size) - sprite.Size.XY);
				cachedSprite = sprite;
			}

			if (palette != cachedPalette)
			{
				pr = WorldRenderer.Palette(palette);
				cachedPalette = palette;
			}

			if (scale != cachedScale)
			{
				offset *= scale;
				cachedScale = scale;
			}

			var size = new float2(sprite.Size.X * scale, sprite.Size.Y * scale);
			Game.Renderer.SpriteRenderer.DrawSprite(sprite, RenderOrigin + offset, pr, size);
		}
	}
}
