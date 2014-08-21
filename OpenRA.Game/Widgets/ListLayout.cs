#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Widgets
{
	public class ListLayout : ILayout
	{
		ScrollPanelWidget widget;

		public ListLayout(ScrollPanelWidget w) { widget = w; }

		public void AdjustChild(Widget w)
		{
			if (widget.Children.Count == 0)
				widget.ContentHeight = widget.ItemSpacing;

			w.Bounds.Y = widget.ContentHeight;
			if (!widget.CollapseHiddenChildren || w.IsVisible())
				widget.ContentHeight += w.Bounds.Height + widget.ItemSpacing;
		}

		public void AdjustChildren()
		{
			widget.ContentHeight = widget.ItemSpacing;
			foreach (var w in widget.Children)
			{
				w.Bounds.Y = widget.ContentHeight;
				if (!widget.CollapseHiddenChildren || w.IsVisible())
					widget.ContentHeight += w.Bounds.Height + widget.ItemSpacing;
			}
		}
	}
}
