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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.FileFormats;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ReplayBrowserLogic : ChromeLogic
	{
		static Filter filter = new Filter();

		readonly Widget panel;
		readonly ScrollPanelWidget replayList, playerList;
		readonly ScrollItemWidget playerTemplate, playerHeader;
		readonly List<ReplayMetadata> replays = new List<ReplayMetadata>();
		readonly Dictionary<ReplayMetadata, ReplayState> replayState = new Dictionary<ReplayMetadata, ReplayState>();
		readonly Action onStart;
		readonly ModData modData;

		Dictionary<CPos, SpawnOccupant> selectedSpawns;
		ReplayMetadata selectedReplay;

		volatile bool cancelLoadingReplays;

		[ObjectCreator.UseCtor]
		public ReplayBrowserLogic(Widget widget, ModData modData, Action onExit, Action onStart)
		{
			panel = widget;

			this.modData = modData;
			this.onStart = onStart;
			Game.BeforeGameStart += OnGameStart;

			playerList = panel.Get<ScrollPanelWidget>("PLAYER_LIST");
			playerHeader = playerList.Get<ScrollItemWidget>("HEADER");
			playerTemplate = playerList.Get<ScrollItemWidget>("TEMPLATE");
			playerList.RemoveChildren();

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () => { cancelLoadingReplays = true; Ui.CloseWindow(); onExit(); };

			replayList = panel.Get<ScrollPanelWidget>("REPLAY_LIST");
			var template = panel.Get<ScrollItemWidget>("REPLAY_TEMPLATE");

			var mod = modData.Manifest.Mod;
			var dir = Platform.ResolvePath("^", "Replays", mod.Id, mod.Version);

			if (Directory.Exists(dir))
				ThreadPool.QueueUserWorkItem(_ => LoadReplays(dir, template));

			var watch = panel.Get<ButtonWidget>("WATCH_BUTTON");
			watch.IsDisabled = () => selectedReplay == null || selectedReplay.GameInfo.MapPreview.Status != MapStatus.Available;
			watch.OnClick = () => { WatchReplay(); };

			panel.Get("REPLAY_INFO").IsVisible = () => selectedReplay != null;

			var preview = panel.Get<MapPreviewWidget>("MAP_PREVIEW");
			preview.SpawnOccupants = () => selectedSpawns;
			preview.Preview = () => selectedReplay != null ? selectedReplay.GameInfo.MapPreview : null;

			var titleLabel = panel.GetOrNull<LabelWidget>("MAP_TITLE");
			if (titleLabel != null)
			{
				titleLabel.IsVisible = () => selectedReplay != null;

				var font = Game.Renderer.Fonts[titleLabel.Font];
				var title = new CachedTransform<MapPreview, string>(m => WidgetUtils.TruncateText(m.Title, titleLabel.Bounds.Width, font));
				titleLabel.GetText = () => title.Update(selectedReplay.GameInfo.MapPreview);
			}

			var type = panel.GetOrNull<LabelWidget>("MAP_TYPE");
			if (type != null)
			{
				var mapType = new CachedTransform<MapPreview, string>(m => m.Categories.FirstOrDefault() ?? "");
				type.GetText = () => mapType.Update(selectedReplay.GameInfo.MapPreview);
			}

			panel.Get<LabelWidget>("DURATION").GetText = () => WidgetUtils.FormatTimeSeconds((int)selectedReplay.GameInfo.Duration.TotalSeconds);

			SetupFilters();
			SetupManagement();
		}

		void LoadReplays(string dir, ScrollItemWidget template)
		{
			using (new Support.PerfTimer("Load replays"))
			{
				var loadedReplays = new ConcurrentBag<ReplayMetadata>();
				Parallel.ForEach(Directory.GetFiles(dir, "*.orarep"), (fileName, pls) =>
				{
					if (cancelLoadingReplays)
					{
						pls.Stop();
						return;
					}

					var replay = ReplayMetadata.Read(fileName);
					if (replay != null)
						loadedReplays.Add(replay);
				});

				if (cancelLoadingReplays)
					return;

				var sortedReplays = loadedReplays.OrderByDescending(replay => replay.GameInfo.StartTimeUtc).ToList();
				Game.RunAfterTick(() =>
				{
					replayList.RemoveChildren();
					foreach (var replay in sortedReplays)
						AddReplay(replay, template);

					SetupReplayDependentFilters();
					ApplyFilter();
				});
			}
		}

		void SetupFilters()
		{
			// Game type
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_GAMETYPE_DROPDOWNBUTTON");
				if (ddb != null)
				{
					// Using list to maintain the order
					var options = new List<Pair<GameType, string>>
					{
						Pair.New(GameType.Any, ddb.GetText()),
						Pair.New(GameType.Singleplayer, "Singleplayer"),
						Pair.New(GameType.Multiplayer, "Multiplayer")
					};
					var lookup = options.ToDictionary(kvp => kvp.First, kvp => kvp.Second);

					ddb.GetText = () => lookup[filter.Type];
					ddb.OnMouseDown = _ =>
					{
						Func<Pair<GameType, string>, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Type == option.First,
								() => { filter.Type = option.First; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option.Second;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, options, setupItem);
					};
				}
			}

			// Date type
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_DATE_DROPDOWNBUTTON");
				if (ddb != null)
				{
					// Using list to maintain the order
					var options = new List<Pair<DateType, string>>
					{
						Pair.New(DateType.Any, ddb.GetText()),
						Pair.New(DateType.Today, "Today"),
						Pair.New(DateType.LastWeek, "Last 7 days"),
						Pair.New(DateType.LastFortnight, "Last 14 days"),
						Pair.New(DateType.LastMonth, "Last 30 days")
					};
					var lookup = options.ToDictionary(kvp => kvp.First, kvp => kvp.Second);

					ddb.GetText = () => lookup[filter.Date];
					ddb.OnMouseDown = _ =>
					{
						Func<Pair<DateType, string>, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Date == option.First,
								() => { filter.Date = option.First; ApplyFilter(); });

							item.Get<LabelWidget>("LABEL").GetText = () => option.Second;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, options, setupItem);
					};
				}
			}

			// Duration
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_DURATION_DROPDOWNBUTTON");
				if (ddb != null)
				{
					// Using list to maintain the order
					var options = new List<Pair<DurationType, string>>
					{
						Pair.New(DurationType.Any, ddb.GetText()),
						Pair.New(DurationType.VeryShort, "Under 5 min"),
						Pair.New(DurationType.Short, "Short (10 min)"),
						Pair.New(DurationType.Medium, "Medium (30 min)"),
						Pair.New(DurationType.Long, "Long (60+ min)")
					};
					var lookup = options.ToDictionary(kvp => kvp.First, kvp => kvp.Second);

					ddb.GetText = () => lookup[filter.Duration];
					ddb.OnMouseDown = _ =>
					{
						Func<Pair<DurationType, string>, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Duration == option.First,
								() => { filter.Duration = option.First; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option.Second;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, options, setupItem);
					};
				}
			}

			// Outcome (depends on Player)
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_OUTCOME_DROPDOWNBUTTON");
				if (ddb != null)
				{
					ddb.IsDisabled = () => string.IsNullOrEmpty(filter.PlayerName);

					// Using list to maintain the order
					var options = new List<Pair<WinState, string>>
					{
						Pair.New(WinState.Undefined, ddb.GetText()),
						Pair.New(WinState.Lost, "Defeat"),
						Pair.New(WinState.Won, "Victory")
					};
					var lookup = options.ToDictionary(kvp => kvp.First, kvp => kvp.Second);

					ddb.GetText = () => lookup[filter.Outcome];
					ddb.OnMouseDown = _ =>
					{
						Func<Pair<WinState, string>, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => filter.Outcome == option.First,
								() => { filter.Outcome = option.First; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option.Second;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, options, setupItem);
					};
				}
			}

			// Reset button
			{
				var button = panel.Get<ButtonWidget>("FLT_RESET_BUTTON");
				button.IsDisabled = () => filter.IsEmpty;
				button.OnClick = () => { filter = new Filter(); ApplyFilter(); };
			}
		}

		void SetupReplayDependentFilters()
		{
			// Map
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_MAPNAME_DROPDOWNBUTTON");
				if (ddb != null)
				{
					var options = replays.Select(r => r.GameInfo.MapTitle).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
					options.Sort(StringComparer.OrdinalIgnoreCase);
					options.Insert(0, null);	// no filter

					var anyText = ddb.GetText();
					ddb.GetText = () => string.IsNullOrEmpty(filter.MapName) ? anyText : filter.MapName;
					ddb.OnMouseDown = _ =>
					{
						Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => string.Compare(filter.MapName, option, true) == 0,
								() => { filter.MapName = option; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option ?? anyText;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, options, setupItem);
					};
				}
			}

			// Players
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_PLAYER_DROPDOWNBUTTON");
				if (ddb != null)
				{
					var options = replays.SelectMany(r => r.GameInfo.Players.Select(p => p.Name)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
					options.Sort(StringComparer.OrdinalIgnoreCase);
					options.Insert(0, null);	// no filter

					var anyText = ddb.GetText();
					ddb.GetText = () => string.IsNullOrEmpty(filter.PlayerName) ? anyText : filter.PlayerName;
					ddb.OnMouseDown = _ =>
					{
						Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => string.Compare(filter.PlayerName, option, true) == 0,
								() => { filter.PlayerName = option; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option ?? anyText;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, options, setupItem);
					};
				}
			}

			// Faction (depends on Player)
			{
				var ddb = panel.GetOrNull<DropDownButtonWidget>("FLT_FACTION_DROPDOWNBUTTON");
				if (ddb != null)
				{
					ddb.IsDisabled = () => string.IsNullOrEmpty(filter.PlayerName);

					var options = replays
						.SelectMany(r => r.GameInfo.Players.Select(p => p.FactionName).Where(n => !string.IsNullOrEmpty(n)))
						.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
					options.Sort(StringComparer.OrdinalIgnoreCase);
					options.Insert(0, null);	// no filter

					var anyText = ddb.GetText();
					ddb.GetText = () => string.IsNullOrEmpty(filter.Faction) ? anyText : filter.Faction;
					ddb.OnMouseDown = _ =>
					{
						Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (option, tpl) =>
						{
							var item = ScrollItemWidget.Setup(
								tpl,
								() => string.Compare(filter.Faction, option, true) == 0,
								() => { filter.Faction = option; ApplyFilter(); });
							item.Get<LabelWidget>("LABEL").GetText = () => option ?? anyText;
							return item;
						};

						ddb.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 330, options, setupItem);
					};
				}
			}
		}

		void SetupManagement()
		{
			{
				var button = panel.Get<ButtonWidget>("MNG_RENSEL_BUTTON");
				button.IsDisabled = () => selectedReplay == null;
				button.OnClick = () =>
				{
					var r = selectedReplay;
					var initialName = Path.GetFileNameWithoutExtension(r.FilePath);
					var directoryName = Path.GetDirectoryName(r.FilePath);
					var invalidChars = Path.GetInvalidFileNameChars();

					ConfirmationDialogs.TextInputPrompt(
						"Rename Replay",
						"Enter a new file name:",
						initialName,
						onAccept: newName => RenameReplay(r, newName),
						onCancel: null,
						acceptText: "Rename",
						cancelText: null,
						inputValidator: newName =>
						{
							if (newName == initialName)
								return false;

							if (string.IsNullOrWhiteSpace(newName))
								return false;

							if (newName.IndexOfAny(invalidChars) >= 0)
								return false;

							if (File.Exists(Path.Combine(directoryName, newName)))
								return false;

							return true;
						});
				};
			}

			Action<ReplayMetadata, Action> onDeleteReplay = (r, after) =>
			{
				ConfirmationDialogs.ButtonPrompt(
					title: "Delete selected replay?",
					text: "Delete replay '{0}'?".F(Path.GetFileNameWithoutExtension(r.FilePath)),
					onConfirm: () =>
					{
						DeleteReplay(r);
						if (after != null)
							after.Invoke();
					},
					confirmText: "Delete",
					onCancel: () => { });
			};

			{
				var button = panel.Get<ButtonWidget>("MNG_DELSEL_BUTTON");
				button.IsDisabled = () => selectedReplay == null;
				button.OnClick = () =>
				{
					onDeleteReplay(selectedReplay, () =>
					{
						if (selectedReplay == null)
							SelectFirstVisibleReplay();
					});
				};
			}

			{
				var button = panel.Get<ButtonWidget>("MNG_DELALL_BUTTON");
				button.IsDisabled = () => replayState.Count(kvp => kvp.Value.Visible) == 0;
				button.OnClick = () =>
				{
					var list = replayState.Where(kvp => kvp.Value.Visible).Select(kvp => kvp.Key).ToList();
					if (list.Count == 0)
						return;

					if (list.Count == 1)
					{
						onDeleteReplay(list[0], () => { if (selectedReplay == null) SelectFirstVisibleReplay(); });
						return;
					}

					ConfirmationDialogs.ButtonPrompt(
						title: "Delete all selected replays?",
						text: "Delete {0} replays?".F(list.Count),
						onConfirm: () =>
						{
							list.ForEach(DeleteReplay);
							if (selectedReplay == null)
								SelectFirstVisibleReplay();
						},
						confirmText: "Delete All",
						onCancel: () => { });
				};
			}
		}

		void RenameReplay(ReplayMetadata replay, string newFilenameWithoutExtension)
		{
			try
			{
				replay.RenameFile(newFilenameWithoutExtension);
				replayState[replay].Item.Text = newFilenameWithoutExtension;
			}
			catch (Exception ex)
			{
				Log.Write("debug", ex.ToString());
				return;
			}
		}

		void DeleteReplay(ReplayMetadata replay)
		{
			try
			{
				File.Delete(replay.FilePath);
			}
			catch (Exception ex)
			{
				Game.Debug("Failed to delete replay file '{0}'. See the logs for details.", replay.FilePath);
				Log.Write("debug", ex.ToString());
				return;
			}

			if (replay == selectedReplay)
				SelectReplay(null);

			replayList.RemoveChild(replayState[replay].Item);
			replays.Remove(replay);
			replayState.Remove(replay);
		}

		bool EvaluateReplayVisibility(ReplayMetadata replay)
		{
			// Game type
			if ((filter.Type == GameType.Multiplayer && replay.GameInfo.IsSinglePlayer) || (filter.Type == GameType.Singleplayer && !replay.GameInfo.IsSinglePlayer))
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

					case DateType.LastFortnight:
						t = TimeSpan.FromDays(14d);
						break;

					case DateType.LastMonth:
					default:
						t = TimeSpan.FromDays(30d);
						break;
				}

				if (replay.GameInfo.StartTimeUtc < DateTime.UtcNow - t)
					return false;
			}

			// Duration
			if (filter.Duration != DurationType.Any)
			{
				var minutes = replay.GameInfo.Duration.TotalMinutes;
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

			// Map
			if (!string.IsNullOrEmpty(filter.MapName) && string.Compare(filter.MapName, replay.GameInfo.MapTitle, true) != 0)
				return false;

			// Player
			if (!string.IsNullOrEmpty(filter.PlayerName))
			{
				var player = replay.GameInfo.Players.FirstOrDefault(p => string.Compare(filter.PlayerName, p.Name, true) == 0);
				if (player == null)
					return false;

				// Outcome
				if (filter.Outcome != WinState.Undefined && filter.Outcome != player.Outcome)
					return false;

				// Faction
				if (!string.IsNullOrEmpty(filter.Faction) && string.Compare(filter.Faction, player.FactionName, true) != 0)
					return false;
			}

			return true;
		}

		void ApplyFilter()
		{
			foreach (var replay in replays)
				replayState[replay].Visible = EvaluateReplayVisibility(replay);

			if (selectedReplay == null || replayState[selectedReplay].Visible == false)
				SelectFirstVisibleReplay();

			replayList.Layout.AdjustChildren();
		}

		void SelectFirstVisibleReplay()
		{
			SelectReplay(replays.FirstOrDefault(r => replayState[r].Visible));
		}

		void SelectReplay(ReplayMetadata replay)
		{
			selectedReplay = replay;
			selectedSpawns = (selectedReplay != null)
				? LobbyUtils.GetSpawnOccupants(selectedReplay.GameInfo.Players, selectedReplay.GameInfo.MapPreview)
				: new Dictionary<CPos, SpawnOccupant>();

			if (replay == null)
				return;

			try
			{
				var players = replay.GameInfo.Players
					.GroupBy(p => p.Team)
					.OrderBy(g => g.Key);

				var teams = new Dictionary<string, IEnumerable<GameInformation.Player>>();
				var noTeams = players.Count() == 1;
				foreach (var p in players)
				{
					var label = noTeams ? "Players" : p.Key == 0 ? "No Team" : "Team {0}".F(p.Key);
					teams.Add(label, p);
				}

				playerList.RemoveChildren();

				foreach (var kv in teams)
				{
					var group = kv.Key;
					if (group.Length > 0)
					{
						var header = ScrollItemWidget.Setup(playerHeader, () => true, () => { });
						header.Get<LabelWidget>("LABEL").GetText = () => group;
						playerList.AddChild(header);
					}

					foreach (var option in kv.Value)
					{
						var o = option;

						var color = o.Color.RGB;

						var item = ScrollItemWidget.Setup(playerTemplate, () => false, () => { });

						var label = item.Get<LabelWidget>("LABEL");
						var font = Game.Renderer.Fonts[label.Font];
						var name = WidgetUtils.TruncateText(o.Name, label.Bounds.Width, font);
						label.GetText = () => name;
						label.GetColor = () => color;

						var flag = item.Get<ImageWidget>("FLAG");
						flag.GetImageCollection = () => "flags";
						var factionInfo = modData.DefaultRules.Actors["world"].TraitInfos<FactionInfo>();
						flag.GetImageName = () => (factionInfo != null && factionInfo.Any(f => f.InternalName == o.FactionId)) ? o.FactionId : "Random";

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
			if (selectedReplay != null && ReplayUtils.PromptConfirmReplayCompatibility(selectedReplay))
			{
				cancelLoadingReplays = true;
				Game.JoinReplay(selectedReplay.FilePath);
			}
		}

		void AddReplay(ReplayMetadata replay, ScrollItemWidget template)
		{
			replays.Add(replay);

			var item = ScrollItemWidget.Setup(template,
				() => selectedReplay == replay,
				() => SelectReplay(replay),
				() => WatchReplay());

			replayState[replay] = new ReplayState
			{
				Item = item,
				Visible = true
			};

			item.Text = Path.GetFileNameWithoutExtension(replay.FilePath);
			item.Get<LabelWidget>("TITLE").GetText = () => item.Text;
			item.IsVisible = () => replayState[replay].Visible;
			replayList.AddChild(item);
		}

		void OnGameStart()
		{
			Ui.CloseWindow();
			onStart();
		}

		bool disposed;
		protected override void Dispose(bool disposing)
		{
			if (disposing && !disposed)
			{
				disposed = true;
				Game.BeforeGameStart -= OnGameStart;
			}

			base.Dispose(disposing);
		}

		class ReplayState
		{
			public bool Visible;
			public ScrollItemWidget Item;
		}

		class Filter
		{
			public GameType Type;
			public DateType Date;
			public DurationType Duration;
			public WinState Outcome;
			public string PlayerName;
			public string MapName;
			public string Faction;

			public bool IsEmpty
			{
				get
				{
					return Type == default(GameType)
						&& Date == default(DateType)
						&& Duration == default(DurationType)
						&& Outcome == default(WinState)
						&& string.IsNullOrEmpty(PlayerName)
						&& string.IsNullOrEmpty(MapName)
						&& string.IsNullOrEmpty(Faction);
				}
			}
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
			LastFortnight,
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
