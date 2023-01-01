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

using System;
using OpenRA.Graphics;
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
		string id;
		WidgetArgs widgetArgs;

		public TooltipContainerWidget()
		{
			graphicSettings = Game.Settings.Graphics;
			IsVisible = () =>
			{
				// PERF: Only load widget once visible.
				var visible = Game.RunTime > Viewport.LastMoveRunTime + TooltipDelayMilliseconds;
				if (visible)
					LoadWidget();

				return visible;
			};
		}

		void LoadWidget()
		{
			if (id == null || tooltip != null)
				return;

			tooltip = Ui.LoadWidget(id, this, new WidgetArgs(widgetArgs) { { "tooltipContainer", this } });
		}

		public int SetTooltip(string id, WidgetArgs args)
		{
			RemoveTooltip();
			currentToken = nextToken++;
			tooltip = null;
			this.id = id;
			widgetArgs = args;
			return currentToken;
		}

		public void RemoveTooltip(int token)
		{
			if (currentToken != token)
				return;

			tooltip = null;
			id = null;
			widgetArgs = null;

			RemoveChildren();
			BeforeRender = Nothing;
		}

		public void RemoveTooltip()
		{
			RemoveTooltip(currentToken);
		}

		public override void Draw()
		{
			BeforeRender();
		}

		public override bool EventBoundsContains(int2 location) { return false; }

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
