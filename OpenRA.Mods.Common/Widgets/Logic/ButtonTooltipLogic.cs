#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ButtonTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public ButtonTooltipLogic(Widget widget, ButtonWidget button)
		{
			var label = widget.Get<LabelWidget>("LABEL");
			var hotkey = widget.Get<LabelWidget>("HOTKEY");

			label.GetText = () => button.TooltipText;
			var labelWidth = Game.Renderer.Fonts[label.Font].Measure(button.TooltipText).X;
			label.Bounds.Width = labelWidth;

			var hotkeyLabel = "({0})".F(button.Key.DisplayString());
			hotkey.GetText = () => hotkeyLabel;
			hotkey.Bounds.X = labelWidth + 2 * label.Bounds.X;

			var panelWidth = hotkey.Bounds.X + label.Bounds.X
				+ Game.Renderer.Fonts[label.Font].Measure(hotkeyLabel).X;
			widget.Bounds.Width = panelWidth;
		}
	}
}