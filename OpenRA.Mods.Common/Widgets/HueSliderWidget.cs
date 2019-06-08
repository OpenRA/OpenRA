#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class HueSliderWidget : SliderWidget
	{
		Sprite hueSprite;

		public HueSliderWidget() { }
		public HueSliderWidget(HueSliderWidget other)
			: base(other) { }

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			var hueSheet = new Sheet(SheetType.BGRA, new Size(256, 1));
			hueSprite = new Sprite(hueSheet, new Rectangle(0, 0, 256, 1), TextureChannel.RGBA);

			var hueData = new uint[1, 256];
			for (var x = 0; x < 256; x++)
				hueData[0, x] = (uint)Color.FromAhsv(x / 255f, 1, 1).ToArgb();

			hueSheet.GetTexture().SetData(hueData);
		}

		public override void Draw()
		{
			if (!IsVisible())
				return;

			var ro = RenderOrigin;
			var rb = RenderBounds;
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(hueSprite, ro, new float2(rb.Size));

			var sprite = ChromeProvider.GetImage("lobby-bits", "huepicker");
			var pos = RenderOrigin + new int2(PxFromValue(Value).Clamp(0, rb.Width - 1) - sprite.Bounds.Width / 2, (rb.Height - sprite.Bounds.Height) / 2);
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, pos);
		}
	}
}
