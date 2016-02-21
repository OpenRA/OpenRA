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

using System.Drawing;
using System.Drawing.Imaging;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class HueSliderWidget : SliderWidget
	{
		Sprite hueSprite;

		public HueSliderWidget() { }
		public HueSliderWidget(HueSliderWidget other) : base(other) { }

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			using (var hueBitmap = new Bitmap(256, 256))
			{
				var hueSheet = new Sheet(SheetType.BGRA, new Size(256, 256));
				hueSprite = new Sprite(hueSheet, new Rectangle(0, 0, 256, 1), TextureChannel.Alpha);

				var bitmapData = hueBitmap.LockBits(hueBitmap.Bounds(),
					ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
				unsafe
				{
					var c = (int*)bitmapData.Scan0;
					for (var h = 0; h < 256; h++)
						*(c + h) = HSLColor.FromHSV(h / 255f, 1, 1).RGB.ToArgb();
				}

				hueBitmap.UnlockBits(bitmapData);
				hueSheet.GetTexture().SetData(hueBitmap);
			}
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
