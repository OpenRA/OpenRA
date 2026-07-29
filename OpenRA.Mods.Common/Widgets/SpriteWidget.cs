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
using System.Numerics;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class SpriteWidget : Widget
	{
		public float Scale = 1f;
		public Func<float> GetScale;
		public string Palette = "chrome";
		public Func<string> GetPalette;
		public Func<Sprite> GetSprite;

		protected readonly WorldRenderer WorldRenderer;

		[ObjectCreator.UseCtor]
		public SpriteWidget(WorldRenderer worldRenderer)
		{
			GetPalette = () => Palette;
			GetScale = () => Scale;

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

		public override SpriteWidget Clone() { return new SpriteWidget(this); }

		Sprite cachedSprite = null;
		string cachedPalette = null;
		float cachedScale;
		PaletteReference pr;
		Vector2 offset = Vector2.Zero;

		public override void Draw()
		{
			var sprite = GetSprite();
			var palette = GetPalette();
			var scale = GetScale();

			if (sprite == null || palette == null)
				return;

			if (sprite != cachedSprite || scale != cachedScale)
			{
				offset = 0.5f * (RenderBounds.Size.ToVector2() - scale * sprite.Size.AsVector2());
				cachedSprite = sprite;
				cachedScale = scale;
			}

			if (palette != cachedPalette)
			{
				pr = WorldRenderer.Palette(palette);
				cachedPalette = palette;
			}

			Game.Renderer.EnableAntialiasingFilter();
			Game.Renderer.SpriteRenderer.DrawSprite(sprite, pr, RenderOrigin.ToVector3() + offset.AsVector3(), scale);
			Game.Renderer.DisableAntialiasingFilter();
		}
	}
}
