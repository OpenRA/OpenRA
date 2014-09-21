#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class LobbyMapPreviewLogic
	{
		[ObjectCreator.UseCtor]
		internal LobbyMapPreviewLogic(Widget widget, OrderManager orderManager, LobbyLogic lobby)
		{
			var available = widget.GetOrNull("MAP_AVAILABLE");
			if (available != null)
			{
				available.IsVisible = () => lobby.Map.Status == MapStatus.Available && lobby.Map.RuleStatus == MapRuleStatus.Cached;

				var preview = available.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Preview = () => lobby.Map;
				preview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint(orderManager, preview, lobby.Map, mi);
				preview.SpawnOccupants = () => LobbyUtils.GetSpawnOccupants(orderManager.LobbyInfo, lobby.Map);

				var title = available.GetOrNull<LabelWidget>("MAP_TITLE");
				if (title != null)
					title.GetText = () => lobby.Map.Title;

				var type = available.GetOrNull<LabelWidget>("MAP_TYPE");
				if (type != null)
					type.GetText = () => lobby.Map.Type;

				var author = available.GetOrNull<LabelWidget>("MAP_AUTHOR");
				if (author != null)
					author.GetText = () => (lobby.Map.Revision > 0) ?
						"Rev.{0} by {1}".F(lobby.Map.Revision, lobby.Map.Author) :
						"Created by {0}".F(lobby.Map.Author);
			}

			var invalid = widget.GetOrNull("MAP_INVALID");
			if (invalid != null)
			{
				invalid.IsVisible = () => lobby.Map.Status == MapStatus.Available && lobby.Map.RuleStatus == MapRuleStatus.Invalid;

				var preview = invalid.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Preview = () => lobby.Map;
				preview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint(orderManager, preview, lobby.Map, mi);
				preview.SpawnOccupants = () => LobbyUtils.GetSpawnOccupants(orderManager.LobbyInfo, lobby.Map);

				var title = invalid.GetOrNull<LabelWidget>("MAP_TITLE");
				if (title != null)
					title.GetText = () => lobby.Map.Title;

				var type = invalid.GetOrNull<LabelWidget>("MAP_TYPE");
				if (type != null)
					type.GetText = () => lobby.Map.Type;
			}

			var download = widget.GetOrNull("MAP_DOWNLOADABLE");
			if (download != null)
			{
				download.IsVisible = () => lobby.Map.Status == MapStatus.DownloadAvailable;

				var preview = download.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Preview = () => lobby.Map;
				preview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint(orderManager, preview, lobby.Map, mi);
				preview.SpawnOccupants = () => LobbyUtils.GetSpawnOccupants(orderManager.LobbyInfo, lobby.Map);

				var title = download.GetOrNull<LabelWidget>("MAP_TITLE");
				if (title != null)
					title.GetText = () => lobby.Map.Title;

				var type = download.GetOrNull<LabelWidget>("MAP_TYPE");
				if (type != null)
					type.GetText = () => lobby.Map.Type;

				var author = download.GetOrNull<LabelWidget>("MAP_AUTHOR");
				if (author != null)
					author.GetText = () => (lobby.Map.Revision > 0) ?
						"Rev.{0} by {1}".F(lobby.Map.Revision, lobby.Map.Author) :
						"Created by {0}".F(lobby.Map.Author);

				var install = download.GetOrNull<ButtonWidget>("MAP_INSTALL");
				if (install != null)
					install.OnClick = () => lobby.Map.Install();
			}

			var progress = widget.GetOrNull("MAP_PROGRESS");
			if (progress != null)
			{
				progress.IsVisible = () => (lobby.Map.Status != MapStatus.Available || lobby.Map.RuleStatus == MapRuleStatus.Unknown) && lobby.Map.Status != MapStatus.DownloadAvailable;

				var preview = progress.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Preview = () => lobby.Map;
				preview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint(orderManager, preview, lobby.Map, mi);
				preview.SpawnOccupants = () => LobbyUtils.GetSpawnOccupants(orderManager.LobbyInfo, lobby.Map);

				var title = progress.GetOrNull<LabelWidget>("MAP_TITLE");
				if (title != null)
					title.GetText = () => lobby.Map.Title;

				var type = progress.GetOrNull<LabelWidget>("MAP_TYPE");
				if (type != null)
					type.GetText = () => lobby.Map.Type;

				var statusSearching = progress.GetOrNull("MAP_STATUS_SEARCHING");
				if (statusSearching != null)
					statusSearching.IsVisible = () => lobby.Map.Status == MapStatus.Searching; 

				var statusUnavailable = progress.GetOrNull("MAP_STATUS_UNAVAILABLE");
				if (statusUnavailable != null)
					statusUnavailable.IsVisible = () => lobby.Map.Status == MapStatus.Unavailable; 

				var statusError = progress.GetOrNull("MAP_STATUS_ERROR");
				if (statusError != null)
					statusError.IsVisible = () => lobby.Map.Status == MapStatus.DownloadError; 

				var statusDownloading = progress.GetOrNull<LabelWidget>("MAP_STATUS_DOWNLOADING");
				if (statusDownloading != null)
				{
					statusDownloading.IsVisible = () => lobby.Map.Status == MapStatus.Downloading; 
					statusDownloading.GetText = () =>
					{
						if (lobby.Map.DownloadBytes == 0)
							return "Connecting...";

						// Server does not provide the total file length
						if (lobby.Map.DownloadPercentage == 0)
							return "Downloading {0} kB".F(lobby.Map.DownloadBytes / 1024);

						return "Downloading {0} kB ({1}%)".F(lobby.Map.DownloadBytes / 1024, lobby.Map.DownloadPercentage);
					};
				}

				var retry = progress.GetOrNull<ButtonWidget>("MAP_RETRY");
				if (retry != null)
				{
					retry.IsVisible = () => lobby.Map.Status == MapStatus.DownloadError || lobby.Map.Status == MapStatus.Unavailable; 
					retry.OnClick = () =>
					{
						if (lobby.Map.Status == MapStatus.DownloadError)
							lobby.Map.Install();
						else if (lobby.Map.Status == MapStatus.Unavailable)
							Game.modData.MapCache.QueryRemoteMapDetails(new [] { lobby.Map.Uid });
					};

					retry.GetText = () => lobby.Map.Status == MapStatus.DownloadError ? "Retry Install" : "Retry Search";
				}

				var progressbar = progress.GetOrNull<ProgressBarWidget>("MAP_PROGRESSBAR");
				if (progressbar != null)
				{
					progressbar.IsIndeterminate = () => lobby.Map.DownloadPercentage == 0;
					progressbar.GetPercentage = () => lobby.Map.DownloadPercentage;
					progressbar.IsVisible = () => !retry.IsVisible();
				}
			}
		}
	}
}
