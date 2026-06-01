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
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SimpleTooltipLogic : ChromeLogic
	{
		const int LineSpacing = 2;

		[ObjectCreator.UseCtor]
		public SimpleTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, Func<string> getText)
		{
			var label = widget.Get<LabelWidget>("LABEL");
			var spacing = widget.Get("LINE_HEIGHT");
			widget.RemoveChildren();

			var font = Game.Renderer.Fonts[label.Font];
			var sidePadding = label.Bounds.X;
			var topPadding = spacing.Bounds.Y;
			var bottomPadding = topPadding;

			var cachedText = "";
			tooltipContainer.BeforeRender = () =>
			{
				var text = getText();
				if (text == cachedText)
					return;

				var lines = text.Split('\n');
				var lineSizes = lines.Select(line => font.Measure(line)).ToArray();
				var textWidth = lineSizes.Length > 0 ? lineSizes.Max(s => s.X) : 0;

				widget.RemoveChildren();
				var y = topPadding;
				for (var i = 0; i < lines.Length; i++)
				{
					var lineText = lines[i];
					var lineSize = lineSizes[i];
					var lineLabel = label.Clone();
					lineLabel.Bounds.X = sidePadding;
					lineLabel.Bounds.Y = y;
					lineLabel.Bounds.Width = textWidth;
					lineLabel.Bounds.Height = lineSize.Y;
					lineLabel.Align = TextAlign.Left;
					lineLabel.VAlign = TextVAlign.Top;
					lineLabel.GetText = () => lineText;
					widget.AddChild(lineLabel);
					y += lineSize.Y + LineSpacing;
				}

				if (lines.Length > 0)
					y -= LineSpacing;

				widget.Bounds.Width = 2 * sidePadding + textWidth;
				widget.Bounds.Height = y + bottomPadding;
				cachedText = text;
			};
		}
	}
}
