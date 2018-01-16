#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ProgressBarWidget : Widget
	{
		public string Background = "progressbar-bg";
		public string Bar = "progressbar-thumb";
		public Size BarMargin = new Size(2, 2);

		public int Percentage = 0;
		public bool Indeterminate = false;

		public Func<int> GetPercentage;
		public Func<bool> IsIndeterminate;

		// Indeterminate bar properties
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
			WidgetUtils.DrawPanel(Background, rb);

			var minBarWidth = (int)(ChromeProvider.GetImage(Bar, "border-l").Size.X + ChromeProvider.GetImage(Bar, "border-r").Size.X);
			var maxBarWidth = rb.Width - BarMargin.Width * 2;
			var barWidth = wasIndeterminate ? maxBarWidth / 4 : percentage * maxBarWidth / 100;
			barWidth = Math.Max(barWidth, minBarWidth);

			var barOffset = wasIndeterminate ? (int)(0.75 * offset * maxBarWidth) : 0;
			var barRect = new Rectangle(rb.X + BarMargin.Width + barOffset, rb.Y + BarMargin.Height, barWidth, rb.Height - 2 * BarMargin.Height);
			WidgetUtils.DrawPanel(Bar, barRect);
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
