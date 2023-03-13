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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ColorMixerWidget : Widget
	{
		readonly Ruleset modRules;

		public string ClickSound = ChromeMetrics.Get<string>("ClickSound");

		public event Action OnChange = () => { };

		public float H { get; private set; }
		public float S { get; private set; }
		public float V { get; private set; }
		float minSat, maxSat, minVal, maxVal;

		Sheet mixerSheet;
		Sprite mixerSprite;
		bool isMoving;

		[ObjectCreator.UseCtor]
		public ColorMixerWidget(ModData modData)
		{
			modRules = modData.DefaultRules;
		}

		public ColorMixerWidget(ColorMixerWidget other)
			: base(other)
		{
			modRules = other.modRules;
			ClickSound = other.ClickSound;
			OnChange = other.OnChange;
			H = other.H;
			S = other.S;
			V = other.V;
			minSat = other.minSat;
			maxSat = other.maxSat;
			minVal = other.minVal;
			maxVal = other.maxVal;
		}

		public void SetColorLimits(float minSaturation, float maxSaturation, float minValue, float maxValue, float? newHue = null)
		{
			minSat = minSaturation;
			maxSat = maxSaturation;
			minVal = minValue;
			maxVal = maxValue;

			newHue ??= H;
			var buffer = new byte[4 * 256 * 256];
			unsafe
			{
				// Generate palette in HSV
				fixed (byte* cc = &buffer[0])
				{
					var c = (int*)cc;
					for (var v = 0; v < 256; v++)
					{
						for (var s = 0; s < 256; s++)
						{
							#pragma warning disable IDE0047
							(*(c + s * 256 + v)) = Color.FromAhsv(newHue.Value, 1 - s / 255f, v / 255f).ToArgb();
							#pragma warning restore IDE0047
						}
					}
				}
			}

			var rect = new Rectangle(
				(int)(255 * minVal),
				(int)(255 * (1 - maxSat)),
				(int)(255 * (maxVal - minVal)),
				(int)(255 * (maxSat - minSat)) + 1);

			mixerSprite = new Sprite(mixerSheet, rect, TextureChannel.RGBA);
			mixerSheet.GetTexture().SetData(buffer, 256, 256);
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			mixerSheet = new Sheet(SheetType.BGRA, new Size(256, 256));
			SetColorLimits(minSat, maxSat, minVal, maxVal);
		}

		public override void Draw()
		{
			WidgetUtils.DrawSprite(mixerSprite, RenderOrigin, RenderBounds.Size);

			var sprite = ChromeProvider.GetImage("lobby-bits", "colorpicker");
			var pos = RenderOrigin + PxFromValue() - new int2((int)sprite.Size.X, (int)sprite.Size.Y) / 2;
			WidgetUtils.FillEllipseWithColor(new Rectangle(pos.X + 1, pos.Y + 1, (int)sprite.Size.X - 2, (int)sprite.Size.Y - 2), Color);
			WidgetUtils.DrawSprite(sprite, pos);
		}

		void SetValueFromPx(int2 xy)
		{
			var rb = RenderBounds;
			var v = float2.Lerp(minVal, maxVal, xy.X * 1f / rb.Width);
			var s = float2.Lerp(minSat, maxSat, 1 - xy.Y * 1f / rb.Height);
			V = v.Clamp(minVal, maxVal);
			S = s.Clamp(minSat, maxSat);
		}

		int2 PxFromValue()
		{
			var rb = RenderBounds;
			var x = rb.Width * (V - minVal) / (maxVal - minVal);
			var y = rb.Height * (1 - (S - minSat) / (maxSat - minSat));
			return new int2((int)x.Clamp(0, rb.Width), (int)y.Clamp(0, rb.Height));
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				return false;
			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
				return false;
			if (!HasMouseFocus)
				return false;

			switch (mi.Event)
			{
				case MouseInputEvent.Up:
					isMoving = false;
					YieldMouseFocus(mi);
					break;

				case MouseInputEvent.Down:
					isMoving = true;
					SetValueFromPx(mi.Location - RenderOrigin);
					OnChange();

					Game.Sound.PlayNotification(modRules, null, "Sounds", ClickSound, null);
					break;

				case MouseInputEvent.Move:
					if (isMoving)
					{
						SetValueFromPx(mi.Location - RenderOrigin);
						OnChange();
					}

					break;
			}

			return true;
		}

		public Color Color => Color.FromAhsv(H, S, V);

		/// <summary>
		/// Set the color picker to nearest valid color to the given value.
		/// The saturation and brightness may be adjusted.
		/// </summary>
		public void Set(Color color)
		{
			var (_, h, s, v) = color.ToAhsv();

			if (H != h || S != s || V != v)
			{
				H = h;
				S = s.Clamp(minSat, maxSat);
				V = v.Clamp(minVal, maxVal);
				OnChange();
			}
		}
	}
}
