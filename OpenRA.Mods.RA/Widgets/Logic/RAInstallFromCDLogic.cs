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
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class RAInstallFromCDLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action continueLoading;
		ButtonWidget retryButton, backButton;
		Widget installingContainer, insertDiskContainer;

		[ObjectCreator.UseCtor]
		public RAInstallFromCDLogic(Widget widget, Action continueLoading)
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

		void CheckForDisk()
		{
			Func<string, bool> ValidDiskFilter = diskRoot => File.Exists(diskRoot+Path.DirectorySeparatorChar+"MAIN.MIX") &&
					File.Exists(new string[] { diskRoot, "INSTALL", "REDALERT.MIX" }.Aggregate(Path.Combine));

			var path = InstallUtils.GetMountedDisk(ValidDiskFilter);

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

			var dest = Platform.GetFolderPath(UserFolder.ModContent);
			var copyFiles = new string[] { "INSTALL/REDALERT.MIX" };

			var extractPackage = "MAIN.MIX";
			var extractFiles = new string[] { "conquer.mix", "russian.mix", "allies.mix", "sounds.mix",
					"scores.mix", "snow.mix", "interior.mix", "temperat.mix" };

			var installCounter = 0;
			var installTotal = copyFiles.Count() + extractFiles.Count();
			var onProgress = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				progressBar.Percentage = installCounter*100/installTotal;
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

					if (!InstallUtils.ExtractFromPackage(source, extractPackage, extractFiles, dest, onProgress, onError))
						return;

					Game.RunAfterTick(() =>
					{
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
