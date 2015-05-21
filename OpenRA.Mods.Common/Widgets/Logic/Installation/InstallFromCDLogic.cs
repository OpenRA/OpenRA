#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class InstallFromCDLogic
	{
		readonly Widget panel;
		readonly ProgressBarWidget progressBar;
		readonly LabelWidget statusLabel;
		readonly Action continueLoading;
		readonly ButtonWidget retryButton, backButton;
		readonly Widget installingContainer, insertDiskContainer;
		readonly ContentInstaller installData;

		[ObjectCreator.UseCtor]
		public InstallFromCDLogic(Widget widget, Action continueLoading)
		{
			installData = Game.ModData.Manifest.Get<ContentInstaller>();
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
			return installData.DiskTestFiles.All(f => File.Exists(Path.Combine(diskRoot, f)));
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

			var dest = Platform.ResolvePath("^", "Content", Game.ModData.Manifest.Mod.Id);
			var copyFiles = installData.CopyFilesFromCD;

			var packageToExtract = installData.PackageToExtractFromCD.Split(':');
			var extractPackage = packageToExtract.First();
			var annotation = packageToExtract.Length > 1 ? packageToExtract.Last() : null;

			var extractFiles = installData.ExtractFilesFromCD;

			var installCounter = 0;
			var installTotal = copyFiles.Length + extractFiles.Length;
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

			new Thread(() =>
			{
				try
				{
					if (!InstallUtils.CopyFiles(source, copyFiles, dest, onProgress, onError))
					{
						onError("Copying files from CD failed.");
						return;
					}

					if (!string.IsNullOrEmpty(extractPackage))
					{
						if (!InstallUtils.ExtractFromPackage(source, extractPackage, annotation, extractFiles, dest, onProgress, onError))
						{
							onError("Extracting files from CD failed.");
							return;
						}
					}

					Game.RunAfterTick(() =>
					{
						statusLabel.GetText = () => "Game assets have been extracted.";
						Ui.CloseWindow();
						continueLoading();
					});
				}
				catch (Exception e)
				{
					onError("Installation failed.\n{0}".F(e.Message));
					Log.Write("debug", e.ToString());
					return;
				}
			}) { IsBackground = true }.Start();
		}
	}
}
