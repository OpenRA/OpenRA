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

using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class HueSliderWidget : SliderWidget
	{
		Sprite hueSprite;
		Sprite pickerSprite;

		public HueSliderWidget() { }
		public HueSliderWidget(HueSliderWidget other)
			: base(other) { }

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			var hueSheet = new Sheet(SheetType.BGRA, new Size(256, 1));

			var buffer = new byte[4 * 256];

			unsafe
			{
				fixed (byte* cc = &buffer[0])
				{
					var c = (int*)cc;
					for (var h = 0; h < 256; h++)
					{
						#pragma warning disable IDE0047
						(*(c + 0 * 256 + h)) = Color.FromAhsv(h / 255f, 1, 1).ToArgb();
						#pragma warning restore IDE0047
					}
				}
			}

			var rect = new Rectangle(0, 0, 256, 1);
			hueSprite = new Sprite(hueSheet, new Rectangle(0, 0, 256, 1), TextureChannel.RGBA);
			hueSheet.GetTexture().SetData(buffer, 256, 1);

			pickerSprite = ChromeProvider.GetImage("lobby-bits", "huepicker");
		}

		public override void Draw()
		{
			if (!IsVisible())
				return;

			var ro = RenderOrigin;
			var rb = RenderBounds;
			WidgetUtils.DrawSprite(hueSprite, ro, rb.Size);

			var pos = RenderOrigin + new int2(PxFromValue(Value).Clamp(0, rb.Width - 1) - (int)pickerSprite.Size.X / 2, (rb.Height - (int)pickerSprite.Size.Y) / 2);
			WidgetUtils.DrawSprite(pickerSprite, pos);
		}
	}
}
