#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using Facebook.Yoga;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ListLayout : ILayout
	{
		ScrollPanelWidget widget;

		public ListLayout(ScrollPanelWidget w) { widget = w; }

		public void AdjustChild(Widget w)
		{
			if (widget.Children.Count == 0)
				widget.ContentHeight = 2 * widget.TopBottomSpacing - widget.ItemSpacing;

			if (w.Node.PositionType == YogaPositionType.Absolute)
			{
				w.Node.Top = widget.ContentHeight - widget.TopBottomSpacing + widget.ItemSpacing;
				w.Node.CalculateLayout();
			}

			if (!widget.CollapseHiddenChildren || w.IsVisible())
				widget.ContentHeight += (int)w.Node.LayoutHeight + widget.ItemSpacing;
		}

		public void AdjustChildren()
		{
			widget.ContentHeight = widget.TopBottomSpacing;
			foreach (var w in widget.Children)
			{
				if (w.Node.PositionType == YogaPositionType.Absolute)
				{
					w.Node.Top = widget.ContentHeight;
					w.Node.CalculateLayout();
				}

				if (!widget.CollapseHiddenChildren || w.IsVisible())
					widget.ContentHeight += (int)w.Node.LayoutHeight + widget.ItemSpacing;
			}

			// The loop above appended an extra widget.ItemSpacing after the last item.
			// Replace it with proper bottom spacing.
			widget.ContentHeight += widget.TopBottomSpacing - widget.ItemSpacing;
		}
	}
}
