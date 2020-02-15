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

using System;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class TooltipContainerWidget : Widget
	{
		static readonly Action Nothing = () => { };
		readonly GraphicSettings graphicSettings;

		public int2 CursorOffset = new int2(0, 20);
		public int BottomEdgeYOffset = -5;

		public Action BeforeRender = Nothing;
		public int TooltipDelayMilliseconds = 200;
		Widget tooltip;
		int nextToken = 1;
		int currentToken;

		public TooltipContainerWidget()
		{
			graphicSettings = Game.Settings.Graphics;
			IsVisible = () => Game.RunTime > Viewport.LastMoveRunTime + TooltipDelayMilliseconds;
		}

		public int SetTooltip(string id, WidgetArgs args)
		{
			RemoveTooltip();
			currentToken = nextToken++;

			tooltip = Ui.LoadWidget(id, this, new WidgetArgs(args) { { "tooltipContainer", this } });

			return currentToken;
		}

		public void RemoveTooltip(int token)
		{
			if (currentToken != token)
				return;

			RemoveChildren();
			BeforeRender = Nothing;
		}

		public void RemoveTooltip()
		{
			RemoveTooltip(currentToken);
		}

		public override void Draw() { BeforeRender(); }

		public override Rectangle GetEventBounds() { return Rectangle.Empty; }

		public override int2 ChildOrigin
		{
			get
			{
				var scale = graphicSettings.CursorDouble ? 2 : 1;
				var pos = Viewport.LastMousePos + scale * CursorOffset;
				if (tooltip != null)
				{
					// If the tooltip overlaps the right edge of the screen, move it left until it fits
					if (pos.X + tooltip.Bounds.Right > Game.Renderer.Resolution.Width)
						pos = pos.WithX(Game.Renderer.Resolution.Width - tooltip.Bounds.Right);

					// If the tooltip overlaps the bottom edge of the screen, switch tooltip above cursor
					if (pos.Y + tooltip.Bounds.Bottom > Game.Renderer.Resolution.Height)
						pos = pos.WithY(Viewport.LastMousePos.Y + scale * BottomEdgeYOffset - tooltip.Bounds.Height);
				}

				return pos;
			}
		}

		public override string GetCursor(int2 pos) { return null; }
	}
}
