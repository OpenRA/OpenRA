#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class SpawnSelectorTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public SpawnSelectorTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, MapPreviewWidget preview)
		{
			widget.IsVisible = () => preview.TooltipSpawnIndex != -1;
			var label = widget.Get<LabelWidget>("LABEL");
			var flag = widget.Get<ImageWidget>("FLAG");
			var team = widget.Get<LabelWidget>("TEAM");
			var singleHeight = widget.Get("SINGLE_HEIGHT").Bounds.Height;
			var doubleHeight = widget.Get("DOUBLE_HEIGHT").Bounds.Height;
			var ownerFont = Game.Renderer.Fonts[label.Font];
			var teamFont = Game.Renderer.Fonts[team.Font];

			// Width specified in YAML is used as the margin between flag / label and label / border
			var labelMargin = widget.Bounds.Width;

			var cachedWidth = 0;
			var labelText = "";
			string playerCountry = null;
			var playerTeam = -1;

			tooltipContainer.BeforeRender = () =>
			{
				var occupant = preview.SpawnOccupants().Values.FirstOrDefault(c => c.SpawnPoint == preview.TooltipSpawnIndex);

				var teamWidth = 0;
				if (occupant == null)
				{
					labelText = "Available spawn";
					playerCountry = null;
					playerTeam = 0;
					widget.Bounds.Height = singleHeight;
				}
				else
				{
					labelText = occupant.PlayerName;
					playerCountry = occupant.Country;
					playerTeam = occupant.Team;
					widget.Bounds.Height = playerTeam > 0 ? doubleHeight : singleHeight;
					teamWidth = teamFont.Measure(team.GetText()).X;
				}

				label.Bounds.X = playerCountry != null ? flag.Bounds.Right + labelMargin : labelMargin;

				var textWidth = ownerFont.Measure(labelText).X;
				if (textWidth != cachedWidth)
				{
					label.Bounds.Width = textWidth;
					widget.Bounds.Width = 2*label.Bounds.X + textWidth;
				}

				widget.Bounds.Width = Math.Max(teamWidth + 2*labelMargin, label.Bounds.Right + labelMargin);
				team.Bounds.Width = widget.Bounds.Width;
			};

			label.GetText = () => labelText;
			flag.IsVisible = () => playerCountry != null;
			flag.GetImageCollection = () => "flags";
			flag.GetImageName = () => playerCountry;
			team.GetText = () => "Team {0}".F(playerTeam);
			team.IsVisible = () => playerTeam > 0;
		}
	}
}

