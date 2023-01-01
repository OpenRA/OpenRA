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
	public class SpawnSelectorTooltipLogic : ChromeLogic
	{
		[TranslationReference]
		const string DisabledSpawn = "label-disabled-spawn";

		[TranslationReference]
		const string AvailableSpawn = "label-available-spawn";

		[TranslationReference("team")]
		const string TeamNumber = "label-team-name";

		readonly CachedTransform<int, string> teamMessage;

		[ObjectCreator.UseCtor]
		public SpawnSelectorTooltipLogic(Widget widget, ModData modData, TooltipContainerWidget tooltipContainer, MapPreviewWidget preview, bool showUnoccupiedSpawnpoints)
		{
			var showTooltip = true;
			widget.IsVisible = () => preview.TooltipSpawnIndex != -1 && showTooltip;
			var label = widget.Get<LabelWidget>("LABEL");
			var flag = widget.Get<ImageWidget>("FLAG");
			var team = widget.Get<LabelWidget>("TEAM");
			var singleHeight = widget.Get("SINGLE_HEIGHT").Bounds.Height;
			var doubleHeight = widget.Get("DOUBLE_HEIGHT").Bounds.Height;
			var ownerFont = Game.Renderer.Fonts[label.Font];
			var teamFont = Game.Renderer.Fonts[team.Font];

			// Width specified in YAML is used as the margin between flag / label and label / border
			var labelMargin = widget.Bounds.Width;

			var labelText = "";
			string playerFaction = null;
			var playerTeam = -1;
			teamMessage = new CachedTransform<int, string>(t => modData.Translation.GetString(TeamNumber, Translation.Arguments("team", t)));
			var disabledSpawn = modData.Translation.GetString(DisabledSpawn);
			var availableSpawn = modData.Translation.GetString(AvailableSpawn);

			tooltipContainer.BeforeRender = () =>
			{
				showTooltip = true;

				var teamWidth = 0;
				if (preview.SpawnOccupants().TryGetValue(preview.TooltipSpawnIndex, out var occupant))
				{
					labelText = occupant.PlayerName;
					playerFaction = occupant.Faction;
					playerTeam = occupant.Team;
					widget.Bounds.Height = playerTeam > 0 ? doubleHeight : singleHeight;
					teamWidth = teamFont.Measure(team.GetText()).X;
				}
				else
				{
					if (!showUnoccupiedSpawnpoints)
					{
						showTooltip = false;
						return;
					}

					labelText = preview.DisabledSpawnPoints().Contains(preview.TooltipSpawnIndex)
						? disabledSpawn
						: availableSpawn;

					playerFaction = null;
					playerTeam = 0;
					widget.Bounds.Height = singleHeight;
				}

				label.Bounds.X = playerFaction != null ? flag.Bounds.Right + labelMargin : labelMargin;
				label.Bounds.Width = ownerFont.Measure(labelText).X;

				widget.Bounds.Width = Math.Max(teamWidth + 2 * labelMargin, label.Bounds.Right + labelMargin);
				team.Bounds.Width = widget.Bounds.Width;
			};

			label.GetText = () => labelText;
			flag.IsVisible = () => playerFaction != null;
			flag.GetImageCollection = () => "flags";
			flag.GetImageName = () => playerFaction;
			team.GetText = () => playerTeam > 0 ? teamMessage.Update(playerTeam) : "";
			team.IsVisible = () => playerTeam > 0;
		}
	}
}
