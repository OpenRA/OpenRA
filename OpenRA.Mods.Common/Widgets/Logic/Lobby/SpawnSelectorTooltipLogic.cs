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
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SpawnSelectorTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SpawnSelectorTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, MapPreviewWidget preview, bool showUnoccupiedSpawnpoints)
		{
			bool showTooltip = true;
			widget.IsVisible = () => preview.TooltipSpawnIndex != -1 && showTooltip;
			var label = widget.Get<LabelWidget>("LABEL");
			var flag = widget.Get<ImageWidget>("FLAG");
			var team = widget.Get<LabelWidget>("TEAM");
			var singleHeight = (int)widget.Get("SINGLE_HEIGHT").LayoutHeight;
			var doubleHeight = (int)widget.Get("DOUBLE_HEIGHT").LayoutHeight;
			var ownerFont = Game.Renderer.Fonts[label.Font];
			var teamFont = Game.Renderer.Fonts[team.Font];

			// Width specified in YAML is used as the margin between flag / label and label / border
			var labelMargin = (int)widget.LayoutWidth;

			var cachedWidth = 0;
			var labelText = "";
			string playerFaction = null;
			var playerTeam = -1;

			tooltipContainer.BeforeRender = () =>
			{
				showTooltip = true;
				var occupant = preview.SpawnOccupants().Values.FirstOrDefault(c => c.SpawnPoint == preview.TooltipSpawnIndex);

				var teamWidth = 0;
				if (occupant == null)
				{
					if (!showUnoccupiedSpawnpoints)
					{
						showTooltip = false;
						return;
					}

					labelText = "Available spawn";
					playerFaction = null;
					playerTeam = 0;
					widget.Height = singleHeight;
				}
				else
				{
					labelText = occupant.PlayerName;
					playerFaction = occupant.Faction;
					playerTeam = occupant.Team;
					widget.Height = playerTeam > 0 ? doubleHeight : singleHeight;
					teamWidth = teamFont.Measure(team.GetText()).X;
				}

				label.Left = playerFaction != null ? ((int)flag.LayoutX + (int)flag.LayoutWidth) + labelMargin : labelMargin;

				var textWidth = ownerFont.Measure(labelText).X;
				if (textWidth != cachedWidth)
				{
					label.Width = textWidth;
					widget.Width = 2 * (int)label.LayoutX + textWidth;
				}

				widget.Width = Math.Max(teamWidth + 2 * labelMargin, ((int)label.LayoutX + (int)label.LayoutWidth) + labelMargin);
				team.Width = (int)widget.LayoutWidth;
			};

			label.GetText = () => labelText;
			flag.IsVisible = () => playerFaction != null;
			flag.GetImageCollection = () => "flags";
			flag.GetImageName = () => playerFaction;
			team.GetText = () => "Team {0}".F(playerTeam);
			team.IsVisible = () => playerTeam > 0;
		}
	}
}
