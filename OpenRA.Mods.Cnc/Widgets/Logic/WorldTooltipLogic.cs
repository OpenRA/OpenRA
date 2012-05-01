#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class WorldTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public WorldTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, CncWorldInteractionControllerWidget wic)
		{
			widget.IsVisible = () => wic.TooltipType != WorldTooltipType.None;
			var label = widget.Get<LabelWidget>("LABEL");
			var flag = widget.Get<ImageWidget>("FLAG");
			var owner = widget.Get<LabelWidget>("OWNER");

			var font = Game.Renderer.Fonts[label.Font];
			var ownerFont = Game.Renderer.Fonts[owner.Font];
			var cachedWidth = 0;
			var labelText = "";
			var showOwner = false;
			var flagRace = "";
			var ownerName = "";
			var ownerColor = Color.White;
			var doubleHeight = 45;
			var singleHeight = 25;

			tooltipContainer.BeforeRender = () =>
			{
				if (wic == null || wic.TooltipType == WorldTooltipType.None)
					return;

				labelText = wic.TooltipType == WorldTooltipType.Unexplored ? "Unexplored Terrain" :
					wic.ActorTooltip.Name();
				var textWidth = font.Measure(labelText).X;
				if (textWidth != cachedWidth)
				{
					label.Bounds.Width = textWidth;
					widget.Bounds.Width = 2*label.Bounds.X + textWidth;
				}
				var o = wic.ActorTooltip != null ? wic.ActorTooltip.Owner() : null;
				showOwner = wic.TooltipType == WorldTooltipType.Actor && o != null && !o.NonCombatant;

				if (showOwner)
				{
					flagRace = o.Country.Race;
					ownerName = o.PlayerName;
					ownerColor = o.ColorRamp.GetColor(0);
					widget.Bounds.Height = doubleHeight;
					widget.Bounds.Width = Math.Max(widget.Bounds.Width,
						owner.Bounds.X + ownerFont.Measure(ownerName).X + 5);
				}
				else
					widget.Bounds.Height = singleHeight;
			};

			label.GetText = () => labelText;
			flag.IsVisible = () => showOwner;
			flag.GetImageCollection = () => "flags";
			flag.GetImageName = () => flagRace;
			owner.IsVisible = () => showOwner;
			owner.GetText = () => ownerName;
			owner.GetColor = () => ownerColor;
		}
	}
}

