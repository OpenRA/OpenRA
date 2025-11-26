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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class AIBattleLogic : ChromeLogic
	{
		readonly ModData modData;
		readonly Action onExit;
		readonly Widget panel;

		MapPreview selectedMap;
		readonly List<AISlotConfig> aiSlots = [];
		int simulationSpeed = 4; // 4x default

		// Game options
		bool exploredMap;
		bool fogOfWar = true;
		int startingCash = 5000;
		string gameSpeed = "default";

		[FluentReference]
		const string AIBattleTitle = "label-ai-battle-title";

		public sealed class AISlotConfig
		{
			public string BotType;
			public string Faction;
			public int Team;
		}

		[ObjectCreator.UseCtor]
		public AIBattleLogic(Widget widget, ModData modData, Action onExit)
		{
			this.modData = modData;
			this.onExit = onExit;
			panel = widget;

			SetupMapSelector();
			SetupGameOptions();
			SetupSpeedSelector();
			SetupButtons();
		}

		void SetupMapSelector()
		{
			var mapButton = panel.Get<ButtonWidget>("MAP_BUTTON");
			var mapPreview = panel.Get<MapPreviewWidget>("MAP_PREVIEW");

			// Default to last used map or first available multiplayer map
			var mapId = Game.Settings.Server.Map;
			selectedMap = modData.MapCache[mapId];

			if (selectedMap == null || selectedMap.Status != MapStatus.Available || selectedMap.PlayerCount < 2)
				selectedMap = modData.MapCache
					.Where(m => m.Status == MapStatus.Available && m.PlayerCount >= 2 && m.Visibility.HasFlag(MapVisibility.Lobby))
					.OrderByDescending(m => m.PlayerCount)
					.FirstOrDefault();

			// Set preview to always return the current selectedMap
			mapPreview.Preview = () => selectedMap;
			RebuildAISlots();

			mapButton.OnClick = () =>
			{
				Ui.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs
				{
					{ "initialMap", selectedMap?.Uid },
					{ "initialGeneratedMap", (MapGenerationArgs)null },
					{ "remoteMapPool", null },
					{ "initialTab", MapClassification.System },
					{ "onExit", () => { } },
					{ "onSelect", (Action<string>)(uid =>
					{
						selectedMap = modData.MapCache[uid];
						RebuildAISlots();
					})},
					{ "onSelectGenerated", null },
					{ "filter", MapVisibility.Lobby },
				});
			};
		}

		void RebuildAISlots()
		{
			if (selectedMap == null)
				return;

			var slotContainer = panel.Get<ScrollPanelWidget>("AI_SLOTS_CONTAINER");
			slotContainer.RemoveChildren();

			aiSlots.Clear();

			// Get available bot types from the selected map
			var botTypes = selectedMap.PlayerActorInfo.TraitInfos<IBotInfo>()
				.Where(b => b.Type != null)
				.Select(b => new BotTypeInfo { Type = b.Type, Name = b.Name })
				.ToList();

			// Get available factions from the selected map
			var factions = selectedMap.WorldActorInfo.TraitInfos<FactionInfo>()
				.Where(f => f.Selectable)
				.ToList();

			var playerCount = Math.Min(selectedMap.PlayerCount, 8);
			var template = panel.Get<ContainerWidget>("AI_SLOT_TEMPLATE");

			// Calculate max valid teams (half the player count, minimum 2 if more than 1 player)
			var maxTeams = Math.Max(2, playerCount / 2);

			for (var i = 0; i < playerCount; i++)
			{
				var slot = new AISlotConfig
				{
					BotType = botTypes.FirstOrDefault()?.Type ?? "normal",
					Faction = factions.Count > 0 ? factions[i % factions.Count].InternalName : "Random",
					Team = (i % maxTeams) + 1, // Alternate teams within valid range
				};
				aiSlots.Add(slot);

				var slotWidget = CreateAISlotWidget(i, slot, botTypes, factions, template, maxTeams);
				slotContainer.AddChild(slotWidget);
			}

			slotContainer.ScrollToTop();
		}

		void SetupGameOptions()
		{
			var gameOptions = panel.GetOrNull("GAME_OPTIONS");
			if (gameOptions == null)
				return;

			// Explored Map checkbox
			var exploredCheckbox = gameOptions.GetOrNull<CheckboxWidget>("EXPLORED_CHECKBOX");
			if (exploredCheckbox != null)
			{
				exploredCheckbox.IsChecked = () => exploredMap;
				exploredCheckbox.OnClick = () => exploredMap = !exploredMap;
			}

			// Fog of War checkbox
			var fogCheckbox = gameOptions.GetOrNull<CheckboxWidget>("FOG_CHECKBOX");
			if (fogCheckbox != null)
			{
				fogCheckbox.IsChecked = () => fogOfWar;
				fogCheckbox.OnClick = () => fogOfWar = !fogOfWar;
			}

			// Starting Cash dropdown
			var cashDropdown = gameOptions.GetOrNull<DropDownButtonWidget>("CASH_DROPDOWN");
			if (cashDropdown != null)
			{
				var cashOptions = new[] { 2500, 5000, 10000, 20000, 50000 };
				cashDropdown.GetText = () => $"${startingCash:N0}";
				cashDropdown.OnClick = () =>
				{
					ScrollItemWidget SetupItem(int c, ScrollItemWidget itemTemplate)
					{
						var item = ScrollItemWidget.Setup(itemTemplate, () => startingCash == c, () => startingCash = c);
						item.Get<LabelWidget>("LABEL").GetText = () => $"${c:N0}";
						return item;
					}

					cashDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 200, cashOptions, SetupItem);
				};
			}

			// Game Speed dropdown
			var speedDropdown = gameOptions.GetOrNull<DropDownButtonWidget>("GAME_SPEED_DROPDOWN");
			if (speedDropdown != null)
			{
				var speedOptions = new[] { ("slowest", "Slowest"), ("slower", "Slower"), ("default", "Normal"), ("faster", "Faster"), ("fastest", "Fastest") };
				speedDropdown.GetText = () => speedOptions.FirstOrDefault(s => s.Item1 == gameSpeed).Item2 ?? "Normal";
				speedDropdown.OnClick = () =>
				{
					ScrollItemWidget SetupItem((string, string) s, ScrollItemWidget itemTemplate)
					{
						var item = ScrollItemWidget.Setup(itemTemplate, () => gameSpeed == s.Item1, () => gameSpeed = s.Item1);
						item.Get<LabelWidget>("LABEL").GetText = () => s.Item2;
						return item;
					}

					speedDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 200, speedOptions, SetupItem);
				};
			}
		}

		sealed class BotTypeInfo
		{
			public string Type;
			public string Name;

			public string GetDisplayName()
			{
				// Name is a fluent reference like "bot-omnius.name"
				if (!string.IsNullOrEmpty(Name))
				{
					var displayName = FluentProvider.GetMessage(Name);
					if (!string.IsNullOrEmpty(displayName))
						return displayName;
				}

				// Fallback to type with first letter capitalized
				if (!string.IsNullOrEmpty(Type))
					return char.ToUpper(Type[0]) + Type[1..];

				return "AI";
			}
		}

		Widget CreateAISlotWidget(int index, AISlotConfig slot,
			List<BotTypeInfo> botTypes, List<FactionInfo> factions, Widget template, int maxTeams)
		{
			var widget = template.Clone();
			widget.Id = $"AI_SLOT_{index}";
			widget.IsVisible = () => true;
			widget.Bounds.Y = index * (template.Bounds.Height + 5);

			// Slot number label
			var slotLabel = widget.Get<LabelWidget>("SLOT_NUMBER");
			var slotNum = index + 1;
			slotLabel.GetText = () => slotNum.ToString();

			// AI Type dropdown - show display name
			var botDropdown = widget.Get<DropDownButtonWidget>("BOT_DROPDOWN");
			botDropdown.GetText = () =>
			{
				var botInfo = botTypes.FirstOrDefault(b => b.Type == slot.BotType);
				return botInfo?.GetDisplayName() ?? slot.BotType;
			};
			botDropdown.OnClick = () =>
			{
				ScrollItemWidget SetupItem(BotTypeInfo bt, ScrollItemWidget itemTemplate)
				{
					var item = ScrollItemWidget.Setup(itemTemplate, () => slot.BotType == bt.Type, () => slot.BotType = bt.Type);
					var displayName = bt.GetDisplayName();
					item.Get<LabelWidget>("LABEL").GetText = () => displayName;
					return item;
				}

				botDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 200, botTypes, SetupItem);
			};

			// Faction dropdown - show display name, not internal name
			var factionDropdown = widget.Get<DropDownButtonWidget>("FACTION_DROPDOWN");
			factionDropdown.GetText = () =>
			{
				var factionInfo = factions.FirstOrDefault(f => f.InternalName == slot.Faction);
				if (factionInfo?.Name != null)
					return FluentProvider.GetMessage(factionInfo.Name);
				return slot.Faction;
			};
			factionDropdown.OnClick = () =>
			{
				ScrollItemWidget SetupItem(FactionInfo f, ScrollItemWidget itemTemplate)
				{
					var item = ScrollItemWidget.Setup(itemTemplate, () => slot.Faction == f.InternalName, () => slot.Faction = f.InternalName);
					var factionName = f.Name != null ? FluentProvider.GetMessage(f.Name) : f.InternalName;
					item.Get<LabelWidget>("LABEL").GetText = () => factionName;
					return item;
				}

				factionDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 200, factions, SetupItem);
			};

			// Team dropdown - limited to valid teams based on player count
			var teamDropdown = widget.Get<DropDownButtonWidget>("TEAM_DROPDOWN");
			teamDropdown.GetText = () => slot.Team == 0 ? "-" : slot.Team.ToString();
			teamDropdown.OnClick = () =>
			{
				// Teams: 0 (no team), then 1 through maxTeams
				var teams = Enumerable.Range(0, maxTeams + 1).ToList();
				ScrollItemWidget SetupItem(int t, ScrollItemWidget itemTemplate)
				{
					var item = ScrollItemWidget.Setup(itemTemplate, () => slot.Team == t, () => slot.Team = t);
					var teamLabel = t == 0 ? "-" : t.ToString();
					item.Get<LabelWidget>("LABEL").GetText = () => teamLabel;
					return item;
				}

				teamDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, teams, SetupItem);
			};

			return widget;
		}

		void SetupSpeedSelector()
		{
			var speedDropdown = panel.Get<DropDownButtonWidget>("SPEED_DROPDOWN");
			var speeds = new[] { 1, 2, 4, 8, 16, 32, 64, 128 };

			speedDropdown.GetText = () => $"{simulationSpeed}x";
			speedDropdown.OnClick = () =>
			{
				ScrollItemWidget SetupItem(int s, ScrollItemWidget itemTemplate)
				{
					var item = ScrollItemWidget.Setup(itemTemplate, () => simulationSpeed == s, () => simulationSpeed = s);
					item.Get<LabelWidget>("LABEL").GetText = () => $"{s}x";
					return item;
				}

				speedDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 250, speeds, SetupItem);
			};
		}

		void SetupButtons()
		{
			var startButton = panel.Get<ButtonWidget>("START_BUTTON");
			startButton.OnClick = StartAIBattle;
			startButton.IsDisabled = () => selectedMap == null || aiSlots.Count < 2;

			var backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = () =>
			{
				Ui.CloseWindow();
				onExit();
			};
		}

		void StartAIBattle()
		{
			if (selectedMap == null || aiSlots.Count < 2)
				return;

			// Store configuration for AIBattleManager
			AIBattleManager.PendingConfig = new AIBattleConfig
			{
				MapUid = selectedMap.Uid,
				AISlots = aiSlots.ToList(),
				SimulationSpeed = simulationSpeed
			};

			// Set up AI Battle state
			AIBattleState.IsAIBattle = true;
			AIBattleState.SpeedMultiplier = simulationSpeed;
			AIBattleState.IsPaused = false;

			// Close the config panel and start the game
			Ui.CloseWindow();

			// Get map slot names for bot placement
			var slots = selectedMap.Players.Players
				.Where(p => p.Value.Playable)
				.Select(p => p.Key)
				.Take(aiSlots.Count)
				.ToArray();

			// Build orders to configure the AI battle
			var orders = new List<Order>();

			// Enable singleplayer mode (allows no human players in slots)
			orders.Add(Order.Command("option singleplayer True"));

			// Set game options
			orders.Add(Order.Command($"option explored {exploredMap}"));
			orders.Add(Order.Command($"option fog {fogOfWar}"));
			orders.Add(Order.Command($"option startingcash {startingCash}"));
			orders.Add(Order.Command($"option gamespeed {gameSpeed}"));

			// Make the local player a spectator (they will observe the AI battle)
			orders.Add(Order.Command("spectate"));

			// Add bots to each slot
			for (var i = 0; i < aiSlots.Count && i < slots.Length; i++)
			{
				var slot = aiSlots[i];
				var slotId = slots[i];

				// Add bot to slot (slot_bot slotId controllerClientIndex botType)
				orders.Add(Order.Command($"slot_bot {slotId} 0 {slot.BotType}"));
			}

			// After bots are added, set their factions and teams
			// Note: The server assigns incremental client indices to bots
			// We need to wait for the bots to be added before setting properties
			// This will be handled by LobbyInfoChanged callback

			// Mark the spectator as ready - game will auto-start when all players are ready
			orders.Add(Order.Command($"state {Session.ClientState.Ready}"));

			Game.CreateAndStartLocalServer(selectedMap.Uid, orders);
		}

	}

	public sealed class AIBattleConfig
	{
		public string MapUid;
		public List<AIBattleLogic.AISlotConfig> AISlots;
		public int SimulationSpeed;
	}

	public static class AIBattleManager
	{
		public static AIBattleConfig PendingConfig;
		public static string LastReplayPath;
		public static AIBattleResults LastResults;
	}

	public sealed class AIBattleResults
	{
		public int DurationTicks;
		public int Timestep;
		public string WinnerName;
		public string WinnerFaction;
		public List<AIPlayerStats> PlayerStats = [];
	}

	public sealed class AIPlayerStats
	{
		public string Name;
		public string Faction;
		public int Team;
		public bool IsWinner;
		public int UnitsKilled;
		public int UnitsDead;
		public int BuildingsKilled;
		public int BuildingsDead;
		public int KillsCost;
		public int DeathsCost;
		public int Earned;
		public int Spent;
		public int ArmyValue;
	}
}
