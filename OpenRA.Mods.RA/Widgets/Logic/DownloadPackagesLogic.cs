#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
		readonly InstallData installData;
		readonly ProgressBarWidget progressBar;
		readonly LabelWidget statusLabel;
		readonly Action afterInstall;
		string mirror;

		[ObjectCreator.UseCtor]
		public DownloadPackagesLogic(Widget widget, Action afterInstall)
		{
			installData = Game.modData.Manifest.ContentInstaller;
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

			var mirrorsFile = new string[] { Platform.SupportDir, "Content", Game.modData.Manifest.Mod.Id, "mirrors.txt" }.Aggregate(Path.Combine);
			var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			var dest = new string[] { Platform.SupportDir, "Content", Game.modData.Manifest.Mod.Id }.Aggregate(Path.Combine);

			Action<DownloadProgressChangedEventArgs> onDownloadProgress = i =>
			{
				progressBar.Indeterminate = false;
				progressBar.Percentage = i.ProgressPercentage;
				statusLabel.GetText = () => "Downloading from {3} {1}/{2} kB ({0}%)".F(i.ProgressPercentage,
					i.BytesReceived / 1024, i.TotalBytesToReceive / 1024, 
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
			var updateMirrors = new Download(installData.PackageMirrorList, mirrorsFile, onDownloadProgress, onFetchMirrorsComplete);
			cancelButton.OnClick = () => { updateMirrors.Cancel(); Ui.CloseWindow(); };
			retryButton.OnClick = () => { updateMirrors.Cancel(); ShowDownloadDialog(); };
		}
	}
}
