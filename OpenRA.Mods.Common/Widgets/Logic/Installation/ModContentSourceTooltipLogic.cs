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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ModContentSourceTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ModContentSourceTooltipLogic(Widget widget, Func<string> getText)
		{
			var sources = widget.Get<ContainerWidget>("SOURCES");
			var template = sources.Get<LabelWidget>("SOURCE_TEMPLATE");
			sources.RemoveChildren();

			var desc = widget.Get<LabelWidget>("DESCRIPTION");

			var font = Game.Renderer.Fonts[template.Font];
			var sourceTitles = getText().Split('\n');

			var maxWidth = Game.Renderer.Fonts[desc.Font].Measure(desc.Text).X;
			var sideMargin = desc.Bounds.X;
			var bottomMargin = sources.Bounds.Height;
			foreach (var source in sourceTitles)
			{
				var label = (LabelWidget)template.Clone();
				var title = source;
				label.GetText = () => title;
				label.Bounds.Y = sources.Bounds.Height;
				label.Bounds.Width = font.Measure(source).X;

				maxWidth = Math.Max(maxWidth, label.Bounds.Width + label.Bounds.X);
				sources.AddChild(label);
				sources.Bounds.Height += label.Bounds.Height;
			}

			widget.Bounds.Width = 2 * sideMargin + maxWidth;
			widget.Bounds.Height = sources.Bounds.Y + bottomMargin + sources.Bounds.Height;
		}
	}
}
