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
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class RGBASpriteWidget : Widget
	{
		public Func<Sprite> GetSprite = () => null;

		public RGBASpriteWidget() { }

		protected RGBASpriteWidget(RGBASpriteWidget other)
			: base(other)
		{
			GetSprite = other.GetSprite;
		}

		public override Widget Clone() { return new RGBASpriteWidget(this); }

		public override void Draw()
		{
			var sprite = GetSprite();
			if (sprite != null)
				WidgetUtils.DrawSprite(sprite, RenderOrigin);
		}
	}
}
