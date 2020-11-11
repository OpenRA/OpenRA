#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
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
			Session.Client client, OrderManager orderManager, MapPreview map)
		{
			var options = new Dictionary<string, IEnumerable<SlotDropDownOption>>
			{
				{
					"Slot", new List<SlotDropDownOption>
					{
						new SlotDropDownOption("Open", "slot_open " + slot.PlayerReference, () => (!slot.Closed && client == null)),
						new SlotDropDownOption("Closed", "slot_close " + slot.PlayerReference, () => slot.Closed)
					}
				}
			};

			var bots = new List<SlotDropDownOption>();
			if (slot.AllowBots)
			{
				foreach (var b in map.Rules.Actors["player"].TraitInfos<IBotInfo>())
				{
					var botController = orderManager.LobbyInfo.Clients.FirstOrDefault(c => c.IsAdmin);
					bots.Add(new SlotDropDownOption(b.Name,
						"slot_bot {0} {1} {2}".F(slot.PlayerReference, botController.Index, b.Type),
						() => client != null && client.Bot == b.Type));
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

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 180, options, setupItem);
		}

		public static void ShowPlayerActionDropDown(DropDownButtonWidget dropdown, Session.Slot slot,
			Session.Client c, OrderManager orderManager, Widget lobby, Action before, Action after)
		{
			Action<bool> okPressed = tempBan => { orderManager.IssueOrder(Order.Command("kick {0} {1}".F(c.Index, tempBan))); after(); };
			var onClick = new Action(() =>
			{
				before();

				Game.LoadWidget(null, "KICK_CLIENT_DIALOG", lobby.Get("TOP_PANELS_ROOT"), new WidgetArgs
				{
					{ "clientName", c.Name },
					{ "okPressed", okPressed },
					{ "cancelPressed", after }
				});
			});

			var options = new List<DropDownOption>
			{
				new DropDownOption
				{
					Title = "Kick",
					OnClick = onClick
				},
			};

			if (orderManager.LobbyInfo.GlobalSettings.Dedicated)
			{
				options.Add(new DropDownOption
				{
					Title = "Transfer Admin",
					OnClick = () => orderManager.IssueOrder(Order.Command("make_admin {0}".F(c.Index)))
				});
			}

			if (!c.IsObserver && orderManager.LobbyInfo.GlobalSettings.AllowSpectators)
			{
				options.Add(new DropDownOption
				{
					Title = "Move to Spectator",
					OnClick = () => orderManager.IssueOrder(Order.Command("make_spectator {0}".F(c.Index)))
				});
			}

			Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate, o.IsSelected, o.OnClick);
				var labelWidget = item.Get<LabelWidget>("LABEL");
				labelWidget.GetText = () => o.Title;
				return item;
			};

			dropdown.ShowDropDown("PLAYERACTION_DROPDOWN_TEMPLATE", 167, options, setupItem);
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

			var options = Enumerable.Range(0, teamCount + 1);
			dropdown.ShowDropDown("TEAM_DROPDOWN_TEMPLATE", 150, options, setupItem);
		}

		public static void ShowSpawnDropDown(DropDownButtonWidget dropdown, Session.Client client,
			OrderManager orderManager, IEnumerable<int> spawnPoints)
		{
			Func<int, ScrollItemWidget, ScrollItemWidget> setupItem = (ii, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => client.SpawnPoint == ii,
					() => SetSpawnPoint(orderManager, client, ii));
				item.Get<LabelWidget>("LABEL").GetText = () => ii == 0 ? "-" : Convert.ToChar('A' - 1 + ii).ToString();
				return item;
			};

			dropdown.ShowDropDown("SPAWN_DROPDOWN_TEMPLATE", 150, spawnPoints, setupItem);
		}

		/// <summary>Splits a string into two parts on the first instance of a given token.</summary>
		static (string First, string Second) SplitOnFirstToken(string input, string token = "\\n")
		{
			if (string.IsNullOrEmpty(input))
				return (null, null);

			var split = input.IndexOf(token, StringComparison.Ordinal);
			var first = split > 0 ? input.Substring(0, split) : input;
			var second = split > 0 ? input.Substring(split + token.Length) : null;
			return (first, second);
		}

		public static void ShowFactionDropDown(DropDownButtonWidget dropdown, Session.Client client,
			OrderManager orderManager, Dictionary<string, LobbyFaction> factions)
		{
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (factionId, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => client.Faction == factionId,
					() => orderManager.IssueOrder(Order.Command("faction {0} {1}".F(client.Index, factionId))));
				var faction = factions[factionId];
				item.Get<LabelWidget>("LABEL").GetText = () => faction.Name;
				var flag = item.Get<ImageWidget>("FLAG");
				flag.GetImageCollection = () => "flags";
				flag.GetImageName = () => factionId;

				var tooltip = SplitOnFirstToken(faction.Description);
				item.GetTooltipText = () => tooltip.First;
				item.GetTooltipDesc = () => tooltip.Second;

				return item;
			};

			var options = factions.Where(f => f.Value.Selectable).GroupBy(f => f.Value.Side)
				.ToDictionary(g => g.Key ?? "", g => g.Select(f => f.Key));

			dropdown.ShowDropDown("FACTION_DROPDOWN_TEMPLATE", 154, options, setupItem);
		}

		public static void ShowColorDropDown(DropDownButtonWidget color, Session.Client client,
			OrderManager orderManager, World world, ColorPreviewManagerWidget preview)
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

			Action<Color> onChange = c => preview.Color = c;

			var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onChange", onChange },
				{ "initialColor", client.Color },
				{ "initialFaction", client.Faction }
			});

			color.AttachPanel(colorChooser, onExit);
		}

		public static void SelectSpawnPoint(OrderManager orderManager, MapPreviewWidget mapPreview, MapPreview preview, MouseInput mi)
		{
			if (orderManager.LocalClient.State == Session.ClientState.Ready)
				return;

			if (mi.Button == MouseButton.Left)
				SelectPlayerSpawnPoint(orderManager, mapPreview, preview, mi);

			if (mi.Button == MouseButton.Right)
				ClearPlayerSpawnPoint(orderManager, mapPreview, preview, mi);
		}

		static void SelectPlayerSpawnPoint(OrderManager orderManager, MapPreviewWidget mapPreview, MapPreview preview, MouseInput mi)
		{
			var selectedSpawn = DetermineSelectedSpawnPoint(mapPreview, preview, mi);

			var locals = orderManager.LobbyInfo.Clients.Where(c => c.Index == orderManager.LocalClient.Index || (Game.IsHost && c.Bot != null));
			var playerToMove = locals.FirstOrDefault(c => ((selectedSpawn == 0) ^ (c.SpawnPoint == 0) && !c.IsObserver));
			SetSpawnPoint(orderManager, playerToMove, selectedSpawn);
		}

		static void ClearPlayerSpawnPoint(OrderManager orderManager, MapPreviewWidget mapPreview, MapPreview preview, MouseInput mi)
		{
			var selectedSpawn = DetermineSelectedSpawnPoint(mapPreview, preview, mi);
			if (Game.IsHost || orderManager.LobbyInfo.Clients.FirstOrDefault(cc => cc.SpawnPoint == selectedSpawn) == orderManager.LocalClient)
				orderManager.IssueOrder(Order.Command("clear_spawn {0}".F(selectedSpawn)));
		}

		static int DetermineSelectedSpawnPoint(MapPreviewWidget mapPreview, MapPreview preview, MouseInput mi)
		{
			var spawnSize = ChromeProvider.GetImage("lobby-bits", "spawn-unclaimed").Size.XY;
			var selectedSpawn = preview.SpawnPoints
				.Select((sp, i) => (SpawnLocation: mapPreview.ConvertToPreview(sp, preview.GridType), Index: i))
				.Where(a => ((a.SpawnLocation - mi.Location).ToFloat2() / spawnSize * 2).LengthSquared <= 1)
				.Select(a => a.Index + 1)
				.FirstOrDefault();
			return selectedSpawn;
		}

		static void SetSpawnPoint(OrderManager orderManager, Session.Client playerToMove, int selectedSpawnPoint)
		{
			var owned = orderManager.LobbyInfo.Clients.Any(c => c.SpawnPoint == selectedSpawnPoint) || orderManager.LobbyInfo.DisabledSpawnPoints.Contains(selectedSpawnPoint);
			if (selectedSpawnPoint == 0 || !owned)
				orderManager.IssueOrder(Order.Command("spawn {0} {1}".F((playerToMove ?? orderManager.LocalClient).Index, selectedSpawnPoint)));
		}

		public static List<int> AvailableSpawnPoints(int spawnPoints, Session lobbyInfo)
		{
			return Enumerable.Range(1, spawnPoints).Except(lobbyInfo.DisabledSpawnPoints).ToList();
		}

		public static bool InsufficientEnabledSpawnPoints(MapPreview map, Session lobbyInfo)
		{
			// If a map doesn't define spawn points we always have enough space
			var spawnPoints = map.SpawnPoints.Length;
			if (spawnPoints == 0)
				return false;

			return AvailableSpawnPoints(spawnPoints, lobbyInfo).Count < lobbyInfo.Clients.Count(c => !c.IsObserver);
		}

		public static Color LatencyColor(Session.ClientPing ping)
		{
			if (ping == null)
				return Color.Gray;

			// Levels set relative to the default order lag of 3 net ticks (360ms)
			// TODO: Adjust this once dynamic lag is implemented
			if (ping.Latency < 0)
				return Color.Gray;
			if (ping.Latency < 300)
				return Color.LimeGreen;
			if (ping.Latency < 600)
				return Color.Orange;
			return Color.Red;
		}

		public static string LatencyDescription(Session.ClientPing ping)
		{
			if (ping == null)
				return "Unknown";

			if (ping.Latency < 0)
				return "Unknown";
			if (ping.Latency < 300)
				return "Good";
			if (ping.Latency < 600)
				return "Moderate";
			return "Poor";
		}

		public static void SetupLatencyWidget(Widget parent, Session.Client c, OrderManager orderManager)
		{
			var visible = c != null && c.Bot == null;
			var block = parent.GetOrNull("LATENCY");
			if (block != null)
			{
				block.IsVisible = () => visible;

				if (visible)
					block.Get<ColorBlockWidget>("LATENCY_COLOR").GetColor = () => LatencyColor(
						orderManager.LobbyInfo.PingFromClient(c));
			}

			var tooltip = parent.Get<ClientTooltipRegionWidget>("LATENCY_REGION");
			tooltip.IsVisible = () => visible;
			if (visible)
				tooltip.Bind(orderManager, null, c);
		}

		public static void SetupProfileWidget(Widget parent, Session.Client c, OrderManager orderManager, WorldRenderer worldRenderer)
		{
			var visible = c != null && c.Bot == null;
			var profile = parent.GetOrNull<ImageWidget>("PROFILE");
			if (profile != null)
			{
				var imageName = (c != null && c.IsAdmin ? "admin-" : "player-")
					+ (c.Fingerprint != null ? "registered" : "anonymous");

				profile.GetImageName = () => imageName;
				profile.IsVisible = () => visible;
			}

			var profileTooltip = parent.GetOrNull<ClientTooltipRegionWidget>("PROFILE_TOOLTIP");
			if (profileTooltip != null)
			{
				if (c != null && c.Fingerprint != null)
					profileTooltip.Template = "REGISTERED_PLAYER_TOOLTIP";

				if (visible)
					profileTooltip.Bind(orderManager, worldRenderer, c);

				profileTooltip.IsVisible = () => visible;
			}
		}

		public static void SetupEditableNameWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, WorldRenderer worldRenderer)
		{
			var name = parent.Get<TextFieldWidget>("NAME");
			name.IsVisible = () => true;
			name.IsDisabled = () => orderManager.LocalClient.IsReady;

			name.Text = c.Name;
			var escPressed = false;
			name.OnLoseFocus = () =>
			{
				if (escPressed)
				{
					escPressed = false;
					return;
				}

				name.Text = name.Text.Trim();
				if (name.Text.Length == 0)
					name.Text = c.Name;
				else if (name.Text != c.Name)
				{
					name.Text = Settings.SanitizedPlayerName(name.Text);
					orderManager.IssueOrder(Order.Command("name " + name.Text));
					Game.Settings.Player.Name = name.Text;
					Game.Settings.Save();
				}
			};

			name.OnEnterKey = () => { name.YieldKeyboardFocus(); return true; };
			name.OnEscKey = () =>
			{
				name.Text = c.Name;
				escPressed = true;
				name.YieldKeyboardFocus();
				return true;
			};

			SetupProfileWidget(name, c, orderManager, worldRenderer);

			HideChildWidget(parent, "SLOT_OPTIONS");
		}

		public static void SetupNameWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, WorldRenderer worldRenderer)
		{
			var name = parent.Get<LabelWidget>("NAME");
			name.IsVisible = () => true;
			var font = Game.Renderer.Fonts[name.Font];
			var label = WidgetUtils.TruncateText(c.Name, name.Bounds.Width, font);
			name.GetText = () => label;

			SetupProfileWidget(parent, c, orderManager, worldRenderer);
		}

		public static void SetupEditableSlotWidget(Widget parent, Session.Slot s, Session.Client c,
			OrderManager orderManager, WorldRenderer worldRenderer, MapPreview map)
		{
			var slot = parent.Get<DropDownButtonWidget>("SLOT_OPTIONS");
			slot.IsVisible = () => true;
			slot.IsDisabled = () => orderManager.LocalClient.IsReady;

			var truncated = new CachedTransform<string, string>(name =>
				WidgetUtils.TruncateText(name, slot.Bounds.Width - slot.Bounds.Height - slot.LeftMargin - slot.RightMargin,
				Game.Renderer.Fonts[slot.Font]));

			slot.GetText = () => truncated.Update(c != null ? c.Name : s.Closed ? "Closed" : "Open");
			slot.OnMouseDown = _ => ShowSlotDropDown(slot, s, c, orderManager, map);

			// Ensure Name selector (if present) is hidden
			HideChildWidget(parent, "NAME");
		}

		public static void SetupSlotWidget(Widget parent, Session.Slot s, Session.Client c)
		{
			var name = parent.Get<LabelWidget>("NAME");
			name.IsVisible = () => true;
			name.GetText = () => c != null ? c.Name : s.Closed ? "Closed" : "Open";

			// Ensure Slot selector (if present) is hidden
			HideChildWidget(parent, "SLOT_OPTIONS");
		}

		public static void SetupPlayerActionWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager,
			WorldRenderer worldRenderer, Widget lobby, Action before, Action after)
		{
			var slot = parent.Get<DropDownButtonWidget>("PLAYER_ACTION");
			slot.IsVisible = () => Game.IsHost && c.Index != orderManager.LocalClient.Index;
			slot.IsDisabled = () => orderManager.LocalClient.IsReady;

			var truncated = new CachedTransform<string, string>(name =>
				WidgetUtils.TruncateText(name, slot.Bounds.Width - slot.Bounds.Height - slot.LeftMargin - slot.RightMargin,
				Game.Renderer.Fonts[slot.Font]));

			slot.GetText = () => truncated.Update(c != null ? c.Name : string.Empty);
			slot.OnMouseDown = _ => ShowPlayerActionDropDown(slot, s, c, orderManager, lobby, before, after);

			SetupProfileWidget(slot, c, orderManager, worldRenderer);

			// Ensure Name selector (if present) is hidden
			HideChildWidget(parent, "NAME");
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
							client => Order.Command("kick {0} {1}".F(client.Index, client.Name))).ToArray());

				after();
			};

			checkBox.OnClick = () =>
			{
				before();

				var spectatorCount = orderManager.LobbyInfo.Clients.Count(c => c.IsObserver);
				if (spectatorCount > 0)
				{
					Game.LoadWidget(null, "KICK_SPECTATORS_DIALOG", lobby.Get("TOP_PANELS_ROOT"), new WidgetArgs
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

		public static void SetupEditableColorWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, World world, ColorPreviewManagerWidget colorPreview)
		{
			var color = parent.Get<DropDownButtonWidget>("COLOR");
			color.IsDisabled = () => (s != null && s.LockColor) || orderManager.LocalClient.IsReady;
			color.OnMouseDown = _ => ShowColorDropDown(color, c, orderManager, world, colorPreview);

			SetupColorWidget(color, s, c);
		}

		public static void SetupColorWidget(Widget parent, Session.Slot s, Session.Client c)
		{
			var color = parent.Get<ColorBlockWidget>("COLORBLOCK");
			color.GetColor = () => c.Color;
		}

		public static void SetupEditableFactionWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager,
			Dictionary<string, LobbyFaction> factions)
		{
			var dropdown = parent.Get<DropDownButtonWidget>("FACTION");
			dropdown.IsDisabled = () => s.LockFaction || orderManager.LocalClient.IsReady;
			dropdown.OnMouseDown = _ => ShowFactionDropDown(dropdown, c, orderManager, factions);

			var tooltip = SplitOnFirstToken(factions[c.Faction].Description);
			dropdown.GetTooltipText = () => tooltip.First;
			dropdown.GetTooltipDesc = () => tooltip.Second;

			SetupFactionWidget(dropdown, s, c, factions);
		}

		public static void SetupFactionWidget(Widget parent, Session.Slot s, Session.Client c,
			Dictionary<string, LobbyFaction> factions)
		{
			var factionName = parent.Get<LabelWidget>("FACTIONNAME");
			factionName.GetText = () => factions[c.Faction].Name;
			var factionFlag = parent.Get<ImageWidget>("FACTIONFLAG");
			factionFlag.GetImageName = () => c.Faction;
			factionFlag.GetImageCollection = () => "flags";
		}

		public static void SetupEditableTeamWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, MapPreview map)
		{
			var dropdown = parent.Get<DropDownButtonWidget>("TEAM_DROPDOWN");
			dropdown.IsVisible = () => true;
			dropdown.IsDisabled = () => s.LockTeam || orderManager.LocalClient.IsReady;
			dropdown.OnMouseDown = _ => ShowTeamDropDown(dropdown, c, orderManager, map.PlayerCount);
			dropdown.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString();

			HideChildWidget(parent, "TEAM");
		}

		public static void SetupTeamWidget(Widget parent, Session.Slot s, Session.Client c)
		{
			var team = parent.Get<LabelWidget>("TEAM");
			team.IsVisible = () => true;
			team.GetText = () => (c.Team == 0) ? "-" : c.Team.ToString();
			HideChildWidget(parent, "TEAM_DROPDOWN");
		}

		public static void SetupEditableSpawnWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, MapPreview map)
		{
			var dropdown = parent.Get<DropDownButtonWidget>("SPAWN_DROPDOWN");
			dropdown.IsVisible = () => true;
			dropdown.IsDisabled = () => s.LockSpawn || orderManager.LocalClient.IsReady;
			dropdown.OnMouseDown = _ =>
			{
				var spawnPoints = Enumerable.Range(0, map.SpawnPoints.Length + 1).Except(
					orderManager.LobbyInfo.Clients.Where(
					client => client != c && client.SpawnPoint != 0).Select(client => client.SpawnPoint))
					.Except(orderManager.LobbyInfo.DisabledSpawnPoints);
				ShowSpawnDropDown(dropdown, c, orderManager, spawnPoints);
			};
			dropdown.GetText = () => (c.SpawnPoint == 0) ? "-" : Convert.ToChar('A' - 1 + c.SpawnPoint).ToString();

			HideChildWidget(parent, "SPAWN");
		}

		public static void SetupSpawnWidget(Widget parent, Session.Slot s, Session.Client c)
		{
			var spawn = parent.Get<LabelWidget>("SPAWN");
			spawn.IsVisible = () => true;
			spawn.GetText = () => (c.SpawnPoint == 0) ? "-" : Convert.ToChar('A' - 1 + c.SpawnPoint).ToString();
			HideChildWidget(parent, "SPAWN_DROPDOWN");
		}

		public static void SetupEditableReadyWidget(Widget parent, Session.Slot s, Session.Client c, OrderManager orderManager, MapPreview map)
		{
			var status = parent.Get<CheckboxWidget>("STATUS_CHECKBOX");
			status.IsChecked = () => orderManager.LocalClient.IsReady || c.Bot != null;
			status.IsVisible = () => true;
			status.IsDisabled = () => c.Bot != null || map.Status != MapStatus.Available ||
				!map.RulesLoaded || map.InvalidCustomRules;

			var state = orderManager.LocalClient.IsReady ? Session.ClientState.NotReady : Session.ClientState.Ready;
			status.OnClick = () => orderManager.IssueOrder(Order.Command("state {0}".F(state)));
		}

		public static void SetupReadyWidget(Widget parent, Session.Slot s, Session.Client c)
		{
			parent.Get<ImageWidget>("STATUS_IMAGE").IsVisible = () => c.IsReady || c.Bot != null;
		}

		public static void HideReadyWidgets(Widget parent)
		{
			HideChildWidget(parent, "STATUS_CHECKBOX");
			HideChildWidget(parent, "STATUS_IMAGE");
		}

		public static void SetupChatLine(ContainerWidget template, DateTime time, string name, Color nameColor, string text, Color textColor)
		{
			var nameLabel = template.Get<LabelWidget>("NAME");
			var timeLabel = template.Get<LabelWidget>("TIME");
			var textLabel = template.Get<LabelWidget>("TEXT");

			var nameText = name + ":";
			var font = Game.Renderer.Fonts[nameLabel.Font];
			var nameSize = font.Measure(nameText);

			timeLabel.GetText = () => "{0:D2}:{1:D2}".F(time.Hour, time.Minute);

			nameLabel.GetColor = () => nameColor;
			nameLabel.GetText = () => nameText;
			nameLabel.Bounds.Width = nameSize.X;

			textLabel.GetColor = () => textColor;
			textLabel.Bounds.X += nameSize.X;
			textLabel.Bounds.Width -= nameSize.X;

			// Hack around our hacky wordwrap behavior: need to resize the widget to fit the text
			text = WidgetUtils.WrapText(text, textLabel.Bounds.Width, font);
			textLabel.GetText = () => text;
			var dh = font.Measure(text).Y - textLabel.Bounds.Height;
			if (dh > 0)
			{
				textLabel.Bounds.Height += dh;
				template.Bounds.Height += dh;
			}
		}

		static void HideChildWidget(Widget parent, string widgetId)
		{
			var widget = parent.GetOrNull(widgetId);
			if (widget != null)
				widget.IsVisible = () => false;
		}
	}

	class ShowPlayerActionDropDownOption
	{
		public Action Click { get; set; }
		public string Title;
		public Func<bool> Selected = () => false;

		public ShowPlayerActionDropDownOption(string title, Action click)
		{
			Click = click;
			Title = title;
		}
	}
}
