#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class GridLayout : ILayout
	{
		ScrollPanelWidget widget;
		int2 pos;

		public GridLayout(ScrollPanelWidget w) { widget = w; }

		public void AdjustChild(Widget w)
		{
			if (widget.Children.Count == 0)
			{
				widget.ContentHeight = 2 * widget.TopBottomSpacing;
				pos = new int2(widget.ItemSpacing, widget.TopBottomSpacing);
			}

			if (pos.X + (int)w.LayoutWidth + widget.ItemSpacing > (int)widget.LayoutWidth - widget.ScrollbarWidth)
			{
				/* start a new row */
				pos = new int2(widget.ItemSpacing, widget.ContentHeight - widget.TopBottomSpacing + widget.ItemSpacing);
			}

			w.Left = (int)w.LayoutX + pos.X;
			w.Top = (int)w.LayoutY + pos.Y;

			pos = pos.WithX(pos.X + (int)w.LayoutWidth + widget.ItemSpacing);

			widget.ContentHeight = Math.Max(widget.ContentHeight, pos.Y + (int)w.LayoutHeight + widget.TopBottomSpacing);
		}

		public void AdjustChildren() { }
	}
}
