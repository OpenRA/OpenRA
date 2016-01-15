#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapChooserLogic : ChromeLogic
	{
		readonly Widget widget;
		readonly DropDownButtonWidget gameModeDropdown;

		MapClassification currentTab;

		Dictionary<MapClassification, ScrollPanelWidget> scrollpanels = new Dictionary<MapClassification, ScrollPanelWidget>();

		Dictionary<MapClassification, MapPreview[]> tabMaps = new Dictionary<MapClassification, MapPreview[]>();
		string[] visibleMaps;

		string selectedUid;
		Action<string> onSelect;

		string gameMode;
		string mapFilter;

		[ObjectCreator.UseCtor]
		internal MapChooserLogic(Widget widget, string initialMap, MapClassification initialTab, Action onExit, Action<string> onSelect, MapVisibility filter)
		{
			this.widget = widget;
			this.onSelect = onSelect;

			var approving = new Action(() => { Ui.CloseWindow(); onSelect(selectedUid); });
			var canceling = new Action(() => { Ui.CloseWindow(); onExit(); });

			var okButton = widget.Get<ButtonWidget>("BUTTON_OK");
			okButton.Disabled = this.onSelect == null;
			okButton.OnClick = approving;
			widget.Get<ButtonWidget>("BUTTON_CANCEL").OnClick = canceling;

			gameModeDropdown = widget.GetOrNull<DropDownButtonWidget>("GAMEMODE_FILTER");

			var itemTemplate = widget.Get<ScrollItemWidget>("MAP_TEMPLATE");
			widget.RemoveChild(itemTemplate);

			var mapFilterInput = widget.GetOrNull<TextFieldWidget>("MAPFILTER_INPUT");
			if (mapFilterInput != null)
			{
				mapFilterInput.TakeKeyboardFocus();
				mapFilterInput.OnEscKey = () =>
				{
					if (mapFilterInput.Text.Length == 0)
						canceling();
					else
					{
						mapFilter = mapFilterInput.Text = null;
						EnumerateMaps(currentTab, itemTemplate);
					}

					return true;
				};
				mapFilterInput.OnEnterKey = () => { approving(); return true; };
				mapFilterInput.OnTextEdited = () =>
				{
					mapFilter = mapFilterInput.Text;
					EnumerateMaps(currentTab, itemTemplate);
				};
			}

			var randomMapButton = widget.GetOrNull<ButtonWidget>("RANDOMMAP_BUTTON");
			if (randomMapButton != null)
			{
				randomMapButton.OnClick = () =>
				{
					var uid = visibleMaps.Random(Game.CosmeticRandom);
					selectedUid = uid;
					scrollpanels[currentTab].ScrollToItem(uid, smooth: true);
				};
				randomMapButton.IsDisabled = () => visibleMaps == null || visibleMaps.Length == 0;
			}

			var deleteMapButton = widget.Get<ButtonWidget>("DELETE_MAP_BUTTON");
			deleteMapButton.IsDisabled = () => Game.ModData.MapCache[selectedUid].Class != MapClassification.User;
			deleteMapButton.IsVisible = () => currentTab == MapClassification.User;
			deleteMapButton.OnClick = () =>
			{
				DeleteOneMap(selectedUid, (string newUid) =>
				{
					RefreshMaps(currentTab, filter);
					EnumerateMaps(currentTab, itemTemplate);
					if (!tabMaps[currentTab].Any())
						SwitchTab(Game.ModData.MapCache[newUid].Class, itemTemplate);
				});
			};

			var deleteAllMapsButton = widget.Get<ButtonWidget>("DELETE_ALL_MAPS_BUTTON");
			deleteAllMapsButton.IsVisible = () => currentTab == MapClassification.User;
			deleteAllMapsButton.OnClick = () =>
			{
				DeleteAllMaps(visibleMaps, (string newUid) =>
				{
					RefreshMaps(currentTab, filter);
					EnumerateMaps(currentTab, itemTemplate);
					SwitchTab(Game.ModData.MapCache[newUid].Class, itemTemplate);
				});
			};

			SetupMapTab(MapClassification.User, filter, "USER_MAPS_TAB_BUTTON", "USER_MAPS_TAB", itemTemplate);
			SetupMapTab(MapClassification.System, filter, "SYSTEM_MAPS_TAB_BUTTON", "SYSTEM_MAPS_TAB", itemTemplate);

			if (initialMap == null && tabMaps.Keys.Contains(initialTab) && tabMaps[initialTab].Any())
			{
				selectedUid = WidgetUtils.ChooseInitialMap(tabMaps[initialTab].Select(mp => mp.Uid).First());
				currentTab = initialTab;
			}
			else
			{
				selectedUid = WidgetUtils.ChooseInitialMap(initialMap);
				currentTab = tabMaps.Keys.FirstOrDefault(k => tabMaps[k].Select(mp => mp.Uid).Contains(selectedUid));
			}

			SwitchTab(currentTab, itemTemplate);
		}

		void SwitchTab(MapClassification tab, ScrollItemWidget itemTemplate)
		{
			currentTab = tab;
			EnumerateMaps(tab, itemTemplate);
		}

		void RefreshMaps(MapClassification tab, MapVisibility filter)
		{
			tabMaps[tab] = Game.ModData.MapCache.Where(m => m.Status == MapStatus.Available &&
				m.Class == tab && (m.Map.Visibility & filter) != 0).ToArray();
		}

		void SetupMapTab(MapClassification tab, MapVisibility filter, string tabButtonName, string tabContainerName, ScrollItemWidget itemTemplate)
		{
			var tabContainer = widget.Get<ContainerWidget>(tabContainerName);
			tabContainer.IsVisible = () => currentTab == tab;
			var tabScrollpanel = tabContainer.Get<ScrollPanelWidget>("MAP_LIST");
			tabScrollpanel.Layout = new GridLayout(tabScrollpanel);
			scrollpanels.Add(tab, tabScrollpanel);

			var tabButton = widget.Get<ButtonWidget>(tabButtonName);
			tabButton.IsHighlighted = () => currentTab == tab;
			tabButton.IsVisible = () => tabMaps[tab].Any();
			tabButton.OnClick = () => SwitchTab(tab, itemTemplate);

			RefreshMaps(tab, filter);
		}

		void SetupGameModeDropdown(MapClassification tab, DropDownButtonWidget gameModeDropdown, ScrollItemWidget itemTemplate)
		{
			if (gameModeDropdown != null)
			{
				var gameModes = tabMaps[tab]
					.GroupBy(m => m.Type)
					.Select(g => Pair.New(g.Key, g.Count())).ToList();

				// 'all game types' extra item
				gameModes.Insert(0, Pair.New(null as string, tabMaps[tab].Count()));

				Func<Pair<string, int>, string> showItem = x => "{0} ({1})".F(x.First ?? "All Game Types", x.Second);

				Func<Pair<string, int>, ScrollItemWidget, ScrollItemWidget> setupItem = (ii, template) =>
				{
					var item = ScrollItemWidget.Setup(template,
						() => gameMode == ii.First,
						() => { gameMode = ii.First; EnumerateMaps(tab, itemTemplate); });
					item.Get<LabelWidget>("LABEL").GetText = () => showItem(ii);
					return item;
				};

				gameModeDropdown.OnClick = () =>
					gameModeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, gameModes, setupItem);

				gameModeDropdown.GetText = () =>
				{
					var item = gameModes.FirstOrDefault(m => m.First == gameMode);
					if (item == default(Pair<string, int>))
						item.First = "No matches";

					return showItem(item);
				};
			}
		}

		void EnumerateMaps(MapClassification tab, ScrollItemWidget template)
		{
			int playerCountFilter;
			if (!int.TryParse(mapFilter, out playerCountFilter))
				playerCountFilter = -1;

			var maps = tabMaps[tab]
				.Where(m => gameMode == null || m.Type == gameMode)
				.Where(m => mapFilter == null ||
					(m.Title != null && m.Title.IndexOf(mapFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
					(m.Author != null && m.Author.IndexOf(mapFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
					m.PlayerCount == playerCountFilter)
				.OrderBy(m => m.PlayerCount)
				.ThenBy(m => m.Title);

			scrollpanels[tab].RemoveChildren();
			foreach (var loop in maps)
			{
				var preview = loop;

				// Access the minimap to trigger async generation of the minimap.
				preview.GetMinimap();

				Action dblClick = () =>
				{
					if (onSelect != null)
					{
						Ui.CloseWindow();
						onSelect(preview.Uid);
					}
				};

				var item = ScrollItemWidget.Setup(preview.Uid, template, () => selectedUid == preview.Uid,
					() => selectedUid = preview.Uid, dblClick);
				item.IsVisible = () => item.RenderBounds.IntersectsWith(scrollpanels[tab].RenderBounds);

				var titleLabel = item.Get<LabelWidget>("TITLE");
				if (titleLabel != null)
				{
					var font = Game.Renderer.Fonts[titleLabel.Font];
					var title = WidgetUtils.TruncateText(preview.Title, titleLabel.Bounds.Width, font);
					titleLabel.GetText = () => title;
				}

				var previewWidget = item.Get<MapPreviewWidget>("PREVIEW");
				previewWidget.Preview = () => preview;

				var detailsWidget = item.GetOrNull<LabelWidget>("DETAILS");
				if (detailsWidget != null)
					detailsWidget.GetText = () => "{0} ({1} players)".F(preview.Type, preview.PlayerCount);

				var authorWidget = item.GetOrNull<LabelWidget>("AUTHOR");
				if (authorWidget != null)
				{
					var font = Game.Renderer.Fonts[authorWidget.Font];
					var author = WidgetUtils.TruncateText("Created by {0}".F(preview.Author), authorWidget.Bounds.Width, font);
					authorWidget.GetText = () => author;
				}

				var sizeWidget = item.GetOrNull<LabelWidget>("SIZE");
				if (sizeWidget != null)
				{
					var size = preview.Bounds.Width + "x" + preview.Bounds.Height;
					var numberPlayableCells = preview.Bounds.Width * preview.Bounds.Height;
					if (numberPlayableCells >= 120 * 120) size += " (Huge)";
					else if (numberPlayableCells >= 90 * 90) size += " (Large)";
					else if (numberPlayableCells >= 60 * 60) size += " (Medium)";
					else size += " (Small)";
					sizeWidget.GetText = () => size;
				}

				scrollpanels[tab].AddChild(item);
			}

			if (tab == currentTab)
			{
				visibleMaps = maps.Select(m => m.Uid).ToArray();
				SetupGameModeDropdown(currentTab, gameModeDropdown, template);
			}

			if (visibleMaps.Contains(selectedUid))
				scrollpanels[tab].ScrollToItem(selectedUid);
		}

		string DeleteMap(string map)
		{
			var path = Game.ModData.MapCache[map].Map.Path;
			try
			{
				if (File.Exists(path))
					File.Delete(path);
				else if (Directory.Exists(path))
					Directory.Delete(path, true);

				Game.ModData.MapCache[map].Invalidate();

				if (selectedUid == map)
					selectedUid = WidgetUtils.ChooseInitialMap(tabMaps[currentTab].Select(mp => mp.Uid).FirstOrDefault());
			}
			catch (Exception ex)
			{
				Game.Debug("Failed to delete map '{0}'. See the debug.log file for details.", path);
				Log.Write("debug", ex.ToString());
			}

			return selectedUid;
		}

		void DeleteOneMap(string map, Action<string> after)
		{
			ConfirmationDialogs.PromptConfirmAction(
				title: "Delete map",
				text: "Delete the map '{0}'?".F(Game.ModData.MapCache[map].Title),
				onConfirm: () =>
				{
					var newUid = DeleteMap(map);
					if (after != null)
						after(newUid);
				},
				confirmText: "Delete");
		}

		void DeleteAllMaps(string[] maps, Action<string> after)
		{
			ConfirmationDialogs.PromptConfirmAction(
				title: "Delete maps",
				text: "Delete all maps on this page?",
				onConfirm: () =>
				{
					maps.Do(m => DeleteMap(m));
					if (after != null)
						after(WidgetUtils.ChooseInitialMap(null));
				},
				confirmText: "Delete");
		}
	}
}
