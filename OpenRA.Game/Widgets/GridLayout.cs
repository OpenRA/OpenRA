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
using OpenRA.FileFormats;
using OpenRA.Support;

namespace OpenRA.Widgets
{
	public class GridLayout : ILayout
	{
		SpacingWidget widget;
		int2 pos;
		bool adjustAllEnabled = false;

		public GridLayout(SpacingWidget w, bool adjustAll = false)
		{
			widget = w;
			adjustAllEnabled = adjustAll;
		}

		public void AdjustChild(Widget w)
		{
			if (widget.Children.Count == 0)
			{
				widget.ContentHeight = widget.ItemSpacing;
				pos = new int2(widget.ItemSpacing, widget.ItemSpacing);
			}

			if (pos.X + widget.ItemSpacing + widget.ItemSpacingH + w.Bounds.Width > widget.GetContentWidth())
			{
				/* start a new row */
				pos.X = widget.ItemSpacing;
				pos.Y = widget.ContentHeight;
			}

			w.Bounds.X += pos.X;
			w.Bounds.Y += pos.Y;

			pos.X += w.Bounds.Width + widget.ItemSpacing + widget.ItemSpacingH;

			widget.ContentHeight = Math.Max(widget.ContentHeight, pos.Y + widget.ItemSpacing + w.Bounds.Height);
		}

		public void AdjustChildren()
		{
			if (adjustAllEnabled)
			{
				widget.ContentHeight = widget.ItemSpacing;
				pos = new int2(widget.ItemSpacing, widget.ItemSpacing);

				foreach (Widget w in widget.Children)
				{
					w.Bounds.X = Evaluator.Evaluate(w.X);
					w.Bounds.Y = Evaluator.Evaluate(w.Y);

					AdjustChild(w);
				}
			}
		}
	}
}

