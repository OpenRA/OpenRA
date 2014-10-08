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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class DownloadPackagesLogic
	{
		readonly Widget panel;
		readonly string mirrorListUrl;
		readonly ProgressBarWidget progressBar;
		readonly LabelWidget statusLabel;
		readonly Action afterInstall;
		string mirror;
		static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

		[ObjectCreator.UseCtor]
		public DownloadPackagesLogic(Widget widget, Action afterInstall, string mirrorListUrl)
		{
			this.mirrorListUrl = mirrorListUrl;
			this.afterInstall = afterInstall;

			panel = widget.Get("INSTALL_DOWNLOAD_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");

			ShowDownloadDialog();
		}

		void ShowDownloadDialog()
		{
			statusLabel.GetText = () => "Fetching list of mirrors...";
			progressBar.Indeterminate = true;

			var retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.IsVisible = () => false;

			var cancelButton = panel.Get<ButtonWidget>("CANCEL_BUTTON");

			var mirrorsFile = Platform.ResolvePath("^", "Content", Game.modData.Manifest.Mod.Id, "mirrors.txt");
			var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			var dest = Platform.ResolvePath("^", "Content", Game.modData.Manifest.Mod.Id);

			Action<DownloadProgressChangedEventArgs> onDownloadProgress = i =>
			{
				var dataReceived = 0.0f;
				var dataTotal = 0.0f;
				var mag = 0;
				var dataSuffix = "";

				if (i.TotalBytesToReceive < 0)
				{
					dataTotal = float.NaN;
					dataReceived = i.BytesReceived;
					dataSuffix = SizeSuffixes[0];
				}
				else
				{
					mag = (int)Math.Log(i.TotalBytesToReceive, 1024);
					dataTotal = i.TotalBytesToReceive / (float)(1L << (mag * 10));
					dataReceived = i.BytesReceived / (float)(1L << (mag * 10));
					dataSuffix = SizeSuffixes[mag];
				}


				progressBar.Indeterminate = false;
				progressBar.Percentage = i.ProgressPercentage;

				statusLabel.GetText = () => "Downloading from {4} {1:0.00}/{2:0.00} {3} ({0}%)".F(i.ProgressPercentage,
					dataReceived, dataTotal, dataSuffix,
					mirror != null ? new Uri(mirror).Host : "unknown host");
			};

			Action<string> onExtractProgress = s => Game.RunAfterTick(() => statusLabel.GetText = () => s);

			Action<string> onError = s => Game.RunAfterTick(() =>
			{
				statusLabel.GetText = () => "Error: " + s;
				retryButton.IsVisible = () => true;
			});

			Action<AsyncCompletedEventArgs, bool> onDownloadComplete = (i, cancelled) =>
			{
				if (i.Error != null)
				{
					onError(Download.FormatErrorMessage(i.Error));
					return;
				}
				else if (cancelled)
				{
					onError("Download cancelled");
					return;
				}

				// Automatically extract
				statusLabel.GetText = () => "Extracting...";
				progressBar.Indeterminate = true;
				if (InstallUtils.ExtractZip(file, dest, onExtractProgress, onError))
				{
					Game.RunAfterTick(() =>
					{
						Ui.CloseWindow();
						afterInstall();
					});
				}
			};

			Action<AsyncCompletedEventArgs, bool> onFetchMirrorsComplete = (i, cancelled) =>
			{
				progressBar.Indeterminate = true;

				if (i.Error != null)
				{
					onError(Download.FormatErrorMessage(i.Error));
					return;
				}
				else if (cancelled)
				{
					onError("Download cancelled");
					return;
				}

				var mirrorList = new List<string>();
				using (var r = new StreamReader(mirrorsFile))
				{
					string line;
					while ((line = r.ReadLine()) != null)
						if (!string.IsNullOrEmpty(line))
							mirrorList.Add(line);
				}
				mirror = mirrorList.Random(new MersenneTwister());

				// Save the package to a temp file
				var dl = new Download(mirror, file, onDownloadProgress, onDownloadComplete);
				cancelButton.OnClick = () => { dl.Cancel(); Ui.CloseWindow(); };
				retryButton.OnClick = () => { dl.Cancel(); ShowDownloadDialog(); };
			};

			// Get the list of mirrors
			var updateMirrors = new Download(mirrorListUrl, mirrorsFile, onDownloadProgress, onFetchMirrorsComplete);
			cancelButton.OnClick = () => { updateMirrors.Cancel(); Ui.CloseWindow(); };
			retryButton.OnClick = () => { updateMirrors.Cancel(); ShowDownloadDialog(); };
		}
	}
}
