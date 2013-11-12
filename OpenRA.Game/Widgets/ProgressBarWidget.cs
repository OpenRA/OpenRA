#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;

namespace OpenRA.Widgets
{
	public class ProgressBarWidget : Widget
	{
		public int Percentage = 0;
		public bool Indeterminate = false;

		// Indeterminant bar properties
		float offset = 0f;
		float tickStep = 0.04f;

		public ProgressBarWidget() {}
		protected ProgressBarWidget(ProgressBarWidget widget)
			: base(widget)
		{
			Percentage = widget.Percentage;
		}

		public override void Draw()
		{
			var rb = RenderBounds;
			WidgetUtils.DrawPanel("progressbar-bg", rb);

			Rectangle barRect = Indeterminate ?
				new Rectangle(rb.X + 2 + (int)(0.75*offset*(rb.Width - 4)), rb.Y + 2, (rb.Width - 4) / 4, rb.Height - 4) :
				new Rectangle(rb.X + 2, rb.Y + 2, Percentage * (rb.Width - 4) / 100, rb.Height - 4);

			if (barRect.Width > 0)
				WidgetUtils.DrawPanel("progressbar-thumb", barRect);
		}

		public override void Tick()
		{
			if (Indeterminate)
			{
				offset += tickStep;
				offset = offset.Clamp(0f, 1f);
				if (offset == 0f || offset == 1f)
					tickStep *= -1;
			}
		}

		public void SetIndeterminate(bool value)
		{
			Indeterminate = value;
			offset = 0f;
		}

		public override Widget Clone() { return new ProgressBarWidget(this); }
	}
}