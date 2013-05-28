#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Utility;

namespace OpenRA.Mods.D2k.Widgets.Logic
{
	public class D2kInstallFromCDLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action continueLoading;
		ButtonWidget retryButton, backButton;
		Widget installingContainer, insertDiskContainer;

		[ObjectCreator.UseCtor]
		public D2kInstallFromCDLogic(Widget widget, Action continueLoading)
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
			this.continueLoading = continueLoading;
		}

		public static bool IsValidDisk(string diskRoot)
		{
			var files = new string[][] {
				new [] { diskRoot, "music", "ambush.aud" },
				new [] { diskRoot, "setup", "setup.z" },
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

			var destMusic = new string[] { Platform.SupportDir, "Content", "d2k", "Music" }.Aggregate(Path.Combine);
			var destData = new [] { Platform.SupportDir, "Content", "d2k" }.Aggregate (Path.Combine);
			var copyFiles = new string[] { "music/ambush.aud", "music/arakatak.aud", "music/atregain.aud", "music/entordos.aud", "music/fightpwr.aud", "music/fremen.aud", "music/hark_bat.aud", "music/landsand.aud", "music/options.aud", "music/plotting.aud", "music/risehark.aud", "music/robotix.aud", "music/score.aud", "music/soldappr.aud", "music/spicesct.aud", "music/undercon.aud", "music/waitgame.aud" };

			var extractPackage = "setup/setup.z";
			var extractFiles = new string[] { "SOUND.RS", "DATA.R8", "MOUSE.R8", "BLOXBASE.R8", "BLOXBAT.R8", "BLOXBGBS.R8", "BLOXICE.R8", "BLOXTREE.R8", "BLOXWAST.R8" };

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
					if (!InstallUtils.CopyFiles(source, copyFiles, destMusic, onProgress, onError))
						return;

					if (!InstallUtils.ExtractFromPackage(source, extractPackage, extractFiles, destData, onProgress, onError))
						return;

					Game.RunAfterTick(() =>
					{
						statusLabel.GetText = () => "Game assets have been extracted.";
						backButton.IsDisabled = () => false;
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
