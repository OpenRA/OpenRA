#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class TooltipContainerWidget : Widget
	{
		static readonly Action Nothing = () => { };

		public int2 CursorOffset = new int2(0, 20);
		public int BottomEdgeYOffset = -5;

		public Action BeforeRender = Nothing;
		public int TooltipDelayMilliseconds = 200;
		Widget tooltip;

		public TooltipContainerWidget()
		{
			IsVisible = () => Game.RunTime > Viewport.LastMoveRunTime + TooltipDelayMilliseconds;
		}

		public void SetTooltip(string id, WidgetArgs args)
		{
			RemoveTooltip();
			tooltip = Ui.LoadWidget(id, this, new WidgetArgs(args) { { "tooltipContainer", this } });
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
				var pos = Viewport.LastMousePos + (CursorProvider.CursorViewportZoomed ? CursorOffset * 2 : CursorOffset);
				if (tooltip != null)
				{
					// If the tooltip overlaps the right edge of the screen, move it left until it fits
					if (pos.X + tooltip.Bounds.Right > Game.Renderer.Resolution.Width)
						pos = pos.WithX(Game.Renderer.Resolution.Width - tooltip.Bounds.Right);

					// If the tooltip overlaps the bottom edge of the screen, switch tooltip above cursor
					if (pos.Y + tooltip.Bounds.Bottom > Game.Renderer.Resolution.Height)
						pos = pos.WithY(Viewport.LastMousePos.Y + (CursorProvider.CursorViewportZoomed ? 2 : 1) * BottomEdgeYOffset - tooltip.Bounds.Height);
				}

				return pos;
			}
		}

		public override string GetCursor(int2 pos) { return null; }
	}
}
