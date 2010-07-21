#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;

namespace OpenRA.Widgets
{
	public class SliderWidget : Widget
	{
		public event Action<float> OnChange;
		public Func<float> GetOffset;
		public float Offset = 0;
		public int Ticks = 0;
		public int TrackHeight = 5;

		int2 lastMouseLocation;
		bool isMoving = false;

		public SliderWidget()
			: base()
		{
			GetOffset = () => Offset;
			OnChange = x => Offset = x;
		}

		public SliderWidget(SliderWidget other)
			: base(other)
		{
			OnChange = other.OnChange;
			GetOffset = other.GetOffset;
			Offset = other.Offset;
			Ticks = other.Ticks;
			TrackHeight = other.TrackHeight;
			lastMouseLocation = other.lastMouseLocation;
			isMoving = other.isMoving;
		}

		public override bool HandleInputInner(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down && !TakeFocus(mi))
				return false;

			if (!Focused)
				return false;

			switch (mi.Event)
			{
				case MouseInputEvent.Up:
					{
						if (Focused)
						{
							isMoving = false;
							base.LoseFocus(mi);
						}
					}
					break;

				case MouseInputEvent.Down:
					{
						if (thumbRect.Contains(mi.Location.ToPoint()))
						{
							isMoving = true;
							lastMouseLocation = mi.Location;
						}
						else if (Ticks != 0)
						{
							var pos = GetOffset();

							// Offset slightly the direction we want to move so we don't get stuck on a tick
							var delta = 0.001;
							var targetTick = (float)((mi.Location.X > thumbRect.Right) ? Math.Ceiling((pos + delta) * (Ticks - 1))
																					   : Math.Floor((pos - delta) * (Ticks - 1)));
							OnChange(targetTick / (Ticks - 1));

							if (thumbRect.Contains(mi.Location.ToPoint()))
							{
								isMoving = true;
								lastMouseLocation = mi.Location;
							}
							return true;
						}
						else // No ticks; move to the mouse position
						{
							var thumb = thumbRect;
							var center = thumb.X + thumb.Width / 2;
							var newOffset = OffsetBy((mi.Location.X - center) * 1f / (RenderBounds.Width - thumb.Width));
							if (newOffset != GetOffset())
							{
								OnChange(newOffset);

								if (thumbRect.Contains(mi.Location.ToPoint()))
								{
									isMoving = true;
									lastMouseLocation = mi.Location;
								}
								return true;
							}
						}
					}
					break;

				case MouseInputEvent.Move:
					{
						if ((mi.Location.X != lastMouseLocation.X) && isMoving)
						{
							var newOffset = OffsetBy((mi.Location.X - lastMouseLocation.X) * 1f / (RenderBounds.Width - thumbRect.Width));
							if (newOffset != Offset)
							{
								lastMouseLocation = mi.Location;
								OnChange(newOffset);
							}
						}
					}
					break;
			}

			return thumbRect.Contains(mi.Location.X, mi.Location.Y);
		}

		float OffsetBy(float amount)
		{
			var centerPos = GetOffset() + amount;
			if (centerPos < 0) centerPos = 0;
			if (centerPos > 1) centerPos = 1;
			return centerPos;
		}

		public override Widget Clone() { return new SliderWidget(this); }

		Rectangle thumbRect
		{
			get
			{
				var width = RenderBounds.Height;
				var height = RenderBounds.Height;
				var origin = (int)((RenderBounds.X + width / 2) + GetOffset() * (RenderBounds.Width - width) - width / 2f);
				return new Rectangle(origin, RenderBounds.Y, width, height);
			}
		}

		public override void DrawInner(World world)
		{
			if (!IsVisible())
				return;

			var trackWidth = RenderBounds.Width - thumbRect.Width;
			var trackOrigin = RenderBounds.X + thumbRect.Width / 2;
			var trackRect = new Rectangle(trackOrigin - 1, RenderBounds.Y + (RenderBounds.Height - TrackHeight) / 2, trackWidth + 2, TrackHeight);

			// Tickmarks (hacked until we have real art)
			for (int i = 0; i < Ticks; i++)
			{
				var tickRect = new Rectangle(trackOrigin - 1 + (int)(i * trackWidth * 1f / (Ticks - 1)),
						  RenderBounds.Y + RenderBounds.Height / 2, 2, RenderBounds.Height / 2);
				WidgetUtils.DrawPanel("dialog2", tickRect);
			}
			// Track
			WidgetUtils.DrawPanel("dialog3", trackRect);

			// Thumb
			WidgetUtils.DrawPanel("dialog2", thumbRect);
		}
	}
}

