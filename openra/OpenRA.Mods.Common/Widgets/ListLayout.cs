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

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ListLayout : ILayout
	{
		readonly ScrollPanelWidget widget;

		public ListLayout(ScrollPanelWidget w) { widget = w; }

		public void AdjustChild(Widget w)
		{
			if (widget.Children.Count == 0)
				widget.ContentHeight = 2 * widget.TopBottomSpacing - widget.ItemSpacing;

			w.Bounds.Y = widget.ContentHeight - widget.TopBottomSpacing + widget.ItemSpacing;
			if (!widget.CollapseHiddenChildren || w.IsVisible())
				widget.ContentHeight += w.Bounds.Height + widget.ItemSpacing;
		}

		public void AdjustChildren()
		{
			widget.ContentHeight = widget.TopBottomSpacing;
			foreach (var w in widget.Children)
			{
				w.Bounds.Y = widget.ContentHeight;
				if (!widget.CollapseHiddenChildren || w.IsVisible())
					widget.ContentHeight += w.Bounds.Height + widget.ItemSpacing;
			}

			// The loop above appended an extra widget.ItemSpacing after the last item.
			// Replace it with proper bottom spacing.
			widget.ContentHeight += widget.TopBottomSpacing - widget.ItemSpacing;
		}
	}
}
