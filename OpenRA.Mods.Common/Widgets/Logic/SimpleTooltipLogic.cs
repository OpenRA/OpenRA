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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SimpleTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SimpleTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, Func<string> getText)
		{
			var label = widget.Get<LabelWidget>("LABEL");
			var spacing = widget.Get("LINE_HEIGHT");
			widget.RemoveChildren();

			var font = Game.Renderer.Fonts[label.Font];
			var horizontalPadding = (int)label.Node.LayoutWidth - (int)widget.Node.LayoutWidth;
			if (horizontalPadding <= 0)
				horizontalPadding = 2 * (int)label.Node.LayoutX;

			var cachedText = "";
			tooltipContainer.BeforeRender = () =>
			{
				var text = getText();
				if (text == cachedText)
					return;

				var lines = text.Split('\n');
				var textWidth = font.Measure(text).X;

				// Set up label widgets
				widget.RemoveChildren();
				var bottom = 0;
				for (var i = 0; i < lines.Length; i++)
				{
					var line = (LabelWidget)label.Clone();
					var lineText = lines[i];
					line.Node.Top = line.Node.LayoutY + (int)spacing.Node.LayoutY + i * (int)spacing.Node.LayoutHeight;
					line.Node.Width = textWidth;
					line.Node.CalculateLayout();
					line.GetText = () => lineText;
					widget.AddChild(line);
					bottom = (int)line.Node.LayoutY + (int)line.Node.LayoutHeight;
				}

				widget.Node.Width = horizontalPadding + textWidth;
				widget.Node.Height = bottom + (int)spacing.Node.LayoutY;
				widget.Node.CalculateLayout();
				cachedText = text;
			};
		}
	}
}
