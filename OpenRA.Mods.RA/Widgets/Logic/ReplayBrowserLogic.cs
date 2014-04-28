#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ReplayBrowserLogic
	{
		static Filter filter = new Filter();

		Widget panel;
		ScrollPanelWidget playerList;
		ScrollItemWidget playerTemplate, playerHeader;
		List<ReplayMetadata> replays;
		Dictionary<ReplayMetadata, bool> replayVis = new Dictionary<ReplayMetadata, bool>();

		Dictionary<CPos, Session.Client> selectedSpawns;
		ReplayMetadata selectedReplay;

		[ObjectCreator.UseCtor]
		public ReplayBrowserLogic(Widget widget, Action onExit, Action onStart)
		{
			panel = widget;

			playerList = panel.Get<ScrollPanelWidget>("PLAYER_LIST");
			playerHeader = playerList.Get<ScrollItemWidget>("HEADER");
			playerTemplate = playerList.Get<ScrollItemWidget>("TEMPLATE");
			playerList.RemoveChildren();

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			var rl = panel.Get<ScrollPanelWidget>("REPLAY_LIST");
			var template = panel.Get<ScrollItemWidget>("REPLAY_TEMPLATE");

			var mod = Game.modData.Manifest.Mod;
			var dir = new[] { Platform.SupportDir, "Replays", mod.Id, mod.Version }.Aggregate(Path.Combine);

			rl.RemoveChildren();
			if (Directory.Exists(dir))
			{
				using (new Support.PerfTimer("Load replays"))
				{
					replays = Directory
						.GetFiles(dir, "*.rep")
						.Select((filename) => ReplayMetadata.Read(filename))
						.Where((r) => r != null)
						.OrderByDescending(r => r.StartTimestampUtc)
						.ToList();
				}

				foreach (var replay in replays)
					AddReplay(rl, replay, template);

				ApplyFilter();
			}

			var watch = panel.Get<ButtonWidget>("WATCH_BUTTON");
			watch.IsDisabled = () => selectedReplay == null || selectedReplay.MapPreview.Status != MapStatus.Available;
			watch.OnClick = () => { WatchReplay(); onStart(); };

			panel.Get("REPLAY_INFO").IsVisible = () => selectedReplay != null;

			var preview = panel.Get<MapPreviewWidget>("MAP_PREVIEW");
			preview.SpawnClients = () => selectedSpawns;
			preview.Preview = () => selectedReplay != null ? selectedReplay.MapPreview : null;

			var title = panel.GetOrNull<LabelWidget>("MAP_TITLE");
			if (title != null)
				title.GetText = () => selectedReplay != null ? selectedReplay.MapPreview.Title : null;

			var type = panel.GetOrNull<LabelWidget>("MAP_TYPE");
			if (type != null)
				type.GetText = () => selectedReplay.MapPreview.Type;

			panel.Get<LabelWidget>("DURATION").GetText = () => WidgetUtils.FormatTimeSeconds((int)selectedReplay.Duration.TotalSeconds);

			SetupFilters();
		}

		void SetupFilters()
		{
			//
			// Game type
			//
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_GAMETYPE_DROPDOWNBUTTON");
				if (ddb != null)
				{
					// Using list to maintain the order
					var options = new List<KeyValuePair<GameType, string>>
					{
						new KeyValuePair<GameType, string>(GameType.Any, ddb.GetText()),
						new KeyValuePair<GameType, string>(GameType.Singleplayer, "Singleplayer"),
						new KeyValuePair<GameType, string>(GameType.Multiplayer, "Multiplayer")
					};
					var lookup = options.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

					ddb.GetText = () => lookup[filter.Type];
					ddb.OnMouseDown = _ =>
					{
						Func<KeyValuePair<GameType, string>, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Type == option.Key,
								() => { filter.Type = option.Key; ApplyFilter(); }
							);
							item.Get<LabelWidget>("LABEL").GetText = () => option.Value;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count * 30, options, setupItem);
					};
				}
			}

			//
			// Date type
			//
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_DATE_DROPDOWNBUTTON");
				if (ddb != null)
				{
					// Using list to maintain the order
					var options = new List<KeyValuePair<DateType, string>>
					{
						new KeyValuePair<DateType, string>(DateType.Any, ddb.GetText()),
						new KeyValuePair<DateType, string>(DateType.Today, "Today"),
						new KeyValuePair<DateType, string>(DateType.LastWeek, "Last Week"),
						new KeyValuePair<DateType, string>(DateType.LastMonth, "Last Month")
					};
					var lookup = options.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

					ddb.GetText = () => lookup[filter.Date];
					ddb.OnMouseDown = _ =>
					{
						Func<KeyValuePair<DateType, string>, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Date == option.Key,
								() => { filter.Date = option.Key; ApplyFilter(); }
							);
							item.Get<LabelWidget>("LABEL").GetText = () => option.Value;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count * 30, options, setupItem);
					};
				}
			}

			//
			// Duration
			//
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_DURATION_DROPDOWNBUTTON");
				if (ddb != null)
				{
					// Using list to maintain the order
					var options = new List<KeyValuePair<DurationType, string>>
					{
						new KeyValuePair<DurationType, string>(DurationType.Any, ddb.GetText()),
						new KeyValuePair<DurationType, string>(DurationType.VeryShort, "Under 5 min"),
						new KeyValuePair<DurationType, string>(DurationType.Short, "Short (10 min)"),
						new KeyValuePair<DurationType, string>(DurationType.Medium, "Medium (30 min)"),
						new KeyValuePair<DurationType, string>(DurationType.Long, "Long (60+ min)")
					};
					var lookup = options.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

					ddb.GetText = () => lookup[filter.Duration];
					ddb.OnMouseDown = _ =>
					{
						Func<KeyValuePair<DurationType, string>, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Duration == option.Key,
								() => { filter.Duration = option.Key; ApplyFilter(); }
							);
							item.Get<LabelWidget>("LABEL").GetText = () => option.Value;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count * 30, options, setupItem);
					};
				}
			}

			//
			// Outcome
			//
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_OUTCOME_DROPDOWNBUTTON");
				if (ddb != null)
				{
					// Using list to maintain the order
					var options = new List<KeyValuePair<WinState, string>>
					{
						new KeyValuePair<WinState, string>(WinState.Undefined, ddb.GetText()),
						new KeyValuePair<WinState, string>(WinState.Won, "Won"),
						new KeyValuePair<WinState, string>(WinState.Lost, "Lost")
					};
					var lookup = options.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

					ddb.GetText = () => lookup[filter.Outcome];
					ddb.OnMouseDown = _ =>
					{
						Func<KeyValuePair<WinState, string>, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Outcome == option.Key,
								() => { filter.Outcome = option.Key; ApplyFilter(); }
							);
							item.Get<LabelWidget>("LABEL").GetText = () => option.Value;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count * 30, options, setupItem);
					};
				}
			}

			//
			// Players
			//
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_PLAYER_DROPDOWNBUTTON");
				if (ddb != null)
				{
					var options = new HashSet<string>(replays.SelectMany(r => r.LobbyInfo.Value.Clients.Select(c => c.Name)), StringComparer.OrdinalIgnoreCase).ToList();
					options.Sort(StringComparer.OrdinalIgnoreCase);
					options.Insert(0, null);	// no filter

					var nobodyText = ddb.GetText();
					ddb.GetText = () => string.IsNullOrEmpty(filter.PlayerName) ? nobodyText : filter.PlayerName;
					ddb.OnMouseDown = _ =>
					{
						Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => string.Compare(filter.PlayerName, option, true) == 0,
								() => { filter.PlayerName = option; ApplyFilter(); }
							);
							item.Get<LabelWidget>("LABEL").GetText = () => option ?? nobodyText;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count * 30, options, setupItem);
					};
				}
			}
		}

		bool EvaluateReplayVisibility(ReplayMetadata replay)
		{
			// Game type
			if ((filter.Type == GameType.Multiplayer && replay.LobbyInfo.Value.IsSinglePlayer) || (filter.Type == GameType.Singleplayer && !replay.LobbyInfo.Value.IsSinglePlayer))
				return false;

			// Date type
			if (filter.Date != DateType.Any)
			{
				TimeSpan t;
				switch (filter.Date)
				{
					case DateType.Today:
						t = TimeSpan.FromDays(1d);
						break;

					case DateType.LastWeek:
						t = TimeSpan.FromDays(7d);
						break;

					case DateType.LastMonth:
					default:
						t = TimeSpan.FromDays(30d);
						break;
				}
				if (replay.StartTimestampUtc < DateTime.UtcNow.Subtract(t))
					return false;
			}

			// Duration
			if (filter.Duration != DurationType.Any)
			{
				double minutes = replay.Duration.TotalMinutes;
				switch (filter.Duration)
				{
					case DurationType.VeryShort:
						if (minutes >= 5)
							return false;
						break;

					case DurationType.Short:
						if (minutes < 5 || minutes >= 20)
							return false;
						break;

					case DurationType.Medium:
						if (minutes < 20 || minutes >= 60)
							return false;
						break;

					case DurationType.Long:
						if (minutes < 60)
							return false;
						break;
				}
			}

			// Outcome
			if (filter.Outcome != WinState.Undefined && filter.Outcome != replay.Outcome)
				return false;

			// Player
			if (!string.IsNullOrEmpty(filter.PlayerName))
			{
				var player = replay.LobbyInfo.Value.Clients.Find(c => string.Compare(filter.PlayerName, c.Name, true) == 0);
				if (player == null)
					return false;
			}

			return true;
		}

		void ApplyFilter()
		{
			foreach (var replay in replays)
				replayVis[replay] = EvaluateReplayVisibility(replay);

			if (selectedReplay == null || replayVis[selectedReplay] == false)
				SelectFirstVisibleReplay();

			panel.Get<ScrollPanelWidget>("REPLAY_LIST").Layout.AdjustChildren();
		}

		void SelectFirstVisibleReplay()
		{
			SelectReplay(replays.FirstOrDefault(r => replayVis[r]));
		}

		void SelectReplay(ReplayMetadata replay)
		{
			selectedReplay = replay;
			selectedSpawns = (selectedReplay != null) ? LobbyUtils.GetSpawnClients(selectedReplay.LobbyInfo.Value, selectedReplay.MapPreview) : null;

			if (replay == null)
				return;

			try
			{
				var lobby = replay.LobbyInfo.Value;

				var clients = lobby.Clients.Where(c => c.Slot != null)
					.GroupBy(c => c.Team)
					.OrderBy(g => g.Key);

				var teams = new Dictionary<string, IEnumerable<Session.Client>>();
				var noTeams = clients.Count() == 1;
				foreach (var c in clients)
				{
					var label = noTeams ? "Players" : c.Key == 0 ? "No Team" : "Team {0}".F(c.Key);
					teams.Add(label, c);
				}

				playerList.RemoveChildren();

				foreach (var kv in teams)
				{
					var group = kv.Key;
					if (group.Length > 0)
					{
						var header = ScrollItemWidget.Setup(playerHeader, () => true, () => {});
						header.Get<LabelWidget>("LABEL").GetText = () => group;
						playerList.AddChild(header);
					}

					foreach (var option in kv.Value)
					{
						var o = option;

						var color = o.Color.RGB;

						var item = ScrollItemWidget.Setup(playerTemplate, () => false, () => { });

						var label = item.Get<LabelWidget>("LABEL");
						label.GetText = () => o.Name;
						label.GetColor = () => color;

						var flag = item.Get<ImageWidget>("FLAG");
						flag.GetImageCollection = () => "flags";
						flag.GetImageName = () => o.Country;

						playerList.AddChild(item);
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("debug", "Exception while parsing replay: {0}", e);
				SelectReplay(null);
			}
		}

		void WatchReplay()
		{
			if (selectedReplay != null)
			{
				Game.JoinReplay(selectedReplay.FilePath);
				Ui.CloseWindow();
			}
		}

		void AddReplay(ScrollPanelWidget list, ReplayMetadata replay, ScrollItemWidget template)
		{
			var item = ScrollItemWidget.Setup(template,
				() => selectedReplay == replay,
				() => SelectReplay(replay),
				() => WatchReplay());
			var f = Path.GetFileNameWithoutExtension(replay.FilePath);
			item.Get<LabelWidget>("TITLE").GetText = () => f;
			item.IsVisible = () => { bool visible; return replayVis.TryGetValue(replay, out visible) && visible; };
			list.AddChild(item);
		}

		class Filter
		{
			public GameType Type;
			public DateType Date;
			public DurationType Duration;
			public WinState Outcome = WinState.Undefined;
			public string PlayerName;
		}
		enum GameType
		{
			Any,
			Singleplayer,
			Multiplayer
		}
		enum DateType
		{
			Any,
			Today,
			LastWeek,
			LastMonth
		}
		enum DurationType
		{
			Any,
			VeryShort,
			Short,
			Medium,
			Long
		}
	}
}
