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
		Sheet hueSheet;

		public HueSliderWidget() { }
		public HueSliderWidget(HueSliderWidget other)
			: base(other) { }

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			hueSheet = new Sheet(SheetType.BGRA, new Size(256, 1));

			var buffer = new byte[4 * 256];

			unsafe
			{
				fixed (byte* cc = &buffer[0])
				{
					var c = (uint*)cc;
					for (var h = 0; h < 256; h++)
					{
						*(c + 0 * 256 + h) = Color.FromAhsv(h / 255f, 1, 1).ToArgb();
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

			// Draw TAB focus indicator when this hue slider has TAB focus
			if (HasTabFocus && !IsDisabled())
				DrawTabFocusIndicator(rb);

			WidgetUtils.DrawSprite(hueSprite, ro, rb.Size);

			var pos = RenderOrigin + new int2(PxFromValue(Value).Clamp(0, rb.Width - 1) - (int)pickerSprite.Size.X / 2, (rb.Height - (int)pickerSprite.Size.Y) / 2);
			WidgetUtils.DrawSprite(pickerSprite, pos);
		}

		// Draws a visual indicator around the hue slider when it has TAB focus
		static void DrawTabFocusIndicator(Rectangle rect)
		{
			if (!ChromeMetrics.TryGet<Color>("TabFocusColor", out var focusColor))
				focusColor = Color.FromArgb(128, 255, 255, 255);

			if (!ChromeMetrics.TryGet<int>("TabFocusWidth", out var focusWidth))
				focusWidth = 2;

			var outer = rect.InflateBy(focusWidth, focusWidth, focusWidth, focusWidth);

			// Top border
			WidgetUtils.FillRectWithColor(new Rectangle(outer.X, outer.Y, outer.Width, focusWidth), focusColor);

			// Bottom border
			WidgetUtils.FillRectWithColor(new Rectangle(outer.X, rect.Bottom, outer.Width, focusWidth), focusColor);

			// Left border
			WidgetUtils.FillRectWithColor(new Rectangle(outer.X, rect.Y, focusWidth, rect.Height), focusColor);

			// Right border
			WidgetUtils.FillRectWithColor(new Rectangle(rect.Right, rect.Y, focusWidth, rect.Height), focusColor);
		}

		// Override to use a smaller step for the hue slider (1/50 instead of 1/10)
		public override bool OnTabFocusKeyPress(KeyInput e)
		{
			if (IsDisabled())
				return false;

			// Use a finer step for hue selection (50 steps across the full range)
			var valueStep = (MaximumValue - MinimumValue) / 50f;

			switch (e.Key)
			{
				case Keycode.LEFT:
				case Keycode.DOWN:
					UpdateValue(Value - valueStep);
					return true;

				case Keycode.RIGHT:
				case Keycode.UP:
					UpdateValue(Value + valueStep);
					return true;

				case Keycode.HOME:
					UpdateValue(MinimumValue);
					return true;

				case Keycode.END:
					UpdateValue(MaximumValue);
					return true;
			}

			return false;
		}

		public override void Removed()
		{
			hueSheet?.Dispose();
			base.Removed();
		}
	}
}
