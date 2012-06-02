#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.D2k.Widgets.Logic
{
	public class D2kInstallFromCDLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		ButtonWidget retryButton, backButton;
		Widget installingContainer, insertDiskContainer;

		[ObjectCreator.UseCtor]
		public D2kInstallFromCDLogic(Widget widget)
		{
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

		public static bool IsValidDisk(string diskRoot)
		{
			var files = new string[][] {
				new [] { diskRoot, "music", "ambush.aud" },
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

			var dest = new string[] { Platform.SupportDir, "Content", "d2k" }.Aggregate(Path.Combine);
			var copyFiles = new string[] { "music/ambush.aud" };

			var extractPackage = "setup/setup.z";
			var extractFiles = new string[] { "DATA.R8", "MOUSE.R8", "BLOXBASE.R8" };

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
						Game.Exit();
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
