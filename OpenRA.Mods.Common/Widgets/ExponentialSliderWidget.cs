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

namespace OpenRA.Mods.Common.Widgets
{
	public class ExponentialSliderWidget : SliderWidget
	{
		// Values are according to https://www.dr-lex.be/info-stuff/volumecontrols.html
		public double ExpA = 1.0e-3;
		public double ExpB = 6.908;

		public ExponentialSliderWidget()
			: base() { }

		public ExponentialSliderWidget(ExponentialSliderWidget other)
			: base(other) { }

		float ExpFromLinear(float x)
		{
			if (x <= 0)
				return 0;

			return (float)(ExpA * Math.Exp(ExpB * x)).Clamp(0.0, 1.0);
		}

		float LinearFromExp(float x)
		{
			return (float)(Math.Log(x / ExpA) / ExpB).Clamp(0.0, 1.0);
		}

		protected override float ValueFromPx(int x)
		{
			return MinimumValue + (MaximumValue - MinimumValue) * ExpFromLinear((x - 0.5f * RenderBounds.Height) / (RenderBounds.Width - RenderBounds.Height));
		}

		protected override int PxFromValue(float x)
		{
			return (int)(0.5f * RenderBounds.Height + (RenderBounds.Width - RenderBounds.Height) * LinearFromExp((x - MinimumValue) / (MaximumValue - MinimumValue)));
		}
	}
}
