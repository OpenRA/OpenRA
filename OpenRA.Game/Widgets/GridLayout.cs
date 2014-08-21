#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;

namespace OpenRA.Widgets
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
				widget.ContentHeight = widget.ItemSpacing;
				pos = new int2(widget.ItemSpacing, widget.ItemSpacing);
			}

			if (pos.X + widget.ItemSpacing + w.Bounds.Width > widget.Bounds.Width - widget.ScrollbarWidth)
			{
				/* start a new row */
				pos.X = widget.ItemSpacing;
				pos.Y = widget.ContentHeight;
			}

			w.Bounds.X += pos.X;
			w.Bounds.Y += pos.Y;

			pos.X += w.Bounds.Width + widget.ItemSpacing;

			widget.ContentHeight = Math.Max(widget.ContentHeight, pos.Y + widget.ItemSpacing + w.Bounds.Height);
		}

		public void AdjustChildren()
		{

		}
	}
}

