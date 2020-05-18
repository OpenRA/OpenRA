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
	public class ModContentDiscTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ModContentDiscTooltipLogic(Widget widget, Func<string> getText)
		{
			var discs = widget.Get<ContainerWidget>("DISCS");
			var template = discs.Get<LabelWidget>("DISC_TEMPLATE");
			discs.RemoveChildren();

			var desc = widget.Get<LabelWidget>("DESCRIPTION");

			var font = Game.Renderer.Fonts[template.Font];
			var discTitles = getText().Split('\n');

			var maxWidth = Game.Renderer.Fonts[desc.Font].Measure(desc.Text).X;
			var sideMargin = (int)desc.Node.LayoutX;
			var bottomMargin = (int)discs.Node.LayoutHeight;
			foreach (var disc in discTitles)
			{
				var label = (LabelWidget)template.Clone();
				var title = disc;
				label.GetText = () => title;
				label.Node.Top = (int)discs.Node.LayoutHeight;
				label.Node.Width = font.Measure(disc).X;
				label.Node.CalculateLayout();

				maxWidth = Math.Max(maxWidth, (int)label.Node.LayoutWidth + (int)label.Node.LayoutX);
				discs.AddChild(label);
				discs.Node.Height = (int)discs.Node.LayoutHeight + (int)label.Node.LayoutHeight;
				discs.Node.CalculateLayout();
			}

			widget.Node.Width = 2 * sideMargin + maxWidth;
			widget.Node.Height = (int)discs.Node.LayoutY + bottomMargin + (int)discs.Node.LayoutHeight;
			widget.Node.CalculateLayout();
		}
	}
}
