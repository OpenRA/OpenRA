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
	public class ColorBlockWidget : Widget
	{
		public Func<Color> GetColor;

		public ColorBlockWidget()
		{
			GetColor = () => Color.White;
		}

		protected ColorBlockWidget(ColorBlockWidget other)
		{
			CopyOf(this, other);
			GetColor = other.GetColor;
		}

		public override Widget Clone()
		{
			return new ColorBlockWidget(this);
		}

		public override void Draw()
		{
			WidgetUtils.FillRectWithColor(RenderBounds, GetColor());
		}
	}
}
