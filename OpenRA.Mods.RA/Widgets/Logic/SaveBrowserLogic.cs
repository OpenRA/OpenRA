#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class SaveBrowserLogic
	{
		Widget panel;
		ScrollPanelWidget saveList, playerList;
		ScrollItemWidget playerTemplate, playerHeader;
		List<ReplayMetadata> saves;
		Dictionary<ReplayMetadata, SaveState> saveState = new Dictionary<ReplayMetadata, SaveState>();
		ScrollPanelWidget descriptionPanel;
		Action<ReplayMetadata> onLoad;
		LabelWidget descriptionLabel;
		SpriteFont descriptionFont;

		Dictionary<CPos, SpawnOccupant> selectedSpawns;
		ReplayMetadata selectedSave;

		[ObjectCreator.UseCtor]
		public SaveBrowserLogic(Widget widget, Action onExit, Action<ReplayMetadata> onLoad)
		{
			this.onLoad = onLoad;
			panel = widget;

			playerList = panel.Get<ScrollPanelWidget>("PLAYER_LIST");
			playerHeader = playerList.Get<ScrollItemWidget>("HEADER");
			playerTemplate = playerList.Get<ScrollItemWidget>("TEMPLATE");
			playerList.RemoveChildren();

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			saveList = panel.Get<ScrollPanelWidget>("SAVE_LIST");
			var template = panel.Get<ScrollItemWidget>("SAVE_TEMPLATE");
			descriptionPanel = panel.Get<ScrollPanelWidget>("SAVE_DESCRIPTION_PANEL");
			descriptionLabel = descriptionPanel.Get<LabelWidget>("SAVE_DESCRIPTION");
			descriptionFont = Game.Renderer.Fonts[descriptionLabel.Font];

			var mod = Game.modData.Manifest.Mod;
			var dir = new[] { Platform.SupportDir, "Saves", mod.Id, mod.Version }.Aggregate(Path.Combine);

			saveList.RemoveChildren();
			if (Directory.Exists(dir))
			{
				using (new Support.PerfTimer("Load saves"))
				{
					saves = Directory
						.GetFiles(dir, "*.orasave")
						.Select(ReplayMetadata.Read)
						.Where(r => r != null)
						.OrderByDescending(r => r.GameInfo.StartTimeUtc)
						.ToList();
				}

				foreach (var save in saves)
					AddSave(save, template);

				SelectFirstVisibleSave();
			}
			else
				saves = new List<ReplayMetadata>();

			var load = panel.Get<ButtonWidget>("LOAD_BUTTON");
			load.IsDisabled = () => selectedSave == null || selectedSave.GameInfo.MapPreview.Status != MapStatus.Available;
			load.OnClick = () => { Ui.CloseWindow(); onLoad(selectedSave); };

			panel.Get("SAVE_INFO").IsVisible = () => selectedSave != null;

			var preview = panel.Get<MapPreviewWidget>("MAP_PREVIEW");
			preview.SpawnOccupants = () => selectedSpawns;
			preview.Preview = () => selectedSave != null ? selectedSave.GameInfo.MapPreview : null;

			var title = panel.GetOrNull<LabelWidget>("MAP_TITLE");
			if (title != null)
				title.GetText = () => selectedSave != null ? selectedSave.GameInfo.MapPreview.Title : null;

			var type = panel.GetOrNull<LabelWidget>("MAP_TYPE");
			if (type != null)
				type.GetText = () => selectedSave.GameInfo.MapPreview.Type;

			panel.Get<LabelWidget>("DURATION").GetText = () => WidgetUtils.FormatTimeSeconds((int)selectedSave.GameInfo.Duration.TotalSeconds);

			SetupManagement();
		}

		void ApplyFilter(GameType type)
		{
			foreach (var save in saves)
				saveState[save].Visible = EvaluateSaveVisibility(save, type);

			if (selectedSave == null || saveState[selectedSave].Visible == false)
				SelectFirstVisibleSave();

			saveList.Layout.AdjustChildren();
		}

		bool EvaluateSaveVisibility(ReplayMetadata replay, GameType type)
		{
			// Game type
			if ((type == GameType.Multiplayer && replay.GameInfo.IsSinglePlayer) || (type == GameType.Singleplayer && !replay.GameInfo.IsSinglePlayer))
				return false;
			return true;
		}

		void SetupManagement()
		{
			{
				var button = panel.Get<ButtonWidget>("MNG_RENSEL_BUTTON");
				button.IsDisabled = () => selectedSave == null;
				button.OnClick = () =>
				{
					var r = selectedSave;
					var initialName = Path.GetFileNameWithoutExtension(r.FilePath);
					var directoryName = Path.GetDirectoryName(r.FilePath);
					var invalidChars = Path.GetInvalidFileNameChars();

					ConfirmationDialogs.TextInputPrompt(
						"Rename Save",
						"Enter a new file name:",
						initialName,
						onAccept: newName => RenameSave(r, newName),
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

			Action<ReplayMetadata, Action> onDeleteSave = (r, after) =>
			{
				ConfirmationDialogs.PromptConfirmAction(
					"Delete selected save?",
					"Delete save '{0}'?".F(Path.GetFileNameWithoutExtension(r.FilePath)),
					() =>
					{
						DeleteSave(r);
						if (after != null)
							after.Invoke();
					},
					null,
					"Delete");
			};

			{
				var button = panel.Get<ButtonWidget>("MNG_DELSEL_BUTTON");
				button.IsDisabled = () => selectedSave == null;
				button.OnClick = () =>
				{
					onDeleteSave(selectedSave, () =>
					{
						if (selectedSave == null)
							SelectFirstVisibleSave();
					});
				};
			}

			{
				var button = panel.Get<ButtonWidget>("MNG_DELALL_BUTTON");
				button.IsDisabled = () => saveState.Count(kvp => kvp.Value.Visible) == 0;
				button.OnClick = () =>
				{
					var list = saveState.Where(kvp => kvp.Value.Visible).Select(kvp => kvp.Key).ToList();
					if (list.Count == 0)
						return;

					if (list.Count == 1)
					{
						onDeleteSave(list[0], () => { if (selectedSave == null) SelectFirstVisibleSave(); });
						return;
					}

					ConfirmationDialogs.PromptConfirmAction(
						"Delete all selected saves?",
						"Delete {0} saves?".F(list.Count),
						() =>
						{
							list.ForEach(DeleteSave);
							if (selectedSave == null)
								SelectFirstVisibleSave();
						},
						null,
						"Delete All");
				};
			}
		}

		void RenameSave(ReplayMetadata save, string newFilenameWithoutExtension)
		{
			try
			{
				save.RenameFile(newFilenameWithoutExtension);
				saveState[save].Item.Text = newFilenameWithoutExtension;
			}
			catch (Exception ex)
			{
				Log.Write("debug", ex.ToString());
				return;
			}
		}

		void DeleteSave(ReplayMetadata save)
		{
			try
			{
				File.Delete(save.FilePath);
			}
			catch (Exception ex)
			{
				Game.Debug("Failed to delete save file '{0}'. See the logs for details.", save.FilePath);
				Log.Write("debug", ex.ToString());
				return;
			}

			if (save == selectedSave)
				SelectSave(null);

			saveList.RemoveChild(saveState[save].Item);
			saves.Remove(save);
			saveState.Remove(save);
		}

		void SelectFirstVisibleSave()
		{
			SelectSave(saves.FirstOrDefault(s => saveState[s].Visible));
		}

		void SelectSave(ReplayMetadata save)
		{
			selectedSave = save;
			selectedSpawns = (selectedSave != null)
				? LobbyUtils.GetSpawnOccupants(selectedSave.GameInfo.Players, selectedSave.GameInfo.MapPreview)
				: new Dictionary<CPos, SpawnOccupant>();

			if (save == null)
				return;

			try
			{
				var players = save.GameInfo.Players
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
						label.GetText = () => o.Name;
						label.GetColor = () => color;

						var flag = item.Get<ImageWidget>("FLAG");
						flag.GetImageCollection = () => "flags";
						flag.GetImageName = () => o.FactionId;

						playerList.AddChild(item);
					}
				}

				if (selectedSave.GameInfo.MapPreview.Status == MapStatus.Available)
				{
					var text = selectedSave.GameInfo.MapPreview.Map.Description != null ? selectedSave.GameInfo.MapPreview.Map.Description.Replace("\\n", "\n") : "";
					text = WidgetUtils.WrapText(text, descriptionLabel.Bounds.Width, descriptionFont);
					descriptionLabel.Text = text;
					descriptionLabel.Bounds.Height = descriptionFont.Measure(text).Y;
					descriptionPanel.ScrollToTop();
					descriptionPanel.Layout.AdjustChildren();
				}
			}
			catch (Exception e)
			{
				Log.Write("debug", "Exception while parsing save: {0}", e);
				SelectSave(null);
			}
		}

		void AddSave(ReplayMetadata save, ScrollItemWidget template)
		{
			var item = ScrollItemWidget.Setup(template,
				() => selectedSave == save,
				() => SelectSave(save),
				() => { Ui.CloseWindow(); onLoad(save); });

			saveState[save] = new SaveState
			{
				Item = item,
				Visible = true
			};

			item.Text = Path.GetFileNameWithoutExtension(save.FilePath);
			item.Get<LabelWidget>("TITLE").GetText = () => item.Text;
			item.IsVisible = () => saveState[save].Visible;
			saveList.AddChild(item);
		}

		class SaveState
		{
			public bool Visible;
			public ScrollItemWidget Item;
		}

		public enum GameType
		{
			Any,
			Singleplayer,
			Multiplayer
		}
	}
}
