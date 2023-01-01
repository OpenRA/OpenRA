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
	public class SliderWidget : InputWidget
	{
		public event Action<float> OnChange = _ => { };
		public int Ticks = 0;
		public int TrackHeight = 5;
		public string Thumb = "slider-thumb";
		public string Track = "slider-track";
		public float MinimumValue = 0;
		public float MaximumValue = 1;
		public float Value = 0;
		public Func<float> GetValue;

		protected bool isMoving = false;

		public SliderWidget()
		{
			GetValue = () => Value;
		}

		public SliderWidget(SliderWidget other)
			: base(other)
		{
			OnChange = other.OnChange;
			Ticks = other.Ticks;
			MinimumValue = other.MinimumValue;
			MaximumValue = other.MaximumValue;
			Value = other.Value;
			TrackHeight = other.TrackHeight;
			GetValue = other.GetValue;
		}

		public void UpdateValue(float newValue)
		{
			var oldValue = Value;
			Value = newValue.Clamp(MinimumValue, MaximumValue);
			if (oldValue != Value)
				OnChange(Value);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button != MouseButton.Left) return false;
			if (IsDisabled()) return false;
			if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi)) return false;
			if (!HasMouseFocus) return false;

			switch (mi.Event)
			{
				case MouseInputEvent.Up:
					isMoving = false;
					YieldMouseFocus(mi);
					break;

				case MouseInputEvent.Down:
					isMoving = true;
					/* TODO: handle snapping to ticks properly again */
					/* TODO: handle nudge via clicking outside the thumb */
					UpdateValue(ValueFromPx(mi.Location.X - RenderBounds.Left));
					break;

				case MouseInputEvent.Move:
					if (isMoving)
						UpdateValue(ValueFromPx(mi.Location.X - RenderBounds.Left));
					break;
			}

			return ThumbRect.Contains(mi.Location);
		}

		protected virtual float ValueFromPx(int x)
		{
			return MinimumValue + (MaximumValue - MinimumValue) * (x - 0.5f * RenderBounds.Height) / (RenderBounds.Width - RenderBounds.Height);
		}

		protected virtual int PxFromValue(float x)
		{
			return (int)(0.5f * RenderBounds.Height + (RenderBounds.Width - RenderBounds.Height) * (x - MinimumValue) / (MaximumValue - MinimumValue));
		}

		public override Widget Clone() { return new SliderWidget(this); }

		Rectangle ThumbRect
		{
			get
			{
				var thumbPos = PxFromValue(Value);
				var rb = RenderBounds;
				var width = rb.Height;
				var height = rb.Height;
				var origin = (int)(rb.X + thumbPos - width / 2f);
				return new Rectangle(origin, rb.Y, width, height);
			}
		}

		public override void Draw()
		{
			if (!IsVisible())
				return;

			UpdateValue(GetValue());

			var tr = ThumbRect;
			var rb = RenderBounds;
			var trackWidth = rb.Width - rb.Height;
			var trackOrigin = rb.X + rb.Height / 2;
			var trackRect = new Rectangle(trackOrigin - 1, rb.Y + (rb.Height - TrackHeight) / 2, trackWidth + 2, TrackHeight);

			// Tickmarks
			var tick = ChromeProvider.GetImage("slider", "tick");
			for (var i = 0; i < Ticks; i++)
			{
				var tickPos = new float2(
					trackOrigin + (i * (trackRect.Width - (int)tick.Size.X) / (Ticks - 1)) - tick.Size.X / 2,
					trackRect.Bottom);

				WidgetUtils.DrawSprite(tick, tickPos);
			}

			// Track
			WidgetUtils.DrawPanel(Track, trackRect);

			// Thumb
			var thumbHover = Ui.MouseOverWidget == this && tr.Contains(Viewport.LastMousePos);
			ButtonWidget.DrawBackground(Thumb, tr, IsDisabled(), isMoving, thumbHover, false);
		}
	}
}
