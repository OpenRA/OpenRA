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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ObserverShroudSelectorLogic
	{
		class CameraOption
		{
			public string Label;
			public Func<bool> IsSelected;
			public Action OnClick;

			public CameraOption(string label, Func<bool> isSelected, Action onClick)
			{
				Label = label;
				IsSelected = isSelected;
				OnClick = onClick;
			}
		}

		static string LabelForPlayer(Player p)
		{
			if (p == null)
				return "Disable shroud";

			if (p.InternalName == "Everyone")
				return "Combined view";

			return p.PlayerName;
		}

		[ObjectCreator.UseCtor]
		public ObserverShroudSelectorLogic(Widget widget, World world)
		{
			var groups = new Dictionary<string, IEnumerable<CameraOption>>();

			var teams = world.Players.Where(p => !p.NonCombatant)
				.GroupBy(p => (world.LobbyInfo.ClientWithIndex(p.ClientIndex) ?? new Session.Client()).Team).OrderBy(g => g.Key);
			var noTeams = teams.Count() == 1;

			foreach (var t in teams)
			{
				var team = t.Select(p => new CameraOption(LabelForPlayer(p),
					() => world.RenderPlayer == p,
					() => world.RenderPlayer = p));

				var label = noTeams ? "Players" : t.Key == 0 ? "No Team" : "Team {0}".F(t.Key);
				groups.Add(label, team);
			}

			var combined = world.Players.First(p => p.InternalName == "Everyone");
			groups.Add("Other", new List<CameraOption>()
			{
				new CameraOption(LabelForPlayer(combined), () => world.RenderPlayer == combined, () => world.RenderPlayer = combined),
				new CameraOption(LabelForPlayer(null), () => world.RenderPlayer == null, () => world.RenderPlayer = null)
			});

			var shroudSelector = widget.Get<DropDownButtonWidget>("SHROUD_SELECTOR");
			shroudSelector.GetText = () => LabelForPlayer(world.RenderPlayer);
			shroudSelector.OnMouseDown = _ =>
			{
				Func<CameraOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
				{
					var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
					item.Get<LabelWidget>("LABEL").GetText = () => option.Label;
					return item;
				};
				shroudSelector.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 400, groups, setupItem);
			};
		}
	}
}
