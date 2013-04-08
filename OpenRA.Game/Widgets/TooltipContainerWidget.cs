#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Widgets;
using System;

namespace OpenRA.Widgets
{
	public class TooltipContainerWidget : Widget
	{
		static readonly Action Nothing = () => {};
		public int2 CursorOffset = new int2(0, 20);
		public Action BeforeRender = Nothing;
		public int TooltipDelay = 5;
		Widget tooltip;

		public TooltipContainerWidget()
		{
			IsVisible = () => Viewport.TicksSinceLastMove >= TooltipDelay;
		}

		public void SetTooltip(string id, WidgetArgs args)
		{
			RemoveTooltip();
			tooltip = Ui.LoadWidget(id, this, new WidgetArgs(args) {{ "tooltipContainer", this }});
		}

		public void RemoveTooltip()
		{
			RemoveChildren();
			BeforeRender = Nothing;
		}

		public override void Draw() { BeforeRender(); }

		public override Rectangle GetEventBounds() { return Rectangle.Empty; }

		public override int2 ChildOrigin
		{
			get
			{
				var pos = Viewport.LastMousePos + CursorOffset;
				if (tooltip != null)
				{
					if (pos.X + tooltip.Bounds.Right > Game.viewport.Width)
						pos.X = Game.viewport.Width - tooltip.Bounds.Right;
				}

				return pos;
			}
		}

		public override string GetCursor(int2 pos) { return null; }
	}
}
