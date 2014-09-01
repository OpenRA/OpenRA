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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Widgets
{
	public interface ILayout
	{
		void AdjustChild(Widget w);
		void AdjustChildren();
	}

	public class SpacingWidget : Widget
	{
		public int ContentHeight = 0;
		public Func<int> GetContentWidth;
		public ILayout Layout = null;
		public int ItemSpacing = 0;
		public int ItemSpacingH = 0;
		public bool CollapseHiddenChildren;

		public SpacingWidget() : base()
		{
			GetContentWidth = () => Bounds.Width;
		}

		public SpacingWidget(SpacingWidget widget) : base()
		{
			GetContentWidth = () => Bounds.Width;

			ContentHeight = widget.ContentHeight;
			ItemSpacing = widget.ItemSpacing;
			ItemSpacingH = widget.ItemSpacingH;

			CopyValuesFrom(widget);

			foreach (var child in widget.Children)
			{
				AddChild(child.Clone());
			}

			Layout = new GridLayout(this);
		}

		public override void AddChild(Widget child)
		{
			if (Layout != null)
			{
				Layout.AdjustChild(child);
			}

			base.AddChild(child);
		}

		public override Widget Clone()
		{
			return new SpacingWidget(this);
		}
	}
}
