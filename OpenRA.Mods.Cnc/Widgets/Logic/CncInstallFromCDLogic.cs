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

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncInstallFromCDLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action afterInstall;
		ButtonWidget retryButton, backButton;
		Widget installingContainer, insertDiskContainer;

		string[] filesToCopy, filesToExtract;

		[ObjectCreator.UseCtor]
		public CncInstallFromCDLogic(Widget widget, Action afterInstall, string[] filesToCopy, string[] filesToExtract)
		{
			this.afterInstall = afterInstall;
			this.filesToCopy = filesToCopy;
			this.filesToExtract = filesToExtract;

			panel = widget;
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

		public static bool IsValidDisk(string diskRoot)
		{
			var files = new string[][] {
				new[] { diskRoot, "CONQUER.MIX" },
				new[] { diskRoot, "DESERT.MIX" },
				new[] { diskRoot, "INSTALL", "SETUP.Z" },
			};

			return files.All(f => File.Exists(f.Aggregate(Path.Combine)));
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

			var dest = Platform.GetFolderPath(UserFolder.ModContent);
			var extractPackage = "INSTALL/SETUP.Z";

			var installCounter = 0;
			var installTotal = filesToExtract.Count() + filesToExtract.Count();
			var onProgress = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				progressBar.Percentage = installCounter * 100 / installTotal;
				installCounter++;

				statusLabel.GetText = () => s;
			}));

			var onError = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				statusLabel.GetText = () => "Error: " + s;
				backButton.IsDisabled = () => false;
				retryButton.IsDisabled = () => false;
			}));

			new Thread(_ =>
			{
				try
				{
					if (!InstallUtils.CopyFiles(source, filesToCopy, dest, onProgress, onError))
						return;

					if (!InstallUtils.ExtractFromPackage(source, extractPackage, filesToExtract, dest, onProgress, onError))
						return;

					Game.RunAfterTick(() =>
					{
						Ui.CloseWindow();
						afterInstall();
					});
				}
				catch
				{
					onError("Installation failed");
				}
			}) { IsBackground = true }.Start();
		}
	}
}
