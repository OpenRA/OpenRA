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
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class DownloadPackagesLogic
	{
		Widget panel;
		Dictionary<string,string> installData;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action afterInstall;
		
		[ObjectCreator.UseCtor]
		public DownloadPackagesLogic(Widget widget, Dictionary<string,string> installData, Action afterInstall)
		{
			this.installData = installData;
			this.afterInstall = afterInstall;

			panel = widget.Get("INSTALL_DOWNLOAD_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");

			ShowDownloadDialog();
		}

		void ShowDownloadDialog()
		{
			statusLabel.GetText = () => "Initializing...";
			progressBar.SetIndeterminate(true);
			var retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.IsVisible = () => false;

			var cancelButton = panel.Get<ButtonWidget>("CANCEL_BUTTON");

			// Save the package to a temp file
			var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			var dest = new string[] { Platform.SupportDir, "Content", Game.modData.Manifest.Mods[0] }.Aggregate(Path.Combine);

			Action<DownloadProgressChangedEventArgs> onDownloadProgress = i =>
			{
				if (progressBar.Indeterminate)
					progressBar.SetIndeterminate(false);

				progressBar.Percentage = i.ProgressPercentage;
				statusLabel.GetText = () => "Downloading {1}/{2} kB ({0}%)".F(i.ProgressPercentage, i.BytesReceived / 1024, i.TotalBytesToReceive / 1024);
			};

			Action<string> onExtractProgress = s =>
			{
					Game.RunAfterTick(() => statusLabel.GetText = () => s);
			};

			Action<string> onError = s =>
			{
				Game.RunAfterTick(() =>
				{
					statusLabel.GetText = () => "Error: "+s;
					retryButton.IsVisible = () => true;
				});
			};

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
				progressBar.SetIndeterminate(true);
				if (InstallUtils.ExtractZip(file, dest, onExtractProgress, onError))
				{
					Game.RunAfterTick(() =>
					{
						Ui.CloseWindow();
						afterInstall();
					});
				}
			};

			var dl = new Download(installData["PackageURL"], file, onDownloadProgress, onDownloadComplete);

			cancelButton.OnClick = () => { dl.Cancel(); Ui.CloseWindow(); };
			retryButton.OnClick = () => { dl.Cancel(); ShowDownloadDialog(); };
		}
	}
}
