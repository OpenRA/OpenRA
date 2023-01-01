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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapPreviewLogic : ChromeLogic
	{
		[TranslationReference]
		const string Connecting = "label-connecting";

		[TranslationReference("size")]
		const string Downloading = "label-downloading-map";

		[TranslationReference("size", "progress")]
		const string DownloadingPercentage = "label-downloading-map-progress";

		[TranslationReference]
		const string RetryInstall = "button-retry-install";

		[TranslationReference]
		const string RetrySearch = "button-retry-search";

		[TranslationReference("author")]
		const string CreatedBy = "label-created-by";

		readonly int blinkTickLength = 10;
		bool installHighlighted;
		int blinkTick;

		[ObjectCreator.UseCtor]
		internal MapPreviewLogic(Widget widget, ModData modData, OrderManager orderManager, Func<(MapPreview Map, Session.MapStatus Status)> getMap,
			Action<MapPreviewWidget, MapPreview, MouseInput> onMouseDown, Func<Dictionary<int, SpawnOccupant>> getSpawnOccupants,
			Func<HashSet<int>> getDisabledSpawnPoints, bool showUnoccupiedSpawnpoints)
		{
			var mapRepository = modData.Manifest.Get<WebServices>().MapRepository;

			var available = widget.GetOrNull("MAP_AVAILABLE");
			if (available != null)
			{
				available.IsVisible = () =>
				{
					var (map, serverStatus) = getMap();
					var isPlayable = (serverStatus & Session.MapStatus.Playable) == Session.MapStatus.Playable;
					return map.Status == MapStatus.Available && isPlayable;
				};

				SetupWidgets(available, modData, getMap, onMouseDown, getSpawnOccupants, getDisabledSpawnPoints, showUnoccupiedSpawnpoints);
			}

			var invalid = widget.GetOrNull("MAP_INVALID");
			if (invalid != null)
			{
				invalid.IsVisible = () =>
				{
					var (map, serverStatus) = getMap();
					return map.Status == MapStatus.Available && (serverStatus & Session.MapStatus.Incompatible) != 0;
				};

				SetupWidgets(invalid, modData, getMap, onMouseDown, getSpawnOccupants, getDisabledSpawnPoints, showUnoccupiedSpawnpoints);
			}

			var validating = widget.GetOrNull("MAP_VALIDATING");
			if (validating != null)
			{
				validating.IsVisible = () =>
				{
					var (map, serverStatus) = getMap();
					return map.Status == MapStatus.Available && (serverStatus & Session.MapStatus.Validating) != 0;
				};

				SetupWidgets(validating, modData, getMap, onMouseDown, getSpawnOccupants, getDisabledSpawnPoints, showUnoccupiedSpawnpoints);
			}

			var download = widget.GetOrNull("MAP_DOWNLOADABLE");
			if (download != null)
			{
				download.IsVisible = () => getMap().Map.Status == MapStatus.DownloadAvailable;

				SetupWidgets(download, modData, getMap, onMouseDown, getSpawnOccupants, getDisabledSpawnPoints, showUnoccupiedSpawnpoints);

				var install = download.GetOrNull<ButtonWidget>("MAP_INSTALL");
				if (install != null)
				{
					install.OnClick = () =>
					{
						getMap().Map.Install(mapRepository, () =>
						{
							if (orderManager != null)
								Game.RunAfterTick(() => orderManager.IssueOrder(Order.Command($"state {Session.ClientState.NotReady}")));
						});
					};

					install.IsHighlighted = () => installHighlighted;
				}
			}

			var progress = widget.GetOrNull("MAP_PROGRESS");
			if (progress != null)
			{
				progress.IsVisible = () =>
				{
					var (map, _) = getMap();
					return map.Status != MapStatus.Available && map.Status != MapStatus.DownloadAvailable;
				};

				SetupWidgets(progress, modData, getMap, onMouseDown, getSpawnOccupants, getDisabledSpawnPoints, showUnoccupiedSpawnpoints);

				var statusSearching = progress.GetOrNull("MAP_STATUS_SEARCHING");
				if (statusSearching != null)
				{
					statusSearching.IsVisible = () =>
					{
						var (map, _) = getMap();
						return map.Status == MapStatus.Searching;
					};
				}

				var statusUnavailable = progress.GetOrNull("MAP_STATUS_UNAVAILABLE");
				if (statusUnavailable != null)
				{
					statusUnavailable.IsVisible = () =>
					{
						var (map, _) = getMap();
						return map.Status == MapStatus.Unavailable && map != MapCache.UnknownMap;
					};
				}

				var statusError = progress.GetOrNull("MAP_STATUS_ERROR");
				if (statusError != null)
					statusError.IsVisible = () => getMap().Map.Status == MapStatus.DownloadError;

				var statusDownloading = progress.GetOrNull<LabelWidget>("MAP_STATUS_DOWNLOADING");
				if (statusDownloading != null)
				{
					statusDownloading.IsVisible = () => getMap().Map.Status == MapStatus.Downloading;

					statusDownloading.GetText = () =>
					{
						var (map, _) = getMap();
						if (map.DownloadBytes == 0)
							return modData.Translation.GetString(Connecting);

						// Server does not provide the total file length
						if (map.DownloadPercentage == 0)
							 modData.Translation.GetString(Downloading, Translation.Arguments("size", map.DownloadBytes / 1024));

						return modData.Translation.GetString(DownloadingPercentage, Translation.Arguments("size", map.DownloadBytes / 1024, "progress", map.DownloadPercentage));
					};
				}

				var retry = progress.GetOrNull<ButtonWidget>("MAP_RETRY");
				if (retry != null)
				{
					retry.IsVisible = () =>
					{
						var (map, _) = getMap();
						return (map.Status == MapStatus.DownloadError || map.Status == MapStatus.Unavailable) && map != MapCache.UnknownMap;
					};

					retry.OnClick = () =>
					{
						modData.MapCache.UpdateMaps();
						var (map, _) = getMap();
						if (map.Status == MapStatus.DownloadError)
						{
							map.Install(mapRepository, () =>
							{
								if (orderManager != null)
									Game.RunAfterTick(() => orderManager.IssueOrder(Order.Command($"state {Session.ClientState.NotReady}")));
							});
						}
						else if (map.Status == MapStatus.Unavailable)
							modData.MapCache.QueryRemoteMapDetails(mapRepository, new[] { map.Uid });
					};

					retry.GetText = () => getMap().Map.Status == MapStatus.DownloadError
						? modData.Translation.GetString(RetryInstall)
						: modData.Translation.GetString(RetrySearch);
				}

				var progressbar = progress.GetOrNull<ProgressBarWidget>("MAP_PROGRESSBAR");
				if (progressbar != null)
				{
					progressbar.IsIndeterminate = () => getMap().Map.DownloadPercentage == 0;
					progressbar.GetPercentage = () => getMap().Map.DownloadPercentage;
					progressbar.IsVisible = () => getMap().Map.Status == MapStatus.Downloading;
				}
			}
		}

		public override void Tick()
		{
			if (++blinkTick >= blinkTickLength)
			{
				installHighlighted ^= true;
				blinkTick = 0;
			}
		}

		static void SetupWidgets(Widget parent, ModData modData,
			Func<(MapPreview Map, Session.MapStatus Status)> getMap,
			Action<MapPreviewWidget, MapPreview, MouseInput> onMouseDown,
			Func<Dictionary<int, SpawnOccupant>> getSpawnOccupants,
			Func<HashSet<int>> getDisabledSpawnPoints,
			bool showUnoccupiedSpawnpoints)
		{
			var preview = parent.Get<MapPreviewWidget>("MAP_PREVIEW");
			preview.Preview = () => getMap().Map;
			preview.OnMouseDown = mi => onMouseDown(preview, getMap().Map, mi);
			preview.SpawnOccupants = getSpawnOccupants;
			preview.DisabledSpawnPoints = getDisabledSpawnPoints;
			preview.ShowUnoccupiedSpawnpoints = showUnoccupiedSpawnpoints;

			var titleLabel = parent.GetOrNull<LabelWithTooltipWidget>("MAP_TITLE");
			if (titleLabel != null)
			{
				titleLabel.IsVisible = () => getMap().Map != MapCache.UnknownMap;
				var font = Game.Renderer.Fonts[titleLabel.Font];
				var title = new CachedTransform<MapPreview, string>(m =>
				{
					var truncated = WidgetUtils.TruncateText(m.Title, titleLabel.Bounds.Width, font);

					if (m.Title != truncated)
						titleLabel.GetTooltipText = () => m.Title;
					else
						titleLabel.GetTooltipText = null;

					return truncated;
				});
				titleLabel.GetText = () => title.Update(getMap().Map);
			}

			var typeLabel = parent.GetOrNull<LabelWidget>("MAP_TYPE");
			if (typeLabel != null)
			{
				var type = new CachedTransform<MapPreview, string>(m => m.Categories.FirstOrDefault() ?? "");
				typeLabel.GetText = () => type.Update(getMap().Map);
			}

			var authorLabel = parent.GetOrNull<LabelWidget>("MAP_AUTHOR");
			if (authorLabel != null)
			{
				var font = Game.Renderer.Fonts[authorLabel.Font];
				var author = new CachedTransform<MapPreview, string>(
					m => WidgetUtils.TruncateText(modData.Translation.GetString(CreatedBy, Translation.Arguments("author", m.Author)), authorLabel.Bounds.Width, font));
				authorLabel.GetText = () => author.Update(getMap().Map);
			}
		}
	}
}
