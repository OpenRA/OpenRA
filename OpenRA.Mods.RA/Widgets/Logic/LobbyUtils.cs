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
using System.Drawing;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public static class LobbyUtils
	{
		public static void SetupNameWidget(OrderManager orderManager, Session.Client c, TextFieldWidget name)
		{
			name.Text = c.Name;
			name.OnEnterKey = () =>
			{
				name.Text = name.Text.Trim();
				if (name.Text.Length == 0)
					name.Text = c.Name;

				name.LoseFocus();
				if (name.Text == c.Name)
					return true;

				orderManager.IssueOrder(Order.Command("name " + name.Text));
				Game.Settings.Player.Name = name.Text;
				Game.Settings.Save();
				return true;
			};
			name.OnLoseFocus = () => name.OnEnterKey();
		}

		class SlotDropDownOption
		{
			public string Title;
			public string Order;
			public Func<bool> Selected;

			public SlotDropDownOption(string title, string order, Func<bool> selected)
			{
				Title = title;
				Order = order;
				Selected = selected;
			}
		}

		public static void ShowSlotDropDown(DropDownButtonWidget dropdown, Session.Slot slot,
			Session.Client client, OrderManager orderManager)
		{
			var options = new List<SlotDropDownOption>()
			{
				new SlotDropDownOption("Open", "slot_open "+slot.PlayerReference, () => (!slot.Closed && client == null)),
				new SlotDropDownOption("Closed", "slot_close "+slot.PlayerReference, () => slot.Closed)
			};

			if (slot.AllowBots)
				foreach (var b in Rules.Info["player"].Traits.WithInterface<IBotInfo>().Select(t => t.Name))
				{
					var bot = b;
					options.Add(new SlotDropDownOption("Bot: {0}".F(bot),
						"slot_bot {0} {1}".F(slot.PlayerReference, bot),
						() => client != null && client.Bot == bot));
				}

			Func<SlotDropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					o.Selected,
					() => orderManager.IssueOrder(Order.Command(o.Order)));
				item.GetWidget<LabelWidget>("LABEL").GetText = () => o.Title;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, options, setupItem);
		}

		public static void ShowTeamDropDown(DropDownButtonWidget dropdown, Session.Client client,
			OrderManager orderManager, Map map)
		{
			Func<int, ScrollItemWidget, ScrollItemWidget> setupItem = (ii, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => client.Team == ii,
					() => orderManager.IssueOrder(Order.Command("team {0} {1}".F(client.Index, ii))));
				item.GetWidget<LabelWidget>("LABEL").GetText = () => ii == 0 ? "-" : ii.ToString();
				return item;
			};

			var options = Graphics.Util.MakeArray(map.SpawnPoints.Count() + 1, i => i).ToList();
			dropdown.ShowDropDown("TEAM_DROPDOWN_TEMPLATE", 150, options, setupItem);
		}

		public static void ShowRaceDropDown(DropDownButtonWidget dropdown, Session.Client client,
			OrderManager orderManager, Dictionary<string, string> countryNames)
		{
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (race, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => client.Country == race,
					() => orderManager.IssueOrder(Order.Command("race {0} {1}".F(client.Index, race))));
				item.GetWidget<LabelWidget>("LABEL").GetText = () => countryNames[race];
				var flag = item.GetWidget<ImageWidget>("FLAG");
				flag.GetImageCollection = () => "flags";
				flag.GetImageName = () => race;
				return item;
			};

			dropdown.ShowDropDown("RACE_DROPDOWN_TEMPLATE", 150, countryNames.Keys.ToList(), setupItem);
		}
	}
}
