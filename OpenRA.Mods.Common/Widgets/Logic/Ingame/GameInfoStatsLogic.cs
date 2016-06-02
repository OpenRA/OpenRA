#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class GameInfoStatsLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public GameInfoStatsLogic(Widget widget, World world, OrderManager orderManager)
		{
			var player = world.RenderPlayer ?? world.LocalPlayer;
			var playerPanel = widget.Get<ScrollPanelWidget>("PLAYER_LIST");

			if (player != null && !player.NonCombatant)
			{
				var checkbox = widget.Get<CheckboxWidget>("STATS_CHECKBOX");
				var statusLabel = widget.Get<LabelWidget>("STATS_STATUS");

				checkbox.IsChecked = () => player.WinState != WinState.Undefined;
				checkbox.GetCheckType = () => player.WinState == WinState.Won ?
					"checked" : "crossed";

				if (player.HasObjectives)
				{
					var mo = player.PlayerActor.Trait<MissionObjectives>();
					checkbox.GetText = () => mo.Objectives.First().Description;
				}

				statusLabel.GetText = () => player.WinState == WinState.Won ? "Accomplished" :
					player.WinState == WinState.Lost ? "Failed" : "In progress";
				statusLabel.GetColor = () => player.WinState == WinState.Won ? Color.LimeGreen :
					player.WinState == WinState.Lost ? Color.Red : Color.White;
			}
			else
			{
				// Expand the stats window to cover the hidden objectives
				var objectiveGroup = widget.Get("OBJECTIVE");
				var statsHeader = widget.Get("STATS_HEADERS");

				objectiveGroup.Visible = false;
				statsHeader.Bounds.Y -= objectiveGroup.Bounds.Height;
				playerPanel.Bounds.Y -= objectiveGroup.Bounds.Height;
				playerPanel.Bounds.Height += objectiveGroup.Bounds.Height;
			}

			var playerTemplate = playerPanel.Get("PLAYER_TEMPLATE");
			playerPanel.RemoveChildren();

			foreach (var p in world.Players.Where(a => !a.NonCombatant))
			{
				var pp = p;
				var client = world.LobbyInfo.ClientWithIndex(pp.ClientIndex);
				var item = playerTemplate.Clone();
				LobbyUtils.SetupClientWidget(item, client, orderManager, client != null && client.Bot == null);
				var nameLabel = item.Get<LabelWidget>("NAME");
				var nameFont = Game.Renderer.Fonts[nameLabel.Font];

				var suffixLength = new CachedTransform<string, int>(s => nameFont.Measure(s).X);
				var name = new CachedTransform<Pair<string, int>, string>(c =>
					WidgetUtils.TruncateText(c.First, nameLabel.Bounds.Width - c.Second, nameFont));

				nameLabel.GetText = () =>
				{
					var suffix = pp.WinState == WinState.Undefined ? "" : " (" + pp.WinState + ")";
					if (client != null && client.State == Session.ClientState.Disconnected)
						suffix = " (Gone)";

					var sl = suffixLength.Update(suffix);
					return name.Update(Pair.New(pp.PlayerName, sl)) + suffix;
				};
				nameLabel.GetColor = () => pp.Color.RGB;

				var flag = item.Get<ImageWidget>("FACTIONFLAG");
				flag.GetImageCollection = () => "flags";
				if (player == null || player.Stances[pp] == Stance.Ally || player.WinState != WinState.Undefined)
				{
					flag.GetImageName = () => pp.Faction.InternalName;
					item.Get<LabelWidget>("FACTION").GetText = () => pp.Faction.Name;
				}
				else
				{
					flag.GetImageName = () => pp.DisplayFaction.InternalName;
					item.Get<LabelWidget>("FACTION").GetText = () => pp.DisplayFaction.Name;
				}

				var team = item.Get<LabelWidget>("TEAM");
				var teamNumber = pp.PlayerReference.Playable ? ((client == null) ? 0 : client.Team) : pp.PlayerReference.Team;
				team.GetText = () => (teamNumber == 0) ? "-" : teamNumber.ToString();
				playerPanel.AddChild(item);

				var stats = pp.PlayerActor.TraitOrDefault<PlayerStatistics>();
				if (stats == null)
					break;
				var totalKills = stats.UnitsKilled + stats.BuildingsKilled;
				var totalDeaths = stats.UnitsDead + stats.BuildingsDead;
				item.Get<LabelWidget>("KILLS").GetText = () => totalKills.ToString();
				item.Get<LabelWidget>("DEATHS").GetText = () => totalDeaths.ToString();
			}
		}
	}
}
