#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class HueSliderWidget : SliderWidget
	{
		Bitmap hueBitmap;
		Sprite hueSprite;

		public HueSliderWidget() : base() {}
		public HueSliderWidget(HueSliderWidget other) : base(other) {}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			hueBitmap = new Bitmap(256, 256);
			hueSprite = new Sprite(new Sheet(new Size(256, 256)), new Rectangle(0, 0, 256, 1), TextureChannel.Alpha);

			var bitmapData = hueBitmap.LockBits(hueBitmap.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			unsafe
			{
				int* c = (int*)bitmapData.Scan0;
				for (var h = 0; h < 256; h++)
					*(c + h) = HSLColor.FromHSV(h/255f, 1, 1).RGB.ToArgb();
			}
			hueBitmap.UnlockBits(bitmapData);

			hueSprite.sheet.Texture.SetData(hueBitmap);
		}

		public override void Draw()
		{
			if (!IsVisible())
				return;

			var ro = RenderOrigin;
			var rb = RenderBounds;
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(hueSprite, ro, new float2(rb.Size));

			var sprite = ChromeProvider.GetImage("lobby-bits", "huepicker");
			var pos = RenderOrigin + new int2(PxFromValue(Value).Clamp(0, rb.Width-1) - sprite.bounds.Width/2, (rb.Height-sprite.bounds.Height)/2);
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, pos);
		}
	}
}

