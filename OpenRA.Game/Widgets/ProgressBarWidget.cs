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

namespace OpenRA.Widgets
{
	public class ProgressBarWidget : Widget
	{
		public int Percentage = 0;
		public bool Indeterminate = false;

		public Func<int> GetPercentage;
		public Func<bool> IsIndeterminate;

		// Indeterminant bar properties
		float offset = 0f;
		float tickStep = 0.04f;

		public ProgressBarWidget()
		{
			GetPercentage = () => Percentage;
			IsIndeterminate = () => Indeterminate;
		}

		protected ProgressBarWidget(ProgressBarWidget other)
			: base(other)
		{
			Percentage = other.Percentage;
			GetPercentage = other.GetPercentage;
			IsIndeterminate = other.IsIndeterminate;
		}

		public override void Draw()
		{
			var rb = RenderBounds;
			var percentage = GetPercentage();
			WidgetUtils.DrawPanel("progressbar-bg", rb);

			Rectangle barRect = wasIndeterminate ?
				new Rectangle(rb.X + 2 + (int)(0.75*offset*(rb.Width - 4)), rb.Y + 2, (rb.Width - 4) / 4, rb.Height - 4) :
				new Rectangle(rb.X + 2, rb.Y + 2, percentage * (rb.Width - 4) / 100, rb.Height - 4);

			if (barRect.Width > 0)
				WidgetUtils.DrawPanel("progressbar-thumb", barRect);
		}

		bool wasIndeterminate;
		public override void Tick()
		{
			var indeterminate = IsIndeterminate();
			if (indeterminate != wasIndeterminate)
				offset = 0f;

			if (indeterminate)
			{
				offset += tickStep;
				offset = offset.Clamp(0f, 1f);
				if (offset == 0f || offset == 1f)
					tickStep *= -1;
			}

			wasIndeterminate = indeterminate;
		}

		public override Widget Clone() { return new ProgressBarWidget(this); }
	}
}