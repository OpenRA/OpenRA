#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class SpriteWidget : Widget
	{
		public string Palette = "chrome";
		public Func<string> GetPalette;
		public Func<Sprite> GetSprite;

		protected readonly WorldRenderer worldRenderer;

		[ObjectCreator.UseCtor]
		public SpriteWidget(WorldRenderer worldRenderer)
		{
			GetPalette = () => { return Palette; };

			this.worldRenderer = worldRenderer;
		}

		protected SpriteWidget(SpriteWidget other)
		{
			CopyOf(this, other);
			Palette = other.Palette;
			GetPalette = other.GetPalette;
			GetSprite = other.GetSprite;

			worldRenderer = other.worldRenderer;
		}

		public override Widget Clone() { return new SpriteWidget(this); }

		Sprite cachedSprite = null;
		string cachedPalette = null;
		PaletteReference pr;
		float2 offset = float2.Zero;

		public override void Draw()
		{
			var sprite = GetSprite();
			var palette = GetPalette();

			if (sprite == null || palette == null)
				return;

			if (sprite != cachedSprite)
			{
				offset = 0.5f * (new float2(RenderBounds.Size) - sprite.size);
				cachedSprite = sprite;
			}

			if (palette != cachedPalette)
			{
				pr = worldRenderer.Palette(palette);
				cachedPalette = palette;
			}

			Game.Renderer.SpriteRenderer.DrawSprite(sprite, RenderOrigin + offset, pr);
		}
	}
}
