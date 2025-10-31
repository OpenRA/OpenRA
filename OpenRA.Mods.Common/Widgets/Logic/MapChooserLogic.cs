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
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapChooserLogic : ChromeLogic
	{
		[FluentReference]
		const string AllMaps = "label-all-maps";

		[FluentReference]
		const string NoMatches = "label-no-matches";

		[FluentReference("players")]
		const string Players = "label-player-count";

		[FluentReference("author")]
		const string CreatedBy = "label-created-by";

		[FluentReference]
		const string MapSizeHuge = "label-map-size-huge";

		[FluentReference]
		const string MapSizeLarge = "label-map-size-large";

		[FluentReference]
		const string MapSizeMedium = "label-map-size-medium";

		[FluentReference]
		const string MapSizeSmall = "label-map-size-small";

		[FluentReference("count")]
		const string MapSearchingCount = "label-map-searching-count";

		[FluentReference("count")]
		const string MapUnavailableCount = "label-map-unavailable-count";

		[FluentReference("map")]
		const string MapDeletionFailed = "notification-map-deletion-failed";

		[FluentReference]
		const string DeleteMapTitle = "dialog-delete-map.title";

		[FluentReference("title")]
		const string DeleteMapPrompt = "dialog-delete-map.prompt";

		[FluentReference]
		const string DeleteMapAccept = "dialog-delete-map.confirm";

		[FluentReference]
		const string DeleteAllMapsTitle = "dialog-delete-all-maps.title";

		[FluentReference]
		const string DeleteAllMapsPrompt = "dialog-delete-all-maps.prompt";

		[FluentReference]
		const string DeleteAllMapsAccept = "dialog-delete-all-maps.confirm";

		[FluentReference]
		const string OrderMapsByPlayers = "options-order-maps.player-count";

		[FluentReference]
		const string OrderMapsByTitle = "options-order-maps.title";

		[FluentReference]
		const string OrderMapsByDate = "options-order-maps.date";

		[FluentReference]
		const string OrderMapsBySize = "options-order-maps.size";

		[FluentReference]
		const string SystemMapsTab = "button-mapchooser-system-maps-tab";

		[FluentReference]
		const string UserMapsTab = "button-mapchooser-user-maps-tab";

		[FluentReference]
		const string RemoteMapsTab = "button-mapchooser-remote-maps-tab";

		[FluentReference]
		const string GeneratedMapsTab = "button-mapchooser-generated-maps-tab";

		public static string MapSizeLabel(Size size)
		{
			var area = size.Width * size.Height;
			var label = area >= 120 * 120 ? MapSizeHuge :
				area >= 90 * 90 ? MapSizeLarge :
				area >= 60 * 60 ? MapSizeMedium :
				MapSizeSmall;

			return $"{size.Width}x{size.Height} ({FluentProvider.GetMessage(label)})";
		}

		readonly string allMaps;

		readonly Widget widget;
		readonly DropDownButtonWidget gameModeDropdown;
		readonly ModData modData;
		readonly HashSet<string> remoteMapPool;
		readonly ScrollItemWidget itemTemplate;
		readonly MapVisibility filter;

		MapClassification currentTab;
		bool disposed;
		int remoteSearching = 0;
		int remoteUnavailable = 0;

		readonly Dictionary<MapClassification, ScrollPanelWidget> scrollpanels = [];
		readonly Dictionary<MapClassification, MapPreview[]> tabMaps = [];
		readonly Dictionary<MapClassification, string> tabLabels = [];

		string[] visibleMaps;

		string selectedUid;
		readonly Action<string> onSelect;
		MapGenerationArgs generatedMapArgs;
		IReadWritePackage generatedMapPackage;

		string category;
		string mapFilter;

		Func<MapPreview, long> orderByFunc;

		[ObjectCreator.UseCtor]
		internal MapChooserLogic(Widget widget, ModData modData, string initialMap, MapGenerationArgs initialGeneratedMap, HashSet<string> remoteMapPool,
			MapClassification initialTab, Action onExit, Action<string> onSelect, Action<MapGenerationArgs> onSelectGenerated, MapVisibility filter)
		{
			this.widget = widget;
			this.modData = modData;
			this.onSelect = onSelect;
			this.remoteMapPool = remoteMapPool;
			this.filter = filter;

			allMaps = FluentProvider.GetMessage(AllMaps);

			var approving = new Action(() =>
			{
				Ui.CloseWindow();
				if (currentTab == MapClassification.Generated && generatedMapArgs != null)
				{
					// PERF: Add the map directly into the map cache to allow an instant map switch for the local player
					var p = modData.MapCache[generatedMapArgs.Uid];
					if (p.Status != MapStatus.Available && generatedMapPackage is ZipFileLoader.ReadWriteZipFile zipPackage)
					{
						// The original package will be disposed, so take a deep copy
						var package = ZipFileLoader.ReadWriteZipFile.FromBase64String(zipPackage.ToBase64String());
						p.UpdateFromMap(package, MapClassification.Generated);
					}

					onSelectGenerated?.Invoke(generatedMapArgs);
				}
				else
					onSelect?.Invoke(selectedUid);
			});

			var canceling = new Action(() => { Ui.CloseWindow(); onExit(); });

			var okButton = widget.Get<ButtonWidget>("BUTTON_OK");
			if (onSelect != null)
				okButton.IsDisabled = () => currentTab == MapClassification.Generated && generatedMapArgs == null;
			else
				okButton.Disabled = true;

			okButton.OnClick = approving;
			widget.Get<ButtonWidget>("BUTTON_CANCEL").OnClick = canceling;

			gameModeDropdown = widget.GetOrNull<DropDownButtonWidget>("GAMEMODE_FILTER");

			itemTemplate = widget.Get<ScrollItemWidget>("MAP_TEMPLATE");
			widget.RemoveChild(itemTemplate);

			SetupOrderByDropdown();

			var filterContainer = widget.GetOrNull("FILTER_ORDER_CONTROLS");
			if (filterContainer != null)
				filterContainer.IsVisible = () => currentTab != MapClassification.Generated;

			var mapFilterInput = widget.GetOrNull<TextFieldWidget>("MAPFILTER_INPUT");
			if (mapFilterInput != null)
			{
				mapFilterInput.TakeKeyboardFocus();
				mapFilterInput.OnEscKey = _ =>
				{
					if (mapFilterInput.Text.Length == 0)
						canceling();
					else
					{
						mapFilter = mapFilterInput.Text = null;
						EnumerateMaps(currentTab);
					}

					return true;
				};
				mapFilterInput.OnEnterKey = _ => { approving(); return true; };
				mapFilterInput.OnTextEdited = () =>
				{
					mapFilter = mapFilterInput.Text;
					EnumerateMaps(currentTab);
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
				randomMapButton.IsVisible = () => currentTab != MapClassification.Generated;
			}

			var deleteMapButton = widget.Get<ButtonWidget>("DELETE_MAP_BUTTON");
			deleteMapButton.IsDisabled = () => currentTab != MapClassification.User;
			deleteMapButton.IsVisible = () => currentTab == MapClassification.User;
			deleteMapButton.OnClick = () =>
			{
				DeleteOneMap(selectedUid, newUid =>
				{
					RefreshMaps(currentTab);
					EnumerateMaps(currentTab);
					SetupMapTabs();
					if (tabMaps[currentTab].Length == 0)
						SwitchTab(modData.MapCache[newUid].Class);
				});
			};

			var deleteAllMapsButton = widget.Get<ButtonWidget>("DELETE_ALL_MAPS_BUTTON");
			deleteAllMapsButton.IsVisible = () => currentTab == MapClassification.User;
			deleteAllMapsButton.OnClick = () =>
			{
				DeleteAllMaps(visibleMaps, newUid =>
				{
					RefreshMaps(currentTab);
					EnumerateMaps(currentTab);
					SetupMapTabs();
					SwitchTab(modData.MapCache[newUid].Class);
				});
			};

			var remoteMapLabel = widget.Get<LabelWidget>("REMOTE_MAP_LABEL");
			var remoteMapText = new CachedTransform<(int Searching, int Unavailable), string>(counts =>
			{
				if (counts.Searching > 0)
					return FluentProvider.GetMessage(MapSearchingCount, "count", counts.Searching);

				return FluentProvider.GetMessage(MapUnavailableCount, "count", counts.Unavailable);
			});

			remoteMapLabel.IsVisible = () => remoteMapPool != null && (remoteSearching > 0 || remoteUnavailable > 0);
			remoteMapLabel.GetText = () => remoteMapText.Update((remoteSearching, remoteUnavailable));

			// SetupMapTab (through RefreshMap) depends on the map search having already started
			if (remoteMapPool != null && Game.Settings.Game.AllowDownloading)
			{
				var services = modData.Manifest.Get<WebServices>();
				modData.MapCache.QueryRemoteMapDetails(services.MapRepository, remoteMapPool);
			}

			SetupMapPanel(MapClassification.User, "USER_MAPS_TAB");
			SetupMapPanel(MapClassification.System, "SYSTEM_MAPS_TAB");
			SetupMapPanel(MapClassification.Remote, "REMOTE_MAPS_TAB");

			var hasGenerator = modData.DefaultRules.Actors[SystemActors.EditorWorld].HasTraitInfo<IEditorMapGeneratorInfo>();
			if (onSelectGenerated != null && hasGenerator)
				SetupGenerateMapPanel(MapClassification.Generated, "GENERATE_MAP_TAB", initialGeneratedMap);

			// System and user map tabs are hidden when the server forces a restricted pool
			if (remoteMapPool != null)
			{
				tabLabels[MapClassification.Remote] = RemoteMapsTab;
				currentTab = MapClassification.Remote;
				selectedUid = initialMap;
			}
			else
			{
				tabLabels[MapClassification.System] = SystemMapsTab;
				tabLabels[MapClassification.User] = UserMapsTab;
				if (onSelectGenerated != null && hasGenerator)
					tabLabels[MapClassification.Generated] = GeneratedMapsTab;

				if (initialMap != null && modData.MapCache[initialMap].Class == MapClassification.Generated && onSelectGenerated != null && hasGenerator)
				{
					currentTab = MapClassification.Generated;
					selectedUid = modData.MapCache.ChooseInitialMap(null, Game.CosmeticRandom);
				}
				else if (initialMap == null && tabMaps.TryGetValue(initialTab, out var map) && map.Length > 0)
				{
					var uid = map.Select(mp => mp.Uid).First();
					selectedUid = Game.ModData.MapCache.ChooseInitialMap(uid, Game.CosmeticRandom);
					currentTab = initialTab;
				}
				else
				{
					selectedUid = Game.ModData.MapCache.ChooseInitialMap(initialMap, Game.CosmeticRandom);
					currentTab = tabMaps.Keys.FirstOrDefault(k => tabMaps[k].Select(mp => mp.Uid).Contains(selectedUid));
				}
			}

			EnumerateMaps(currentTab);
			SetupMapTabs();
		}

		void SwitchTab(MapClassification tab)
		{
			currentTab = tab;
			EnumerateMaps(tab);
		}

		void RefreshMaps(MapClassification tab)
		{
			if (tab == MapClassification.System || tab == MapClassification.User)
				tabMaps[tab] = modData.MapCache.Where(m => m.Status == MapStatus.Available &&
					m.Class == tab && (m.Visibility & filter) != 0).ToArray();
			else if (remoteMapPool != null)
			{
				var loaded = new List<MapPreview>();
				remoteSearching = 0;
				remoteUnavailable = 0;
				foreach (var uid in remoteMapPool)
				{
					var preview = modData.MapCache[uid];
					var status = preview.Status;
					if (status == MapStatus.Searching)
						remoteSearching++;
					else if (status == MapStatus.Unavailable)
						remoteUnavailable++;
					else
						loaded.Add(preview);
				}

				tabMaps[tab] = loaded.ToArray();

				if (remoteSearching > 0)
				{
					Game.RunAfterDelay(1000, () =>
					{
						if (disposed)
							return;

						var missingBefore = remoteSearching + remoteUnavailable;
						RefreshMaps(MapClassification.Remote);
						var missingAfter = remoteSearching + remoteUnavailable;
						if (currentTab == MapClassification.Remote && missingBefore != missingAfter)
							EnumerateMaps(MapClassification.Remote);
					});
				}
			}
			else
				tabMaps[tab] = [];
		}

		void SetupMapTabs()
		{
			for (var i = 0; i < 3; i++)
				widget.Get<ButtonWidget>($"BUTTON{i + 1}").Visible = false;

			var tabCount = 0;
			foreach (var kv in tabLabels)
			{
				var tab = kv.Key;
				if (tab == MapClassification.User && tabMaps[tab].Length == 0)
					continue;

				var tabButton = widget.Get<ButtonWidget>($"BUTTON{++tabCount}");
				tabButton.IsHighlighted = () => currentTab == tab;
				tabButton.OnClick = () => SwitchTab(tab);
				tabButton.Visible = true;
				tabButton.Text = kv.Value;
			}
		}

		void SetupMapPanel(MapClassification tab, string tabContainerName)
		{
			var tabContainer = widget.Get<ContainerWidget>(tabContainerName);
			tabContainer.IsVisible = () => currentTab == tab;
			var tabScrollpanel = tabContainer.Get<ScrollPanelWidget>("MAP_LIST");
			tabScrollpanel.Layout = new GridLayout(tabScrollpanel);
			scrollpanels.Add(tab, tabScrollpanel);

			RefreshMaps(tab);
		}

		void SetupGenerateMapPanel(MapClassification tab, string tabContainerName, MapGenerationArgs initialSettings)
		{
			var tabContainer = widget.Get<ContainerWidget>(tabContainerName);
			tabContainer.IsVisible = () => currentTab == tab;
			Ui.LoadWidget("MAPCHOOSER_GENERATE_PANEL", tabContainer, new WidgetArgs
			{
				{ "modData", modData },
				{ "initialSettings", initialSettings },
				{
					"onGenerate", (Action<MapGenerationArgs, IReadWritePackage>)((args, package) =>
					{
						generatedMapArgs = args;
						generatedMapPackage = package;
					})
				}
			});
		}

		void SetupGameModeDropdown(MapClassification tab, DropDownButtonWidget gameModeDropdown)
		{
			if (gameModeDropdown != null)
			{
				var categoryDict = new Dictionary<string, int>();
				foreach (var map in tabMaps[tab])
				{
					foreach (var category in map.Categories)
					{
						categoryDict.TryGetValue(category, out var count);
						categoryDict[category] = count + 1;
					}
				}

				// Order categories alphabetically
				var categories = categoryDict
					.Select(kv => (Category: kv.Key, Count: kv.Value))
					.OrderBy(p => p.Category)
					.ToList();

				// 'all game types' extra item
				categories.Insert(0, (null, tabMaps[tab].Length));

				string ShowItem((string Category, int Count) x) => (x.Category ?? allMaps) + $" ({x.Count})";

				ScrollItemWidget SetupItem((string Category, int Count) ii, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template,
						() => category == ii.Category,
						() => { category = ii.Category; EnumerateMaps(tab); });
					item.Get<LabelWidget>("LABEL").GetText = () => ShowItem(ii);
					return item;
				}

				gameModeDropdown.OnClick = () =>
					gameModeDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 210, categories, SetupItem);

				gameModeDropdown.GetText = () =>
				{
					var item = categories.FirstOrDefault(m => m.Category == category);
					if (item == default((string, int)))
						item.Category = FluentProvider.GetMessage(NoMatches);

					return ShowItem(item);
				};
			}
		}

		void SetupOrderByDropdown()
		{
			var orderByDropdown = widget.GetOrNull<DropDownButtonWidget>("ORDERBY");
			if (orderByDropdown == null)
				return;

			var orderByPlayer = FluentProvider.GetMessage(OrderMapsByPlayers);

			var orderByDict = new Dictionary<string, Func<MapPreview, long>>()
			{
				{ orderByPlayer, m => m.PlayerCount },
				{ FluentProvider.GetMessage(OrderMapsByTitle), null },
				{ FluentProvider.GetMessage(OrderMapsByDate), m => -m.ModifiedDate.Ticks },
				{ FluentProvider.GetMessage(OrderMapsBySize), m => m.Bounds.Width * m.Bounds.Height },
			};

			orderByFunc = orderByDict[orderByPlayer];

			ScrollItemWidget SetupItem(string o, ScrollItemWidget template)
			{
				var item = ScrollItemWidget.Setup(template,
					() => orderByFunc == orderByDict[o],
					() => { orderByFunc = orderByDict[o]; EnumerateMaps(currentTab); });
				item.Get<LabelWidget>("LABEL").GetText = () => o;

				return item;
			}

			orderByDropdown.OnClick = () =>
				orderByDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, orderByDict.Keys, SetupItem);

			orderByDropdown.GetText = () =>
				orderByDict.FirstOrDefault(m => m.Value == orderByFunc).Key;
		}

		void EnumerateMaps(MapClassification tab)
		{
			if (tab == MapClassification.Generated)
				return;

			if (!int.TryParse(mapFilter, out var playerCountFilter))
				playerCountFilter = -1;

			var maps = tabMaps[tab]
				.Where(m => (category == null || m.Categories.Contains(category)) &&
					(mapFilter == null ||
					(m.Title != null && m.Title.Contains(mapFilter, StringComparison.CurrentCultureIgnoreCase)) ||
					(m.Author != null && m.Author.Contains(mapFilter, StringComparison.CurrentCultureIgnoreCase)) ||
					m.PlayerCount == playerCountFilter));

			if (orderByFunc == null)
				maps = maps.OrderBy(m => m.Title);
			else
				maps = maps.OrderBy(orderByFunc).ThenBy(m => m.Title);

			maps = maps.ToList();

			scrollpanels[tab].RemoveChildren();
			foreach (var loop in maps)
			{
				var preview = loop;

				// Access the minimap to trigger async generation of the minimap.
				preview.GetMinimap();

				void DblClick()
				{
					if (onSelect != null)
					{
						Ui.CloseWindow();
						onSelect(preview.Uid);
					}
				}

				var item = ScrollItemWidget.Setup(preview.Uid, itemTemplate, () => selectedUid == preview.Uid,
					() => selectedUid = preview.Uid, DblClick);
				item.IsVisible = () => item.RenderBounds.IntersectsWith(scrollpanels[tab].RenderBounds);

				var titleLabel = item.Get<LabelWithTooltipWidget>("TITLE");
				if (titleLabel != null)
				{
					WidgetUtils.TruncateLabelToTooltip(titleLabel, preview.Title);
				}

				var previewWidget = item.Get<MapPreviewWidget>("PREVIEW");
				previewWidget.Preview = () => preview;

				var detailsWidget = item.GetOrNull<LabelWidget>("DETAILS");
				if (detailsWidget != null)
				{
					var type = preview.Categories.FirstOrDefault();
					var details = "";
					if (type != null)
						details = type + " ";

					details += FluentProvider.GetMessage(Players, "players", preview.PlayerCount);
					detailsWidget.GetText = () => details;
				}

				var authorWidget = item.GetOrNull<LabelWithTooltipWidget>("AUTHOR");
				if (authorWidget != null && !string.IsNullOrEmpty(preview.Author))
					WidgetUtils.TruncateLabelToTooltip(authorWidget, FluentProvider.GetMessage(CreatedBy, "author", preview.Author));

				var sizeWidget = item.GetOrNull<LabelWidget>("SIZE");
				if (sizeWidget != null)
				{
					var size = MapSizeLabel(preview.Bounds.Size);
					sizeWidget.GetText = () => size;
				}

				scrollpanels[tab].AddChild(item);
			}

			if (tab == currentTab)
			{
				visibleMaps = maps.Select(m => m.Uid).ToArray();
				SetupGameModeDropdown(currentTab, gameModeDropdown);
			}

			if (visibleMaps.Contains(selectedUid))
				scrollpanels[tab].ScrollToItem(selectedUid);
		}

		string DeleteMap(string map)
		{
			try
			{
				modData.MapCache[map].Delete();
				if (selectedUid == map)
					selectedUid = modData.MapCache.ChooseInitialMap(tabMaps[currentTab].Select(mp => mp.Uid).FirstOrDefault(),
						Game.CosmeticRandom);
			}
			catch (Exception ex)
			{
				TextNotificationsManager.Debug(FluentProvider.GetMessage(MapDeletionFailed, "map", map));
				Log.Write("debug", ex.ToString());
			}

			return selectedUid;
		}

		void DeleteOneMap(string map, Action<string> after)
		{
			ConfirmationDialogs.ButtonPrompt(modData,
				title: DeleteMapTitle,
				text: DeleteMapPrompt,
				textArguments: ["title", modData.MapCache[map].Title],
				onConfirm: () =>
				{
					var newUid = DeleteMap(map);
					after?.Invoke(newUid);
				},
				confirmText: DeleteMapAccept,
				onCancel: () => { });
		}

		void DeleteAllMaps(string[] maps, Action<string> after)
		{
			ConfirmationDialogs.ButtonPrompt(modData,
				title: DeleteAllMapsTitle,
				text: DeleteAllMapsPrompt,
				onConfirm: () =>
				{
					foreach (var map in maps)
						DeleteMap(map);

					after?.Invoke(modData.MapCache.ChooseInitialMap(null, Game.CosmeticRandom));
				},
				confirmText: DeleteAllMapsAccept,
				onCancel: () => { });
		}

		protected override void Dispose(bool disposing)
		{
			disposed = true;
			base.Dispose(disposing);
		}
	}
}
