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
			var options = new Dictionary<string, IEnumerable<SlotDropDownOption>>() {{"Slot", new List<SlotDropDownOption>()
			{
				new SlotDropDownOption("Open", "slot_open "+slot.PlayerReference, () => (!slot.Closed && client == null)),
				new SlotDropDownOption("Closed", "slot_close "+slot.PlayerReference, () => slot.Closed)
			}}};

			var bots = new List<SlotDropDownOption>();
			if (slot.AllowBots)
			{
				foreach (var b in Rules.Info["player"].Traits.WithInterface<IBotInfo>().Select(t => t.Name))
				{
					var bot = b;
					var botController = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
					bots.Add(new SlotDropDownOption(bot,
						"slot_bot {0} {1} {2}".F(slot.PlayerReference, botController.Index, bot),
						() => client != null && client.Bot == bot));
				}
			}
			options.Add(bots.Any() ? "Bots" : "Bots Disabled", bots);

			Func<SlotDropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					o.Selected,
					() => orderManager.IssueOrder(Order.Command(o.Order)));
				item.Get<LabelWidget>("LABEL").GetText = () => o.Title;
				return item;
			};

			dropdown.ShowDropDown<SlotDropDownOption>("LABEL_DROPDOWN_TEMPLATE", 167, options, setupItem);
		}

		public static void ShowTeamDropDown(DropDownButtonWidget dropdown, Session.Client client,
			OrderManager orderManager, int teamCount)
		{
			Func<int, ScrollItemWidget, ScrollItemWidget> setupItem = (ii, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => client.Team == ii,
					() => orderManager.IssueOrder(Order.Command("team {0} {1}".F(client.Index, ii))));
				item.Get<LabelWidget>("LABEL").GetText = () => ii == 0 ? "-" : ii.ToString();
				return item;
			};

			var options = Exts.MakeArray(teamCount + 1, i => i).ToList();
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
			Action onExit = () =>
			{
				if (client.Bot == null)
				{
					Game.Settings.Player.Color = preview.Color;
					Game.Settings.Save();
				}

				color.RemovePanel();
				orderManager.IssueOrder(Order.Command("color {0} {1}".F(client.Index, preview.Color)));
			};

			Action<HSLColor> onChange = c => preview.Color = c;

			var colorChooser = Game.LoadWidget(orderManager.world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onChange", onChange },
				{ "initialColor", client.Color }
			});

			color.AttachPanel(colorChooser, onExit);
		}

		public static Dictionary<CPos, Session.Client> GetSpawnClients(OrderManager orderManager, MapPreview preview)
		{
			var spawns = preview.SpawnPoints;
			return orderManager.LobbyInfo.Clients
				.Where(c => c.SpawnPoint != 0)
				.ToDictionary(c => spawns[c.SpawnPoint - 1], c => c);
		}

		public static void SelectSpawnPoint(OrderManager orderManager, MapPreviewWidget mapPreview, MapPreview preview, MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				return; 

			if (!orderManager.LocalClient.IsObserver && orderManager.LocalClient.State == Session.ClientState.Ready)
				return;

			var selectedSpawn = preview.SpawnPoints
				.Select((sp, i) => Pair.New(mapPreview.ConvertToPreview(sp), i))
				.Where(a => (a.First - mi.Location).LengthSquared < 64)
				.Select(a => a.Second + 1)
				.FirstOrDefault();

			var owned = orderManager.LobbyInfo.Clients.Any(c => c.SpawnPoint == selectedSpawn);
			if (selectedSpawn == 0 || !owned)
			{
				var locals = orderManager.LobbyInfo.Clients.Where(c => c.Index == orderManager.LocalClient.Index || (Game.IsHost && c.Bot != null));
				var playerToMove = locals.FirstOrDefault(c => ((selectedSpawn == 0) ^ (c.SpawnPoint == 0) && !c.IsObserver));
				orderManager.IssueOrder(Order.Command("spawn {0} {1}".F((playerToMove ?? orderManager.LocalClient).Index, selectedSpawn)));
			}
		}

		public static Color LatencyColor(int latency)
		{
			// Levels set relative to the default order lag of 3 net ticks (360ms)
			// TODO: Adjust this once dynamic lag is implemented
			if (latency < 0)
				return Color.Gray;
			if (latency < 300)
				return Color.LimeGreen;
			if (latency < 600)
				return Color.Orange;
			return Color.Red;
		}

		public static string LatencyDescription(int latency)
		{
			if (latency < 0)
				return "Unknown";
			if (latency < 300)
				return "Good";
			if (latency < 600)
				return "Moderate";
			return "Poor";
		}

		public static string DescriptiveIpAddress(string ip)
		{
			if (ip == null)
				return "Unknown Host";
			if (ip == "127.0.0.1")
				return "Local Host";
			return ip;
		}

		public static string LookupCountry(string ip)
		{
			var ip2geo = new GeoIP.LookupService("GeoIP.dat", GeoIP.LookupService.GEOIP_MEMORY_CACHE);
			var country = ip2geo.getCountry(ip);
			return country.getName();
		}

		public static void SetupClientWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, bool visible)
		{
			parent.Get("ADMIN_INDICATOR").IsVisible = () => c.IsAdmin;
			var block = parent.Get("LATENCY");
			block.IsVisible = () => visible;

			if (visible)
				block.Get<ColorBlockWidget>("LATENCY_COLOR").GetColor = () => LatencyColor(c.Latency);

			var tooltip = parent.Get<ClientTooltipRegionWidget>("CLIENT_REGION");
			tooltip.IsVisible = () => visible;
			tooltip.Bind(orderManager, c.Index);
		}

		public static void SetupEditableNameWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager)
		{
			var name = parent.Get<TextFieldWidget>("NAME");
			name.IsVisible = () => true;
			name.IsDisabled = () => orderManager.LocalClient.IsReady;

			name.Text = c.Name;
			name.OnLoseFocus = () =>
			{
				name.Text = name.Text.Trim();
				if (name.Text.Length == 0)
					name.Text = c.Name;

				if (name.Text == c.Name)
					return;

				orderManager.IssueOrder(Order.Command("name " + name.Text));
				Game.Settings.Player.Name = name.Text;
				Game.Settings.Save();
			};

			name.OnEnterKey = () => { name.YieldKeyboardFocus(); return true; };
		}

		public static void SetupNameWidget(Widget parent, Session.Slot s, Session.Client c)
		{
			var name = parent.Get<LabelWidget>("NAME");
			name.GetText = () => c.Name;
		}

		public static void SetupEditableSlotWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager)
		{
			var slot = parent.Get<DropDownButtonWidget>("SLOT_OPTIONS");
			slot.IsVisible = () => true;
			slot.IsDisabled = () => orderManager.LocalClient.IsReady;
			slot.GetText = () => c != null ? c.Name : s.Closed ? "Closed" : "Open";
			slot.OnMouseDown = _ => ShowSlotDropDown(slot, s, c, orderManager);

			// Ensure Name selector (if present) is hidden
			var name = parent.GetOrNull("NAME");
			if (name != null)
				name.IsVisible = () => false;
		}

		public static void SetupSlotWidget(Widget parent, Session.Slot s, Session.Client c)
		{
			var name = parent.Get<LabelWidget>("NAME");
			name.IsVisible = () => true;
			name.GetText = () => c != null ? c.Name : s.Closed ? "Closed" : "Open";

			// Ensure Slot selector (if present) is hidden
			var slot = parent.GetOrNull("SLOT_OPTIONS");
			if (slot != null)
				slot.IsVisible = () => false;
		}

		public static void SetupKickWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, Widget lobby, Action before, Action after)
		{
			var button = parent.Get<ButtonWidget>("KICK");
			button.IsVisible = () => Game.IsHost && c.Index != orderManager.LocalClient.Index;
			button.IsDisabled = () => orderManager.LocalClient.IsReady;
			Action<bool> okPressed = tempBan => { orderManager.IssueOrder(Order.Command("kick {0} {1}".F(c.Index, tempBan))); after(); };
			button.OnClick = () =>
			{
				before();

				Game.LoadWidget(null, "KICK_CLIENT_DIALOG", lobby, new WidgetArgs
				{
					{ "clientName", c.Name },
					{ "okPressed", okPressed },
					{ "cancelPressed", after }
				});
			};
		}

		public static void SetupKickSpectatorsWidget(Widget parent, OrderManager orderManager, Widget lobby, Action before, Action after, bool skirmishMode)
		{
			var checkBox = parent.Get<CheckboxWidget>("TOGGLE_SPECTATORS");
			checkBox.IsChecked = () => orderManager.LobbyInfo.GlobalSettings.AllowSpectators;
			checkBox.IsVisible = () => orderManager.LocalClient.IsAdmin && !skirmishMode;
			checkBox.IsDisabled = () => false;

			Action okPressed = () => 
			{
				orderManager.IssueOrder(Order.Command("allow_spectators {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllowSpectators)));
				orderManager.IssueOrders(
					orderManager.LobbyInfo.Clients.Where(
						c => c.IsObserver && !c.IsAdmin).Select(
							client => Order.Command(String.Format("kick {0} {1}", client.Index, client.Name
							))).ToArray());

				after();
			};

			checkBox.OnClick = () =>
			{
				before();

				int spectatorCount = orderManager.LobbyInfo.Clients.Count(c => c.IsObserver);
				if (spectatorCount > 0)
				{
					Game.LoadWidget(null, "KICK_SPECTATORS_DIALOG", lobby, new WidgetArgs
					{
						{ "clientCount", "{0}".F(spectatorCount) },
						{ "okPressed", okPressed },
						{ "cancelPressed", after }
					});
				}
				else
				{
					orderManager.IssueOrder(Order.Command("allow_spectators {0}".F(!orderManager.LobbyInfo.GlobalSettings.AllowSpectators)));
					after();
				}
			};
		}

		public static void SetupEditableColorWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, ColorPreviewManagerWidget colorPreview)
		{
			var color = parent.Get<DropDownButtonWidget>("COLOR");
			color.IsDisabled = () => (s != null && s.LockColor) || orderManager.LocalClient.IsReady;
			color.OnMouseDown = _ => ShowColorDropDown(color, c, orderManager, colorPreview);

			SetupColorWidget(color, s, c);
		}

		public static void SetupColorWidget(Widget parent, Session.Slot s, Session.Client c)
		{
			var color = parent.Get<ColorBlockWidget>("COLORBLOCK");
			color.GetColor = () => c.Color.RGB;
		}

		public static void SetupEditableFactionWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, Dictionary<string,string> countryNames)
		{
			var dropdown = parent.Get<DropDownButtonWidget>("FACTION");
			dropdown.IsDisabled = () => s.LockRace || orderManager.LocalClient.IsReady;
			dropdown.OnMouseDown = _ => ShowRaceDropDown(dropdown, c, orderManager, countryNames);
			SetupFactionWidget(dropdown, s, c, countryNames);
		}

		public static void SetupFactionWidget(Widget parent, Session.Slot s, Session.Client c, Dictionary<string,string> countryNames)
		{
			var factionname = parent.Get<LabelWidget>("FACTIONNAME");
			factionname.GetText = () => countryNames[c.Country];
			var factionflag = parent.Get<ImageWidget>("FACTIONFLAG");
			factionflag.GetImageName = () => c.Country;
			factionflag.GetImageCollection = () => "flags";
		}

		public static void SetupEditableTeamWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, int teamCount)
		{
			var dropdown = parent.Get<DropDownButtonWidget>("TEAM");
			dropdown.IsDisabled = () => s.LockTeam || orderManager.LocalClient.IsReady;
			dropdown.OnMouseDown = _ => ShowTeamDropDown(dropdown, c, orderManager, teamCount);
			dropdown.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString();
		}

		public static void SetupTeamWidget(Widget parent, Session.Slot s, Session.Client c)
		{
			parent.Get<LabelWidget>("TEAM").GetText = () => (c.Team == 0) ? "-" : c.Team.ToString();
		}

		public static void SetupEditableReadyWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager)
		{
			var status = parent.Get<CheckboxWidget>("STATUS_CHECKBOX");
			status.IsChecked = () => orderManager.LocalClient.IsReady || c.Bot != null;
			status.IsVisible = () => true;
			status.IsDisabled = () => c.Bot != null;
			status.OnClick = () => orderManager.IssueOrder(Order.Command("ready"));
		}

		public static void SetupReadyWidget(Widget parent, Session.Slot s, Session.Client c)
		{
			parent.Get<ImageWidget>("STATUS_IMAGE").IsVisible = () => c.IsReady || c.Bot != null;
		}

		public static void AddPlayerFlagAndName(ScrollItemWidget template, Player player)
		{
			var flag = template.Get<ImageWidget>("FLAG");
			flag.GetImageName = () => player.Country.Race;
			flag.GetImageCollection = () => "flags";

			var playerName = template.Get<LabelWidget>("PLAYER");
			playerName.GetText = () => player.PlayerName + (player.WinState == WinState.Undefined ? "" : " (" + player.WinState + ")");
			playerName.GetColor = () => player.Color.RGB;
		}
	}
}
