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
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncInstallFromCDLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action continueLoading;
		ButtonWidget retryButton, backButton;
		Widget installingContainer, insertDiskContainer;

		[ObjectCreator.UseCtor]
		public CncInstallFromCDLogic([ObjectCreator.Param] Widget widget,
		                       [ObjectCreator.Param] Action continueLoading)
		{
			this.continueLoading = continueLoading;
			panel = widget.GetWidget("INSTALL_FROMCD_PANEL");
			progressBar = panel.GetWidget<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.GetWidget<LabelWidget>("STATUS_LABEL");

			backButton = panel.GetWidget<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = Widget.CloseWindow;

			retryButton = panel.GetWidget<ButtonWidget>("RETRY_BUTTON");
			retryButton.OnClick = CheckForDisk;

			installingContainer = panel.GetWidget("INSTALLING");
			insertDiskContainer = panel.GetWidget("INSERT_DISK");
			CheckForDisk();
		}

		void CheckForDisk()
		{
			Func<string, bool> ValidDiskFilter = diskRoot => File.Exists(diskRoot+Path.DirectorySeparatorChar+"CONQUER.MIX") &&
					File.Exists(diskRoot+Path.DirectorySeparatorChar+"DESERT.MIX") &&
					File.Exists(new string[] { diskRoot, "INSTALL", "SETUP.Z" }.Aggregate(Path.Combine));

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

			var dest = new string[] { Platform.SupportDir, "Content", "cnc" }.Aggregate(Path.Combine);
			var copyFiles = new string[] { "CONQUER.MIX", "DESERT.MIX",
					"SCORES.MIX", "SOUNDS.MIX", "TEMPERAT.MIX", "WINTER.MIX" };

			var extractPackage = "INSTALL/SETUP.Z";
			var extractFiles = new string[] { "speech.mix", "tempicnh.mix", "transit.mix" };

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
						Widget.CloseWindow();
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
