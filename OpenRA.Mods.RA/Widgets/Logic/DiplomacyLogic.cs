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
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class DiplomacyLogic
	{
		readonly World world;

		ScrollPanelWidget diplomacyPanel;

		[ObjectCreator.UseCtor]
		public DiplomacyLogic(Widget widget, Action onExit, World world)
		{
			this.world = world;

			diplomacyPanel = widget.Get<ScrollPanelWidget>("DIPLOMACY_PANEL");

			LayoutPlayers();
		}

		void LayoutPlayers()
		{
			var teamTemplate = diplomacyPanel.Get<ScrollItemWidget>("TEAM_TEMPLATE");
			var players = world.Players.Where(p => p != world.LocalPlayer && !p.NonCombatant);
			var teams = players.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.ClientIndex) ?? new Session.Client()).Team).OrderBy(g => g.Key);
			foreach (var t in teams)
			{
				var team = t;
				var tt = ScrollItemWidget.Setup(teamTemplate, () => false, () => { });
				tt.IgnoreMouseOver = true;
				tt.Get<LabelWidget>("TEAM").GetText = () => team.Key == 0 ? "No Team" : "Team " + team.Key;
				diplomacyPanel.AddChild(tt);
				foreach (var p in team)
				{
					var player = p;
					diplomacyPanel.AddChild(DiplomaticStatus(player));
				}
			}
		}

		ScrollItemWidget DiplomaticStatus(Player player)
		{
			var playerTemplate = diplomacyPanel.Get<ScrollItemWidget>("PLAYER_TEMPLATE");
			var pt = ScrollItemWidget.Setup(playerTemplate, () => false, () => { });
			pt.IgnoreMouseOver = true;
			LobbyUtils.AddPlayerFlagAndName(pt, player);
			pt.Get<LabelWidget>("THEIR_STANCE").GetText = () => player.Stances[world.LocalPlayer].ToString();
			var myStance = pt.Get<DropDownButtonWidget>("MY_STANCE");
			myStance.GetText = () => world.LocalPlayer.Stances[player].ToString();
			myStance.IsDisabled = () => !world.LobbyInfo.GlobalSettings.FragileAlliances;
			myStance.OnMouseDown = mi => ShowDropDown(player, myStance);
			return pt;
		}

		void ShowDropDown(Player p, DropDownButtonWidget dropdown)
		{
			var stances = Enum<Stance>.GetValues();
			Func<Stance, ScrollItemWidget, ScrollItemWidget> setupItem = (s, template) =>
			{
				var item = ScrollItemWidget.Setup(template,
					() => s == world.LocalPlayer.Stances[ p ],
					() => SetStance(dropdown, p, s));

				item.Get<LabelWidget>("LABEL").GetText = () => s.ToString();
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, stances, setupItem);
		}

		void SetStance(ButtonWidget bw, Player p, Stance ss)
		{
			if (!p.World.LobbyInfo.GlobalSettings.FragileAlliances)
				return;	// stance changes are banned

			// HACK: Abuse of the type system here with `CPos`
			world.IssueOrder(new Order("SetStance", world.LocalPlayer.PlayerActor, false)
			{ TargetLocation = new CPos((int)ss, 0), TargetString = p.InternalName, SuppressVisualFeedback = true });

			bw.Text = ss.ToString();
		}
	}
}
