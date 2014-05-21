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
	public class RGBASpriteWidget : Widget
	{
		public Func<Sprite> GetSprite = () => null;

		public RGBASpriteWidget() { }

		protected RGBASpriteWidget(RGBASpriteWidget other)
		{
			CopyOf(this, other);
			GetSprite = other.GetSprite;
		}

		public override Widget Clone() { return new RGBASpriteWidget(this); }

		public override void Draw()
		{
			var sprite = GetSprite();
			if (sprite != null)
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, RenderOrigin);
		}
	}
}
