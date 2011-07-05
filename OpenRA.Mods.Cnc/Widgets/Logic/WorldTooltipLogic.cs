#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Support;
using OpenRA.Widgets;
using T = OpenRA.Mods.Cnc.Widgets.TooltipWorldInteractionControllerWidget;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class WorldTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public WorldTooltipLogic([ObjectCreator.Param] Widget widget,
		                         [ObjectCreator.Param] TooltipWorldInteractionControllerWidget wic)
		{
			widget.IsVisible = () => wic.TooltipType != T.WorldTooltipType.None;

			var label = widget.GetWidget<LabelWidget>("LABEL");
			var font = Game.Renderer.Fonts[label.Font];
			var cachedWidth = 0;
			label.GetText = () => 
			{
				var text = wic.TooltipType == T.WorldTooltipType.Unexplored ? "Unexplored Terrain" : 
					wic.ActorTooltip.Name();
				var textWidth = font.Measure(text).X;
				if (textWidth != cachedWidth)
				{
					label.Bounds.Width = textWidth;
					widget.Bounds.Width = 2*label.Bounds.X + textWidth;
				}
				return text;
			};
		}
	}
}

