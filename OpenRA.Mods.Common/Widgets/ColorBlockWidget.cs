#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ColorBlockWidget : Widget
	{
		public Func<Color> GetColor;

		public ColorBlockWidget()
		{
			GetColor = () => Color.White;
		}

		protected ColorBlockWidget(ColorBlockWidget widget)
			: base(widget)
		{
			GetColor = widget.GetColor;
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
