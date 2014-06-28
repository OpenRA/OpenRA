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
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class InstallFromCDLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action continueLoading;
		ButtonWidget retryButton, backButton;
		Widget installingContainer, insertDiskContainer;

		[ObjectCreator.UseCtor]
		public InstallFromCDLogic(Widget widget, Action continueLoading)
		{
			this.continueLoading = continueLoading;
			panel = widget.Get("INSTALL_FROMCD_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");

			backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = Ui.CloseWindow;

			retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.OnClick = CheckForDisk;

			installingContainer = panel.Get("INSTALLING");
			insertDiskContainer = panel.Get("INSERT_DISK");
			CheckForDisk();
		}

		bool IsValidDisk(string diskRoot)
		{
			var files = Game.modData.Manifest.ContentInstaller["DiskTestFiles"].Split(',');
			return files.All(f => File.Exists(Path.Combine(diskRoot, f)));
		}

		void CheckForDisk()
		{
			var path = InstallUtils.GetMountedDisk(IsValidDisk);

			if (path != null)
				Install(path);
			else
			{
				insertDiskContainer.IsVisible = () => true;
				installingContainer.IsVisible = () => false;
			}
		}

		void Install(string source)
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			insertDiskContainer.IsVisible = () => false;
			installingContainer.IsVisible = () => true;

			var dest = new string[] { Platform.SupportDir, "Content", Game.modData.Manifest.Mod.Id }.Aggregate(Path.Combine);
			var copyFiles = Game.modData.Manifest.ContentInstaller["CopyFilesFromCD"].Split(',');

			var extractPackage = Game.modData.Manifest.ContentInstaller["PackageToExtractFromCD"];
			var extractFiles = Game.modData.Manifest.ContentInstaller["ExtractFilesFromCD"].Split(',');

			var installCounter = 0;
			var installTotal = copyFiles.Count() + extractFiles.Count();
			var onProgress = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				progressBar.Percentage = installCounter * 100 / installTotal;
				installCounter++;

				statusLabel.GetText = () => s;
			}));

			var onError = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				statusLabel.GetText = () => "Error: "+s;
				backButton.IsDisabled = () => false;
				retryButton.IsDisabled = () => false;
			}));

			var t = new Thread( _ =>
			{
				try
				{
					if (!InstallUtils.CopyFiles(source, copyFiles, dest, onProgress, onError))
						return;

					if (!string.IsNullOrEmpty(extractPackage))
						if (!InstallUtils.ExtractFromPackage(source, extractPackage, extractFiles, dest, onProgress, onError))
							return;

					Game.RunAfterTick(() =>
					{
						statusLabel.GetText = () => "Game assets have been extracted.";
						Ui.CloseWindow();
						continueLoading();
					});
				}
				catch
				{
					onError("Installation failed");
				}
			}) { IsBackground = true };
			t.Start();
		}
	}
}
