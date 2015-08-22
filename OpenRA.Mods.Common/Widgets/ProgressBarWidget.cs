#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ProgressBarWidget : Widget
	{
		public int Percentage = 0;
		public bool Indeterminate = false;
		public int Maximum = 100;

		public Func<int> GetPercentage;
		public Func<int> GetMaximumPercentage;
		public Func<bool> IsIndeterminate;

		// Indeterminant bar properties
		float offset = 0f;
		float tickStep = 0.04f;

		public int CalculateOnePercentofMaximum(int maxValue)
		{
			if (maxValue > 0)
				Maximum = maxValue;

			return maxValue / GetMaximumPercentage();
		}

		public ProgressBarWidget()
		{
			GetMaximumPercentage = () => Maximum;
			GetPercentage = () =>
			{
				if (Percentage < Maximum)
					return Percentage;
				else
					return GetMaximumPercentage();
			};

			IsIndeterminate = () => Indeterminate;
		}

		protected ProgressBarWidget(ProgressBarWidget other)
			: base(other)
		{
			Percentage = other.Percentage;
			Maximum = other.Maximum;
			GetMaximumPercentage = other.GetMaximumPercentage;
			GetPercentage = other.GetPercentage;
			IsIndeterminate = other.IsIndeterminate;
		}

		public override void Draw()
		{
			var rb = RenderBounds;
			var percentage = GetPercentage();
			var maximum = GetMaximumPercentage();

			WidgetUtils.DrawPanel("progressbar-bg", rb);

			var barRect = wasIndeterminate ?
				new Rectangle(rb.X + 2 + (int)(0.75 * offset * (rb.Width - 4)), rb.Y + 2, (rb.Width - 4) / 4, rb.Height - 4) :
				new Rectangle(rb.X + 2, rb.Y + 2, percentage * (rb.Width - 4) / maximum, rb.Height - 4);

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
