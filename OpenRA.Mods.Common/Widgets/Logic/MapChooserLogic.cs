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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
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
		const string CommunityMapsTab = "button-mapchooser-community-maps-tab";

		[FluentReference]
		const string GeneratedMapsTab = "button-mapchooser-generated-maps-tab";

		[FluentReference]
		const string CommunityMapsLoading = "label-community-maps-loading";

		[FluentReference]
		const string CommunityMapsError = "label-community-maps-error";

		[FluentReference("total")]
		const string CommunityPageTotal = "label-community-page-total";

		[FluentReference]
		const string CommunityPageTotalLoading = "label-community-page-total-loading";

		[FluentReference]
		const string CommunitySortLatest = "label-community-sort-latest";

		[FluentReference]
		const string CommunitySortOldest = "label-community-sort-oldest";

		[FluentReference]
		const string CommunitySortTitle = "label-community-sort-title";

		[FluentReference]
		const string CommunitySortTitleReversed = "label-community-sort-title-reversed";

		[FluentReference]
		const string CommunitySortPlayers = "label-community-sort-players";

		[FluentReference]
		const string CommunitySortLatelyCommented = "label-community-sort-lately-commented";

		[FluentReference]
		const string CommunitySortRating = "label-community-sort-rating";

		[FluentReference]
		const string CommunitySortViews = "label-community-sort-views";

		[FluentReference]
		const string CommunitySortDownloads = "label-community-sort-downloads";

		[FluentReference]
		const string CommunitySortRevisions = "label-community-sort-revisions";

		[FluentReference]
		const string CommunityFilterTilesetAny = "label-community-filter-tileset-any";

		[FluentReference]
		const string CommunityFilterTagsChoose = "label-community-filter-tags-choose";

		[FluentReference]
		const string CommunityFilterTagsAdvanced = "label-community-filter-tags-advanced";

		[FluentReference]
		const string CommunityFilterTagsLua = "label-community-filter-tags-lua";

		[FluentReference("count")]
		const string CommunityCount = "label-community-count";

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
		readonly FrozenSet<string> remoteMapPool;
		readonly ScrollItemWidget itemTemplate;
		readonly MapVisibility filter;

		MapClassification currentTab;
		bool disposed;
		int remoteSearching = 0;
		int remoteUnavailable = 0;

		readonly Widget filterContainer;
		readonly int filterContainerDefaultY;

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

		CommunityMapQuery communityQuery;
		int communitySearching;
		string communityStatusText;
		TextFieldWidget communityPageInput;
		bool communityFiltersDirty;

		// Community sort options: display label → API sort_by value.
		readonly Dictionary<string, string> communitySortOptions = [];
		string selectedCommunitySortLabel;

		// Community filter state.
		string selectedCommunityTileset;
		string selectedCommunityTilesetLabel;
		bool filterAdvanced;
		bool filterLua;

		[ObjectCreator.UseCtor]
		internal MapChooserLogic(Widget widget, ModData modData, string initialMap, MapGenerationArgs initialGeneratedMap, FrozenSet<string> remoteMapPool,
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
				// CloseWindow will dispose this logic, so take ownership of the package.
				var package = generatedMapPackage;
				generatedMapPackage = null;

				Ui.CloseWindow();
				if (currentTab == MapClassification.Generated && generatedMapArgs != null)
				{
					// PERF: Add the map directly into the map cache to allow an instant map switch for the local player
					var p = modData.MapCache[generatedMapArgs.Uid];
					if (p.Status != MapStatus.Available && package is ZipFileLoader.ReadWriteZipFile zipPackage)
					{
						p.UpdateFromGenerationArgs(generatedMapArgs);
						p.UpdateFromMap(zipPackage, MapClassification.Generated);

						// UpdateFromMap took ownership of the package.
						package = null;
					}

					onSelectGenerated?.Invoke(generatedMapArgs);
				}
				else
					onSelect?.Invoke(selectedUid);

				package?.Dispose();
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

			filterContainer = widget.GetOrNull("FILTER_ORDER_CONTROLS");
			if (filterContainer != null)
			{
				filterContainerDefaultY = filterContainer.Bounds.Y;
				filterContainer.IsVisible = () => currentTab != MapClassification.Generated;
			}

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
				randomMapButton.IsVisible = () => currentTab != MapClassification.Generated
					&& currentTab != MapClassification.Community;
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
				var services = modData.GetOrCreate<WebServices>();
				modData.MapCache.QueryRemoteMapDetails(services.MapRepository, remoteMapPool);
			}

			SetupMapPanel(MapClassification.User, "USER_MAPS_TAB");
			SetupMapPanel(MapClassification.System, "SYSTEM_MAPS_TAB");
			SetupMapPanel(MapClassification.Remote, "REMOTE_MAPS_TAB");

			// Community maps tab is available when downloading is allowed and not in a server pool.
			if (remoteMapPool == null && Game.Settings.Game.AllowDownloading)
				SetupCommunityMapsPanel();

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
				tabLabels[MapClassification.Community] = CommunityMapsTab;
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

		void SetupCommunityMapsPanel()
		{
			var services = modData.GetOrCreate<WebServices>();
			communityQuery = new CommunityMapQuery(modData, services.MapBrowserApi, services.MapRepository);

			SetupMapPanel(MapClassification.Community, "COMMUNITY_MAPS_TAB");

			var communityMapLabel = widget.GetOrNull<LabelWidget>("COMMUNITY_MAP_LABEL");
			if (communityMapLabel != null)
			{
				communityMapLabel.IsVisible = () => currentTab == MapClassification.Community && communityStatusText != null;
				communityMapLabel.GetText = () => communityStatusText;
			}

			var communityFilterControls = widget.GetOrNull("COMMUNITY_FILTER_CONTROLS");
			if (communityFilterControls != null)
				communityFilterControls.IsVisible = () => currentTab == MapClassification.Community;

			SetupCommunityTilesetFilter();
			SetupCommunityTagsFilter();
			SetupCommunityPlayersFilter();
			SetupCommunitySortDropdown();
			SetupCommunityPagination();
			SetupCommunitySearchButton();

			// Start loading the first page automatically.
			SearchCommunityMaps();
		}

		void SetupCommunityTilesetFilter()
		{
			var tilesetDropdown = widget.GetOrNull<DropDownButtonWidget>("COMMUNITY_TILESET_FILTER");
			if (tilesetDropdown == null)
				return;

			var anyLabel = FluentProvider.GetMessage(CommunityFilterTilesetAny);
			selectedCommunityTilesetLabel = anyLabel;

			// Build tileset options from the current mod's terrain definitions.
			var tilesets = new List<(string Id, string Label)> { (null, anyLabel) };
			foreach (var kv in modData.DefaultTerrainInfo)
				tilesets.Add((kv.Key, kv.Key));

			tilesetDropdown.GetText = () => selectedCommunityTilesetLabel;
			tilesetDropdown.OnClick = () =>
			{
				ScrollItemWidget SetupItem((string Id, string Label) ts, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template,
						() => selectedCommunityTileset == ts.Id,
						() =>
						{
							selectedCommunityTileset = ts.Id;
							selectedCommunityTilesetLabel = ts.Label;
							communityQuery.Tileset = ts.Id;
							communityFiltersDirty = true;
						});
					item.Get<LabelWidget>("LABEL").GetText = () => ts.Label;
					return item;
				}

				tilesetDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", tilesets.Count * 30, tilesets, SetupItem);
			};
		}

		void SetupCommunityTagsFilter()
		{
			var tagsDropdown = widget.GetOrNull<DropDownButtonWidget>("COMMUNITY_TAGS_FILTER");
			if (tagsDropdown == null)
				return;

			var advancedLabel = FluentProvider.GetMessage(CommunityFilterTagsAdvanced);
			var luaLabel = FluentProvider.GetMessage(CommunityFilterTagsLua);

			var chooseLabel = FluentProvider.GetMessage(CommunityFilterTagsChoose);
			tagsDropdown.GetText = () =>
			{
				if (filterAdvanced && filterLua)
					return $"{advancedLabel}, {luaLabel}";
				if (filterAdvanced)
					return advancedLabel;
				if (filterLua)
					return luaLabel;
				return chooseLabel;
			};

			var tagsPanel = CreateCommunityTagsPanel(advancedLabel, luaLabel);
			tagsDropdown.OnMouseDown = _ =>
			{
				tagsDropdown.RemovePanel();
				tagsDropdown.AttachPanel(tagsPanel);
			};
		}

		Widget CreateCommunityTagsPanel(string advancedLabel, string luaLabel)
		{
			var panel = Ui.LoadWidget("COMMUNITY_TAGS_PANEL", null, []);
			var template = panel.Get<CheckboxWidget>("COMMUNITY_TAG_TEMPLATE");

			var advancedCheckbox = template.Clone();
			advancedCheckbox.GetText = () => advancedLabel;
			advancedCheckbox.IsChecked = () => filterAdvanced;
			advancedCheckbox.IsVisible = () => true;
			advancedCheckbox.OnClick = () =>
			{
				filterAdvanced = !filterAdvanced;
				communityQuery.OnlyAdvanced = filterAdvanced;
				communityFiltersDirty = true;
			};

			panel.AddChild(advancedCheckbox);

			var luaCheckbox = template.Clone();
			luaCheckbox.GetText = () => luaLabel;
			luaCheckbox.IsChecked = () => filterLua;
			luaCheckbox.IsVisible = () => true;
			luaCheckbox.OnClick = () =>
			{
				filterLua = !filterLua;
				communityQuery.OnlyLua = filterLua;
				communityFiltersDirty = true;
			};

			panel.AddChild(luaCheckbox);

			return panel;
		}

		void SetupCommunityPlayersFilter()
		{
			var playersField = widget.GetOrNull<TextFieldWidget>("COMMUNITY_PLAYERS_FILTER");
			if (playersField == null)
				return;

			playersField.Type = TextFieldType.Integer;
			playersField.MaxLength = 2;
			playersField.OnTextEdited = () =>
			{
				if (int.TryParse(playersField.Text, out var players) && players > 0)
					communityQuery.Players = players;
				else
					communityQuery.Players = null;

				communityFiltersDirty = true;
			};
		}

		void SetupCommunitySortDropdown()
		{
			var sortDropdown = widget.GetOrNull<DropDownButtonWidget>("COMMUNITY_SORT");
			if (sortDropdown == null)
				return;

			// Build the sort options mapping display labels to API sort_by values.
			communitySortOptions[FluentProvider.GetMessage(CommunitySortLatest)] = "latest";
			communitySortOptions[FluentProvider.GetMessage(CommunitySortOldest)] = "oldest";
			communitySortOptions[FluentProvider.GetMessage(CommunitySortTitle)] = "title";
			communitySortOptions[FluentProvider.GetMessage(CommunitySortTitleReversed)] = "title_reversed";
			communitySortOptions[FluentProvider.GetMessage(CommunitySortPlayers)] = "players";
			communitySortOptions[FluentProvider.GetMessage(CommunitySortLatelyCommented)] = "lately_commented";
			communitySortOptions[FluentProvider.GetMessage(CommunitySortRating)] = "rating";
			communitySortOptions[FluentProvider.GetMessage(CommunitySortViews)] = "views";
			communitySortOptions[FluentProvider.GetMessage(CommunitySortDownloads)] = "downloads";
			communitySortOptions[FluentProvider.GetMessage(CommunitySortRevisions)] = "revisions";

			selectedCommunitySortLabel = communitySortOptions.Keys.First();

			sortDropdown.GetText = () => selectedCommunitySortLabel;
			sortDropdown.OnClick = () =>
			{
				ScrollItemWidget SetupItem(string label, ScrollItemWidget template)
				{
					var item = ScrollItemWidget.Setup(template,
						() => selectedCommunitySortLabel == label,
						() =>
						{
							selectedCommunitySortLabel = label;
							communityQuery.SortBy = communitySortOptions[label];
							communityFiltersDirty = true;
						});
					item.Get<LabelWidget>("LABEL").GetText = () => label;
					return item;
				}

				sortDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", communitySortOptions.Count * 30, communitySortOptions.Keys, SetupItem);
			};
		}

		void SetupCommunitySearchButton()
		{
			var searchButton = widget.GetOrNull<ButtonWidget>("COMMUNITY_SEARCH");
			if (searchButton == null)
				return;

			searchButton.IsVisible = () => currentTab == MapClassification.Community;
			searchButton.IsDisabled = () => communityQuery.IsLoading;
			searchButton.OnClick = SearchCommunityMaps;
		}

		void SetupCommunityPagination()
		{
			var pageFirst = widget.GetOrNull<ButtonWidget>("COMMUNITY_PAGE_FIRST");
			if (pageFirst != null)
			{
				pageFirst.IsVisible = () => currentTab == MapClassification.Community;
				pageFirst.IsDisabled = () => communityFiltersDirty || communityQuery.IsLoading || communityQuery.CurrentPage <= 1;
				pageFirst.OnClick = () => GoToCommunityPage(1);
			}

			var pagePrev = widget.GetOrNull<ButtonWidget>("COMMUNITY_PAGE_PREV");
			if (pagePrev != null)
			{
				pagePrev.IsVisible = () => currentTab == MapClassification.Community;
				pagePrev.IsDisabled = () => communityFiltersDirty || communityQuery.IsLoading || communityQuery.CurrentPage <= 1;
				pagePrev.OnClick = () => GoToCommunityPage(communityQuery.CurrentPage - 1);

				var prevIcon = pagePrev.GetOrNull<ImageWidget>("IMAGE_PREV");
				if (prevIcon != null)
					prevIcon.GetImageName = () => pagePrev.IsDisabled() ? "left-disabled" : "left";
			}

			var pageNext = widget.GetOrNull<ButtonWidget>("COMMUNITY_PAGE_NEXT");
			if (pageNext != null)
			{
				pageNext.IsVisible = () => currentTab == MapClassification.Community;
				pageNext.IsDisabled = () => communityFiltersDirty || communityQuery.IsLoading
					|| (communityQuery.TotalPages.HasValue && communityQuery.CurrentPage >= communityQuery.TotalPages.Value);
				pageNext.OnClick = () => GoToCommunityPage(communityQuery.CurrentPage + 1);

				var nextIcon = pageNext.GetOrNull<ImageWidget>("IMAGE_NEXT");
				if (nextIcon != null)
					nextIcon.GetImageName = () => pageNext.IsDisabled() ? "right-disabled" : "right";
			}

			var pageLast = widget.GetOrNull<ButtonWidget>("COMMUNITY_PAGE_LAST");
			if (pageLast != null)
			{
				pageLast.IsVisible = () => currentTab == MapClassification.Community;
				pageLast.IsDisabled = () => communityFiltersDirty || communityQuery.IsLoading || !communityQuery.TotalPages.HasValue
					|| communityQuery.CurrentPage >= communityQuery.TotalPages.Value;
				pageLast.OnClick = () => GoToCommunityPage(communityQuery.TotalPages.Value);
			}

			var pageLabel = widget.GetOrNull<LabelWidget>("COMMUNITY_PAGE_LABEL");
			if (pageLabel != null)
				pageLabel.IsVisible = () => currentTab == MapClassification.Community;

			communityPageInput = widget.GetOrNull<TextFieldWidget>("COMMUNITY_PAGE_INPUT");
			if (communityPageInput != null)
			{
				communityPageInput.IsVisible = () => currentTab == MapClassification.Community;
				communityPageInput.IsDisabled = () => communityFiltersDirty || communityQuery.IsLoading;
				communityPageInput.Type = TextFieldType.Integer;
				communityPageInput.MaxLength = 4;
				communityPageInput.Text = communityQuery.CurrentPage.ToString(CultureInfo.InvariantCulture);
				communityPageInput.OnEnterKey = _ =>
				{
					if (int.TryParse(communityPageInput.Text, out var page) && page >= 1)
					{
						if (communityQuery.TotalPages.HasValue)
							page = Math.Min(page, communityQuery.TotalPages.Value);

						GoToCommunityPage(page);
					}

					return true;
				};

				communityPageInput.OnLoseFocus = () =>
					communityPageInput.Text = communityQuery.CurrentPage.ToString(CultureInfo.InvariantCulture);
			}

			var pageTotal = widget.GetOrNull<LabelWidget>("COMMUNITY_PAGE_TOTAL");
			if (pageTotal != null)
			{
				pageTotal.IsVisible = () => currentTab == MapClassification.Community;
				pageTotal.GetText = () =>
				{
					var totalPages = communityQuery.TotalPages;
					return totalPages.HasValue
						? FluentProvider.GetMessage(CommunityPageTotal, "total", totalPages.Value)
						: FluentProvider.GetMessage(CommunityPageTotalLoading);
				};
			}

			var countLabel = widget.GetOrNull<LabelWidget>("COMMUNITY_COUNT_LABEL");
			if (countLabel != null)
			{
				countLabel.IsVisible = () => currentTab == MapClassification.Community && communityQuery.TotalAvailable.HasValue;
				countLabel.GetText = () =>
				{
					var total = communityQuery.TotalAvailable;
					return total.HasValue
						? FluentProvider.GetMessage(CommunityCount, "count", total.Value)
						: "";
				};
			}

			var resourceCenterButton = widget.GetOrNull<ButtonWidget>("COMMUNITY_RESOURCE_CENTER_BUTTON");
			if (resourceCenterButton != null)
			{
				var services = modData.GetOrCreate<WebServices>();
				resourceCenterButton.IsVisible = () => currentTab == MapClassification.Community;
				resourceCenterButton.OnClick = () => Game.Renderer.TryOpenUrl(services.ResourceCenter);
			}
		}

		void GoToCommunityPage(int page)
		{
			communityStatusText = FluentProvider.GetMessage(CommunityMapsLoading);
			communityQuery.GoToPage(page, OnCommunityMapsLoaded, OnCommunityMapsError);
		}

		void SearchCommunityMaps()
		{
			communityFiltersDirty = false;
			communityStatusText = FluentProvider.GetMessage(CommunityMapsLoading);
			communityQuery.Search(OnCommunityMapsLoaded, OnCommunityMapsError);
		}

		void OnCommunityMapsLoaded()
		{
			if (disposed)
				return;

			// Update the page input field to reflect the current page.
			if (communityPageInput != null)
				communityPageInput.Text = communityQuery.CurrentPage.ToString(CultureInfo.InvariantCulture);

			// Community maps are loaded asynchronously via QueryRemoteMapDetails.
			// Poll for results until all previews are resolved.
			communitySearching = 0;
			RefreshCommunityMaps();

			if (communitySearching > 0)
			{
				Game.RunAfterDelay(1000, () =>
				{
					if (disposed)
						return;

					OnCommunityMapsLoaded();
				});
			}
			else
				communityStatusText = null;

			if (currentTab == MapClassification.Community)
				EnumerateMaps(MapClassification.Community);

			SetupMapTabs();
		}

		void OnCommunityMapsError(string error)
		{
			if (disposed)
				return;

			communityStatusText = FluentProvider.GetMessage(CommunityMapsError);
			Log.Write("debug", $"Community map browser error: {error}");
		}

		void RefreshCommunityMaps()
		{
			var loaded = new List<MapPreview>();
			communitySearching = 0;
			foreach (var preview in modData.MapCache)
			{
				if (preview.Class != MapClassification.Community)
					continue;

				// Only include maps that belong to the current query results.
				if (!communityQuery.ContainsHash(preview.Uid))
					continue;

				if (preview.Status == MapStatus.Searching)
					communitySearching++;
				else if (preview.Status == MapStatus.DownloadAvailable || preview.Status == MapStatus.Available
					|| preview.Status == MapStatus.Downloading)
					loaded.Add(preview);
			}

			tabMaps[MapClassification.Community] = loaded.ToArray();
		}

		void SwitchTab(MapClassification tab)
		{
			currentTab = tab;

			// On the Community tab, move the filter row up to make room
			// for the community-specific filter controls on the row below.
			if (filterContainer != null)
				filterContainer.Bounds.Y = tab == MapClassification.Community
					? filterContainerDefaultY - 30
					: filterContainerDefaultY;

			EnumerateMaps(tab);
		}

		void RefreshMaps(MapClassification tab)
		{
			if (tab == MapClassification.System || tab == MapClassification.User)
				tabMaps[tab] = modData.MapCache.Where(m => m.Status == MapStatus.Available &&
					m.Class == tab && (m.Visibility & filter) != 0).ToArray();
			else if (tab == MapClassification.Community)
				RefreshCommunityMaps();
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
			for (var i = 0; i < 4; i++)
				widget.Get<ButtonWidget>($"BUTTON{i + 1}").Visible = false;

			var tabCount = 0;
			foreach (var kv in tabLabels)
			{
				var tab = kv.Key;
				if (tab == MapClassification.User && tabMaps[tab].Length == 0)
					continue;

				// Hide the Community tab when downloading is disabled or not configured.
				if (tab == MapClassification.Community && communityQuery == null)
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

		void SetupGenerateMapPanel(MapClassification tab, string tabContainerName, MapGenerationArgs initialGeneratedMap)
		{
			var tabContainer = widget.Get<ContainerWidget>(tabContainerName);
			tabContainer.IsVisible = () => currentTab == tab;
			Ui.LoadWidget("MAPCHOOSER_GENERATE_PANEL", tabContainer, new WidgetArgs
			{
				{ "modData", modData },
				{ "initialGeneratedMap", initialGeneratedMap },
				{
					"onGenerate", (Action<MapGenerationArgs, IReadWritePackage>)((args, package) =>
					{
						generatedMapArgs = args;
						generatedMapPackage?.Dispose();
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

			// Hide the standard Order By controls on the Community tab (replaced by COMMUNITY_SORT).
			orderByDropdown.IsVisible = () => currentTab != MapClassification.Community;

			var orderByLabel = widget.GetOrNull<LabelWidget>("ORDERBY_LABEL");
			if (orderByLabel != null)
				orderByLabel.IsVisible = () => currentTab != MapClassification.Community;

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

			communityQuery?.CancelPending();

			generatedMapPackage?.Dispose();
			generatedMapPackage = null;

			base.Dispose(disposing);
		}
	}
}
