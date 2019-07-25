#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
			var sideMargin = (int)desc.LayoutX;
			var bottomMargin = (int)discs.LayoutHeight;
			foreach (var disc in discTitles)
			{
				var label = (LabelWidget)template.Clone();
				var title = disc;
				label.GetText = () => title;
				label.Top = (int)discs.LayoutHeight;
				label.Width = font.Measure(disc).X;

				maxWidth = Math.Max(maxWidth, (int)label.LayoutWidth + (int)label.LayoutX);
				discs.AddChild(label);
				discs.Height = (int)discs.LayoutHeight + (int)label.LayoutHeight;
			}

			widget.Width = 2 * sideMargin + maxWidth;
			widget.Height = (int)discs.LayoutY + bottomMargin + (int)discs.LayoutHeight;
		}
	}
}
