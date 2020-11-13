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
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapPreviewLogic : ChromeLogic
	{
		readonly int blinkTickLength = 10;
		bool installHighlighted;
		int blinkTick;

		[ObjectCreator.UseCtor]
		internal MapPreviewLogic(Widget widget, ModData modData, OrderManager orderManager, Func<MapPreview> getMap, Action<MapPreviewWidget, MapPreview, MouseInput> onMouseDown,
			Func<Dictionary<int, SpawnOccupant>> getSpawnOccupants, Func<HashSet<int>> getDisabledSpawnPoints, bool showUnoccupiedSpawnpoints)
		{
			var mapRepository = modData.Manifest.Get<WebServices>().MapRepository;

			var available = widget.GetOrNull("MAP_AVAILABLE");
			if (available != null)
			{
				available.IsVisible = () =>
				{
					var map = getMap();
					return map.Status == MapStatus.Available && (!map.RulesLoaded || !map.InvalidCustomRules);
				};

				SetupWidgets(available, getMap, onMouseDown, getSpawnOccupants, getDisabledSpawnPoints, showUnoccupiedSpawnpoints);
			}

			var invalid = widget.GetOrNull("MAP_INVALID");
			if (invalid != null)
			{
				invalid.IsVisible = () =>
				{
					var map = getMap();
					return map.Status == MapStatus.Available && map.InvalidCustomRules;
				};

				SetupWidgets(invalid, getMap, onMouseDown, getSpawnOccupants, getDisabledSpawnPoints, showUnoccupiedSpawnpoints);
			}

			var download = widget.GetOrNull("MAP_DOWNLOADABLE");
			if (download != null)
			{
				download.IsVisible = () => getMap().Status == MapStatus.DownloadAvailable;
				SetupWidgets(download, getMap, onMouseDown, getSpawnOccupants, getDisabledSpawnPoints, showUnoccupiedSpawnpoints);

				var install = download.GetOrNull<ButtonWidget>("MAP_INSTALL");
				if (install != null)
				{
					install.OnClick = () =>
					{
						var map = getMap();
						map.Install(mapRepository, () =>
						{
							map.PreloadRules();
							if (orderManager != null)
								Game.RunAfterTick(() => orderManager.IssueOrder(Order.Command("state {0}".F(Session.ClientState.NotReady))));
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
					var map = getMap();
					return map.Status != MapStatus.Available && map.Status != MapStatus.DownloadAvailable;
				};

				SetupWidgets(progress, getMap, onMouseDown, getSpawnOccupants, getDisabledSpawnPoints, showUnoccupiedSpawnpoints);

				var statusSearching = progress.GetOrNull("MAP_STATUS_SEARCHING");
				if (statusSearching != null)
					statusSearching.IsVisible = () => getMap().Status == MapStatus.Searching;

				var statusUnavailable = progress.GetOrNull("MAP_STATUS_UNAVAILABLE");
				if (statusUnavailable != null)
				{
					statusUnavailable.IsVisible = () =>
					{
						var map = getMap();
						return map.Status == MapStatus.Unavailable && map != MapCache.UnknownMap;
					};
				}

				var statusError = progress.GetOrNull("MAP_STATUS_ERROR");
				if (statusError != null)
					statusError.IsVisible = () => getMap().Status == MapStatus.DownloadError;

				var statusDownloading = progress.GetOrNull<LabelWidget>("MAP_STATUS_DOWNLOADING");
				if (statusDownloading != null)
				{
					statusDownloading.IsVisible = () => getMap().Status == MapStatus.Downloading;
					statusDownloading.GetText = () =>
					{
						var map = getMap();
						if (map.DownloadBytes == 0)
							return "Connecting...";

						// Server does not provide the total file length
						if (map.DownloadPercentage == 0)
							return "Downloading {0} kB".F(map.DownloadBytes / 1024);

						return "Downloading {0} kB ({1}%)".F(map.DownloadBytes / 1024, map.DownloadPercentage);
					};
				}

				var retry = progress.GetOrNull<ButtonWidget>("MAP_RETRY");
				if (retry != null)
				{
					retry.IsVisible = () =>
					{
						var map = getMap();
						return (map.Status == MapStatus.DownloadError || map.Status == MapStatus.Unavailable) && map != MapCache.UnknownMap;
					};

					retry.OnClick = () =>
					{
						var map = getMap();
						if (map.Status == MapStatus.DownloadError)
						{
							map.Install(mapRepository, () =>
							{
								map.PreloadRules();
								if (orderManager != null)
									Game.RunAfterTick(() => orderManager.IssueOrder(Order.Command("state {0}".F(Session.ClientState.NotReady))));
							});
						}
						else if (map.Status == MapStatus.Unavailable)
							modData.MapCache.QueryRemoteMapDetails(mapRepository, new[] { map.Uid });
					};

					retry.GetText = () => getMap().Status == MapStatus.DownloadError ? "Retry Install" : "Retry Search";
				}

				var progressbar = progress.GetOrNull<ProgressBarWidget>("MAP_PROGRESSBAR");
				if (progressbar != null)
				{
					progressbar.IsIndeterminate = () => getMap().DownloadPercentage == 0;
					progressbar.GetPercentage = () => getMap().DownloadPercentage;
					progressbar.IsVisible = () => getMap().Status == MapStatus.Downloading;
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

		void SetupWidgets(Widget parent, Func<MapPreview> getMap,
			Action<MapPreviewWidget, MapPreview, MouseInput> onMouseDown, Func<Dictionary<int, SpawnOccupant>> getSpawnOccupants, Func<HashSet<int>> getDisabledSpawnPoints, bool showUnoccupiedSpawnpoints)
		{
			var preview = parent.Get<MapPreviewWidget>("MAP_PREVIEW");
			preview.Preview = () => getMap();
			preview.OnMouseDown = mi => onMouseDown(preview, getMap(), mi);
			preview.SpawnOccupants = getSpawnOccupants;
			preview.DisabledSpawnPoints = getDisabledSpawnPoints;
			preview.ShowUnoccupiedSpawnpoints = showUnoccupiedSpawnpoints;

			var titleLabel = parent.GetOrNull<LabelWithTooltipWidget>("MAP_TITLE");
			if (titleLabel != null)
			{
				titleLabel.IsVisible = () => getMap() != MapCache.UnknownMap;
				var font = Game.Renderer.Fonts[titleLabel.Font];
				var title = new CachedTransform<MapPreview, string>(m => WidgetUtils.TruncateText(m.Title, titleLabel.Bounds.Width, font));
				titleLabel.GetText = () => title.Update(getMap());
				titleLabel.GetTooltipText = () => getMap().Title;
			}

			var typeLabel = parent.GetOrNull<LabelWidget>("MAP_TYPE");
			if (typeLabel != null)
			{
				var type = new CachedTransform<MapPreview, string>(m => m.Categories.FirstOrDefault() ?? "");
				typeLabel.GetText = () => type.Update(getMap());
			}

			var authorLabel = parent.GetOrNull<LabelWidget>("MAP_AUTHOR");
			if (authorLabel != null)
			{
				var font = Game.Renderer.Fonts[authorLabel.Font];
				var author = new CachedTransform<MapPreview, string>(
					m => WidgetUtils.TruncateText("Created by {0}".F(m.Author), authorLabel.Bounds.Width, font));
				authorLabel.GetText = () => author.Update(getMap());
			}
		}
	}
}
