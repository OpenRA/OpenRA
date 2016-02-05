#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class LobbyMapPreviewLogic : ChromeLogic
	{
		readonly int blinkTickLength = 10;
		bool installHighlighted;
		int blinkTick;

		[ObjectCreator.UseCtor]
		internal LobbyMapPreviewLogic(Widget widget, OrderManager orderManager, LobbyLogic lobby)
		{
			var available = widget.GetOrNull("MAP_AVAILABLE");
			if (available != null)
			{
				available.IsVisible = () => lobby.MapPreview.Status == MapStatus.Available && lobby.MapPreview.RuleStatus == MapRuleStatus.Cached;

				var preview = available.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Preview = () => lobby.MapPreview;
				preview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint(orderManager, preview, lobby.MapPreview, mi);
				preview.SpawnOccupants = () => LobbyUtils.GetSpawnOccupants(orderManager.LobbyInfo, lobby.MapPreview);

				var titleLabel = available.GetOrNull<LabelWidget>("MAP_TITLE");
				if (titleLabel != null)
				{
					var font = Game.Renderer.Fonts[titleLabel.Font];
					var title = new CachedTransform<MapPreview, string>(m => WidgetUtils.TruncateText(m.Title, titleLabel.Bounds.Width, font));
					titleLabel.GetText = () => title.Update(lobby.MapPreview);
				}

				var typeLabel = available.GetOrNull<LabelWidget>("MAP_TYPE");
				if (typeLabel != null)
					typeLabel.GetText = () => lobby.MapPreview.Type;

				var authorLabel = available.GetOrNull<LabelWidget>("MAP_AUTHOR");
				if (authorLabel != null)
				{
					var font = Game.Renderer.Fonts[authorLabel.Font];
					var author = new CachedTransform<MapPreview, string>(
						m => WidgetUtils.TruncateText("Created by {0}".F(lobby.MapPreview.Author), authorLabel.Bounds.Width, font));
					authorLabel.GetText = () => author.Update(lobby.MapPreview);
				}
			}

			var invalid = widget.GetOrNull("MAP_INVALID");
			if (invalid != null)
			{
				invalid.IsVisible = () => lobby.MapPreview.Status == MapStatus.Available && lobby.MapPreview.RuleStatus == MapRuleStatus.Invalid;

				var preview = invalid.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Preview = () => lobby.MapPreview;
				preview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint(orderManager, preview, lobby.MapPreview, mi);
				preview.SpawnOccupants = () => LobbyUtils.GetSpawnOccupants(orderManager.LobbyInfo, lobby.MapPreview);

				var title = invalid.GetOrNull<LabelWidget>("MAP_TITLE");
				if (title != null)
					title.GetText = () => lobby.MapPreview.Title;

				var type = invalid.GetOrNull<LabelWidget>("MAP_TYPE");
				if (type != null)
					type.GetText = () => lobby.MapPreview.Type;
			}

			var download = widget.GetOrNull("MAP_DOWNLOADABLE");
			if (download != null)
			{
				download.IsVisible = () => lobby.MapPreview.Status == MapStatus.DownloadAvailable;

				var preview = download.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Preview = () => lobby.MapPreview;
				preview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint(orderManager, preview, lobby.MapPreview, mi);
				preview.SpawnOccupants = () => LobbyUtils.GetSpawnOccupants(orderManager.LobbyInfo, lobby.MapPreview);

				var title = download.GetOrNull<LabelWidget>("MAP_TITLE");
				if (title != null)
					title.GetText = () => lobby.MapPreview.Title;

				var type = download.GetOrNull<LabelWidget>("MAP_TYPE");
				if (type != null)
					type.GetText = () => lobby.MapPreview.Type;

				var author = download.GetOrNull<LabelWidget>("MAP_AUTHOR");
				if (author != null)
					author.GetText = () => "Created by {0}".F(lobby.MapPreview.Author);

				var install = download.GetOrNull<ButtonWidget>("MAP_INSTALL");
				if (install != null)
				{
					install.OnClick = () => lobby.MapPreview.Install();
					install.IsHighlighted = () => installHighlighted;
				}
			}

			var progress = widget.GetOrNull("MAP_PROGRESS");
			if (progress != null)
			{
				progress.IsVisible = () =>
					(lobby.MapPreview.Status != MapStatus.Available || lobby.MapPreview.RuleStatus == MapRuleStatus.Unknown) &&
					lobby.MapPreview.Status != MapStatus.DownloadAvailable;

				var preview = progress.Get<MapPreviewWidget>("MAP_PREVIEW");
				preview.Preview = () => lobby.MapPreview;
				preview.OnMouseDown = mi => LobbyUtils.SelectSpawnPoint(orderManager, preview, lobby.MapPreview, mi);
				preview.SpawnOccupants = () => LobbyUtils.GetSpawnOccupants(orderManager.LobbyInfo, lobby.MapPreview);

				var title = progress.GetOrNull<LabelWidget>("MAP_TITLE");
				if (title != null)
					title.GetText = () => lobby.MapPreview.Title;

				var type = progress.GetOrNull<LabelWidget>("MAP_TYPE");
				if (type != null)
					type.GetText = () => lobby.MapPreview.Type;

				var statusSearching = progress.GetOrNull("MAP_STATUS_SEARCHING");
				if (statusSearching != null)
					statusSearching.IsVisible = () => lobby.MapPreview.Status == MapStatus.Searching;

				var statusUnavailable = progress.GetOrNull("MAP_STATUS_UNAVAILABLE");
				if (statusUnavailable != null)
					statusUnavailable.IsVisible = () => lobby.MapPreview.Status == MapStatus.Unavailable;

				var statusError = progress.GetOrNull("MAP_STATUS_ERROR");
				if (statusError != null)
					statusError.IsVisible = () => lobby.MapPreview.Status == MapStatus.DownloadError;

				var statusDownloading = progress.GetOrNull<LabelWidget>("MAP_STATUS_DOWNLOADING");
				if (statusDownloading != null)
				{
					statusDownloading.IsVisible = () => lobby.MapPreview.Status == MapStatus.Downloading;
					statusDownloading.GetText = () =>
					{
						if (lobby.MapPreview.DownloadBytes == 0)
							return "Connecting...";

						// Server does not provide the total file length
						if (lobby.MapPreview.DownloadPercentage == 0)
							return "Downloading {0} kB".F(lobby.MapPreview.DownloadBytes / 1024);

						return "Downloading {0} kB ({1}%)".F(lobby.MapPreview.DownloadBytes / 1024, lobby.MapPreview.DownloadPercentage);
					};
				}

				var retry = progress.GetOrNull<ButtonWidget>("MAP_RETRY");
				if (retry != null)
				{
					retry.IsVisible = () => (lobby.MapPreview.Status == MapStatus.DownloadError || lobby.MapPreview.Status == MapStatus.Unavailable) &&
						lobby.MapPreview != MapCache.UnknownMap;
					retry.OnClick = () =>
					{
						if (lobby.MapPreview.Status == MapStatus.DownloadError)
							lobby.MapPreview.Install();
						else if (lobby.MapPreview.Status == MapStatus.Unavailable)
							Game.ModData.MapCache.QueryRemoteMapDetails(new[] { lobby.MapPreview.Uid });
					};

					retry.GetText = () => lobby.MapPreview.Status == MapStatus.DownloadError ? "Retry Install" : "Retry Search";
				}

				var progressbar = progress.GetOrNull<ProgressBarWidget>("MAP_PROGRESSBAR");
				if (progressbar != null)
				{
					progressbar.IsIndeterminate = () => lobby.MapPreview.DownloadPercentage == 0;
					progressbar.GetPercentage = () => lobby.MapPreview.DownloadPercentage;
					progressbar.IsVisible = () => !retry.IsVisible();
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
	}
}
