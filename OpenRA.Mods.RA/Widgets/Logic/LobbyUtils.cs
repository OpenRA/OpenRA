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
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public static class LobbyUtils
	{
		public static void SetupNameWidget(OrderManager orderManager, Session.Client c, TextFieldWidget name)
		{
			if (c.IsAdmin)
				name.Font = "Bold";
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
					var botController = orderManager.LobbyInfo.Clients.Where(c => c.IsAdmin).FirstOrDefault();
					options.Add(new SlotDropDownOption("Bot: {0}".F(bot),
						"slot_bot {0} {1} {2}".F(slot.PlayerReference, botController.Index, bot),
						() => client != null && client.Bot == bot));
				}

			Func<SlotDropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					o.Selected,
					() => orderManager.IssueOrder(Order.Command(o.Order)));
				item.Get<LabelWidget>("LABEL").GetText = () => o.Title;
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
				item.Get<LabelWidget>("LABEL").GetText = () => ii == 0 ? "-" : ii.ToString();
				return item;
			};

			var options = Exts.MakeArray(map.GetSpawnPoints().Length + 1, i => i).ToList();
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
				item.Get<LabelWidget>("LABEL").GetText = () => countryNames[race];
				var flag = item.Get<ImageWidget>("FLAG");
				flag.GetImageCollection = () => "flags";
				flag.GetImageName = () => race;
				return item;
			};

			dropdown.ShowDropDown("RACE_DROPDOWN_TEMPLATE", 150, countryNames.Keys, setupItem);
		}

		public static void ShowColorDropDown(DropDownButtonWidget color, Session.Client client,
			OrderManager orderManager, ColorPreviewManagerWidget preview)
		{
			Action<ColorRamp> onSelect = c =>
			{
				if (client.Bot == null)
				{
					Game.Settings.Player.ColorRamp = c;
					Game.Settings.Save();
				}

				color.RemovePanel();
				orderManager.IssueOrder(Order.Command("color {0} {1}".F(client.Index, c)));
			};

			Action<ColorRamp> onChange = c => preview.Ramp = c;

			var colorChooser = Game.LoadWidget(orderManager.world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onSelect", onSelect },
				{ "onChange", onChange },
				{ "initialRamp", client.ColorRamp }
			});

			color.AttachPanel(colorChooser);
		}

		public static Dictionary<int2, Color> GetSpawnColors(OrderManager orderManager, Map map)
		{
			var spawns = map.GetSpawnPoints();
			return orderManager.LobbyInfo.Clients
				.Where( c => c.SpawnPoint != 0)
				.ToDictionary(
					c => spawns[c.SpawnPoint - 1],
					c => c.ColorRamp.GetColor(0));
		}

		public static void SelectSpawnPoint(OrderManager orderManager, MapPreviewWidget mapPreview, Map map, MouseInput mi)
		{
			if (map == null || mi.Button != MouseButton.Left
				|| orderManager.LocalClient.State == Session.ClientState.Ready)
				return;

			var selectedSpawn = map.GetSpawnPoints()
				.Select((sp, i) => Pair.New(mapPreview.ConvertToPreview(sp), i))
				.Where(a => (a.First - mi.Location).LengthSquared < 64)
				.Select(a => a.Second + 1)
				.FirstOrDefault();

			var owned = orderManager.LobbyInfo.Clients.Any(c => c.SpawnPoint == selectedSpawn);
			if (selectedSpawn == 0 || !owned)
			{
				var locals = orderManager.LobbyInfo.Clients.Where(c => c.Index == orderManager.LocalClient.Index || (Game.IsHost && c.Bot != null));
				var playerToMove = locals.FirstOrDefault(c => (selectedSpawn == 0) ^ (c.SpawnPoint == 0));
				orderManager.IssueOrder(Order.Command("spawn {0} {1}".F((playerToMove ?? orderManager.LocalClient).Index, selectedSpawn)));
			}
		}

		public static void ShowSpawnPointTooltip(OrderManager orderManager, int spawnPoint, int2 position)
		{
			var client = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.SpawnPoint == spawnPoint);
			if (client != null)
			{
				Game.Renderer.Fonts["Bold"].DrawTextWithContrast(client.Name, position + new int2(5, 5), Color.White, Color.Black, 1);
			}
		}
	}
}
