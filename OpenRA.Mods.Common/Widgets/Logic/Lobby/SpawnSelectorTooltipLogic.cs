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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SpawnSelectorTooltipLogic : ChromeLogic
	{
		[FluentReference("spawn")]
		const string DisabledSpawn = "label-disabled-spawn";

		[FluentReference("spawn")]
		const string AvailableSpawn = "label-available-spawn";

		[FluentReference("spawn")]
		const string SpawnName = "label-spawn-name";

		[FluentReference("team", "spawn")]
		const string TeamSpawn = "label-team-spawn";

		readonly CachedTransform<string, string> disabledSpawnMessage;
		readonly CachedTransform<string, string> availableSpawnMessage;
		readonly CachedTransform<string, string> spawnMessage;
		readonly CachedTransform<(int Team, string Spawn), string> teamSpawnMessage;

		[ObjectCreator.UseCtor]
		public SpawnSelectorTooltipLogic(Widget widget, ModData modData,
			TooltipContainerWidget tooltipContainer, MapPreviewWidget preview, bool showUnoccupiedSpawnpoints)
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
			var playerSpawn = "";
			var occupied = false;
			disabledSpawnMessage = new CachedTransform<string, string>(s => FluentProvider.GetMessage(DisabledSpawn, "spawn", s));
			availableSpawnMessage = new CachedTransform<string, string>(s => FluentProvider.GetMessage(AvailableSpawn, "spawn", s));
			spawnMessage = new CachedTransform<string, string>(s => FluentProvider.GetMessage(SpawnName, "spawn", s));
			teamSpawnMessage = new CachedTransform<(int Team, string Spawn), string>(
				t => FluentProvider.GetMessage(TeamSpawn, "team", t.Team, "spawn", t.Spawn));

			tooltipContainer.BeforeRender = () =>
			{
				showTooltip = true;

				var teamWidth = 0;
				playerSpawn = Convert.ToChar('A' - 1 + preview.TooltipSpawnIndex).ToString();
				if (preview.SpawnOccupants().TryGetValue(preview.TooltipSpawnIndex, out var occupant))
				{
					labelText = occupant.PlayerName;
					playerFaction = occupant.Faction;
					playerTeam = occupant.Team;
					occupied = true;
					widget.Bounds.Height = doubleHeight;
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
						? disabledSpawnMessage.Update(playerSpawn)
						: availableSpawnMessage.Update(playerSpawn);

					playerFaction = null;
					playerTeam = 0;
					occupied = false;
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
			team.GetText = () =>
			{
				if (!occupied)
					return "";

				return playerTeam > 0 ? teamSpawnMessage.Update((playerTeam, playerSpawn)) : spawnMessage.Update(playerSpawn);
			};
			team.IsVisible = () => occupied;
		}
	}
}
