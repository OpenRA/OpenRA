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
using OpenRA.Widgets;

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
				new[] { diskRoot, "music", "ambush.aud" },
				new[] { diskRoot, "setup", "setup.z" },
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
			var destData = new[] { Platform.SupportDir, "Content", "d2k" }.Aggregate(Path.Combine);
			var destSound = new[] { destData, "GAMESFX" }.Aggregate(Path.Combine);
			var copyFiles = new string[] { "music/ambush.aud", "music/arakatak.aud", "music/atregain.aud", "music/entordos.aud", "music/fightpwr.aud", "music/fremen.aud", "music/hark_bat.aud", "music/landsand.aud", "music/options.aud", "music/plotting.aud", "music/risehark.aud", "music/robotix.aud", "music/score.aud", "music/soldappr.aud", "music/spicesct.aud", "music/undercon.aud", "music/waitgame.aud" };

			var extractPackage = "setup/setup.z";
			var extractFiles = new string[] { "SOUND.RS", "DATA.R8", "MOUSE.R8", "BLOXBASE.R8", "BLOXBAT.R8", "BLOXBGBS.R8", "BLOXICE.R8", "BLOXTREE.R8", "BLOXWAST.R8" };
			var extractAudio = new string[] { "A_ECONF1.AUD", "A_ECONF2.AUD", "A_ECONF3.AUD", "A_ESEL1.AUD", "A_ESEL2.AUD", "A_ESEL3.AUD", 
				"A_FCONF1.AUD", "A_FCONF2.AUD", "A_FCONF3.AUD",	"A_FCONF4.AUD", "A_FSEL1.AUD", "A_FSEL2.AUD", "A_FSEL3.AUD", "A_FSEL4.AUD",
				"AI_1MIN.AUD", "AI_2MIN.AUD", "AI_3MIN.AUD", "AI_4MIN.AUD", "AI_5MIN.AUD", "AI_ABORT.AUD", "AI_ATACK.AUD", "AI_BDRDY.AUD", 
				"AI_BLOST.AUD", "AI_BUILD.AUD", "AI_CANCL.AUD", "AI_CAPT.AUD", "A_ICONF1.AUD", "A_ICONF2.AUD", "A_ICONF3.AUD", "AI_DHRDY.AUD",
				"AI_DPLOY.AUD", "AI_ENEMY.AUD", "AI_GANEW.AUD", "AI_GLOAD.AUD", "AI_GSAVE.AUD", "AI_GUARD.AUD", "AI_HATTK.AUD", "AI_HOLD.AUD",
				"AI_LAUNC.AUD", "AI_MAP1A.AUD", "AI_MAP1B.AUD", "AI_MAP1C.AUD", "AI_MAP2A.AUD", "AI_MAP2B.AUD", "AI_MAP2C.AUD", "AI_MAP3A.AUD",
				"AI_MAP4A.AUD", "AI_MAP5A.AUD", "AI_MAP6A.AUD", "AI_MAP7A.AUD", "AI_MAP8A.AUD", "AI_MAP9A.AUD", "AI_MEND.AUD", "AI_MFAIL.AUD",
				"AI_MONEY.AUD", "AI_MWIN.AUD", "AI_NEWOP.AUD", "AI_NROOM.AUD", "AI_ORDER.AUD", "AI_PLACE.AUD", "AI_POWER.AUD", "AI_PREP.AUD",
				"AI_PRMRY.AUD", "AI_REINF.AUD", "AI_RUN.AUD", "A_ISEL1.AUD", "A_ISEL2.AUD", "A_ISEL3.AUD", "AI_SELL.AUD", "AI_SILOS.AUD",
				"AI_SPORT.AUD", "AI_TRAIN.AUD", "AI_ULOST.AUD", "AI_UNRDY.AUD", "AI_UPGOP.AUD", "AI_UPGRD.AUD", "AI_WATTK.AUD", "AI_WSIGN.AUD",
				"A_VCONF1.AUD", "A_VCONF2.AUD", "A_VCONF3.AUD", "A_VSEL1.AUD", "A_VSEL2.AUD", "A_VSEL3.AUD", "G_SCONF1.AUD", "G_SCONF2.AUD",
				"G_SCONF3.AUD", "G_SSEL1.AUD", "G_SSEL2.AUD", "G_SSEL3.AUD", "H_ECONF1.AUD", "H_ECONF2.AUD", "H_ECONF3.AUD", "H_ESEL1.AUD",
				"H_ESEL2.AUD", "H_ESEL3.AUD", "HI_1MIN.AUD", "HI_2MIN.AUD", "HI_3MIN.AUD", "HI_4MIN.AUD", "HI_5MIN.AUD", "HI_ABORT.AUD",
				"HI_ATACK.AUD", "HI_BDRDY.AUD", "HI_BLOST.AUD", "HI_BUILD.AUD", "HI_CANCL.AUD", "HI_CAPT.AUD", "H_ICONF1.AUD", "H_ICONF2.AUD",
				"H_ICONF3.AUD", "HI_DHRDY.AUD", "HI_DPLOY.AUD", "HI_ENEMY.AUD", "HI_GANEW.AUD",	"HI_GLOAD.AUD", "HI_GSAVE.AUD", "HI_GUARD.AUD",
				"HI_HATTK.AUD", "HI_HOLD.AUD", "HI_LAUNC.AUD", "HI_MAP1A.AUD", "HI_MAP1B.AUD", "HI_MAP1C.AUD", "HI_MAP2A.AUD", "HI_MAP2B.AUD",
				"HI_MAP2C.AUD", "HI_MAP3A.AUD", "HI_MAP3B.AUD", "HI_MAP4A.AUD", "HI_MAP4B.AUD", "HI_MAP5A.AUD", "HI_MAP6A.AUD", "HI_MAP6B.AUD",
				"HI_MAP7A.AUD", "HI_MAP9A.AUD", "HI_MAP9.AUD", "HI_MEND.AUD", "HI_MFAIL.AUD", "HI_MONEY.AUD", "HI_MWIN.AUD", "HI_NEWOP.AUD",
				"HI_NROOM.AUD", "HI_ORDER.AUD", "HI_PLACE.AUD", "HI_POWER.AUD", "HI_PREP.AUD", "HI_PRMRY.AUD", "HI_REINF.AUD", "HI_RUN.AUD",
				"H_ISEL1.AUD", "H_ISEL2.AUD", "H_ISEL3.AUD", "HI_SELL.AUD", "HI_SILOS.AUD",	"HI_SPORT.AUD", "HI_TRAIN.AUD", "HI_ULOST.AUD",
				"HI_UNRDY.AUD", "HI_UPGOP.AUD", "HI_UPGRD.AUD", "HI_WATTK.AUD", "HI_WSIGN.AUD", "H_VCONF1.AUD", "H_VCONF2.AUD", "H_VCONF3.AUD",
				"H_VSEL1.AUD", "H_VSEL2.AUD", "H_VSEL3.AUD", "O_ECONF1.AUD", "O_ECONF2.AUD", "O_ECONF3.AUD", "O_ESEL1.AUD", "O_ESEL2.AUD",
				"O_ESEL3.AUD", "OI_1MIN.AUD", "OI_2MIN.AUD", "OI_3MIN.AUD", "OI_4MIN.AUD", "OI_5MIN.AUD", "OI_ABORT.AUD", "OI_ATACK.AUD",
				"OI_BDRDY.AUD", "OI_BLOST.AUD", "OI_BUILD.AUD", "OI_CANCL.AUD", "OI_CAPT.AUD", "O_ICONF1.AUD", "O_ICONF2.AUD", "O_ICONF3.AUD",
				"OI_DHRDY.AUD", "OI_DPLOY.AUD", "OI_ENEMY.AUD", "OI_GANEW.AUD", "OI_GLOAD.AUD", "OI_GSAVE.AUD", "OI_GUARD.AUD", "OI_HATTK.AUD",
				"OI_HOLD.AUD", "OI_LAUNC.AUD", "OI_MAP1A.AUD", "OI_MAP1B.AUD", "OI_MAP1C.AUD", "OI_MAP2A.AUD", "OI_MAP2B.AUD", "OI_MAP2C.AUD",
				"OI_MAP3A.AUD", "OI_MAP4A.AUD", "OI_MAP5A.AUD", "OI_MAP6A.AUD", "OI_MAP7A.AUD", "OI_MAP8A.AUD", "OI_MAP9A.AUD", "OI_MEND.AUD",
				"OI_MFAIL.AUD", "OI_MONEY.AUD", "OI_MWIN.AUD", "OI_NEWOP.AUD", "OI_NROOM.AUD", "OI_ORDER.AUD", "OI_PLACE.AUD", "OI_POWER.AUD",
				"OI_PREP.AUD", "OI_PRMRY.AUD", "OI_REINF.AUD", "OI_RUN.AUD", "O_ISEL1.AUD", "O_ISEL2.AUD", "O_ISEL3.AUD", "OI_SELL.AUD",
				"OI_SILOS.AUD", "OI_SPORT.AUD", "OI_TRAIN.AUD", "OI_ULOST.AUD", "OI_UNRDY.AUD", "OI_UPGOP.AUD", "OI_UPGRD.AUD", "OI_WATTK.AUD",
				"OI_WSIGN.AUD", "O_SCONF1.AUD", "O_SCONF2.AUD", "O_SCONF3.AUD", "O_SSEL1.AUD", "O_SSEL2.AUD", "O_SSEL3.AUD", "O_VCONF1.AUD",
				"O_VCONF2.AUD", "O_VCONF3.AUD", "O_VSEL1.AUD", "O_VSEL2.AUD", "O_VSEL3.AUD" };

			var installCounter = 0;
			var installTotal = copyFiles.Count() + extractFiles.Count() + extractAudio.Count();

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

			var t = new Thread(_ =>
			{
				try
				{
					if (!InstallUtils.CopyFiles(source, copyFiles, destMusic, onProgress, onError))
						return;

					if (!InstallUtils.ExtractFromPackage(source, extractPackage, extractFiles, destData, onProgress, onError))
						return;

					if (!InstallUtils.ExtractFromPackage(source, extractPackage, extractAudio, destSound, onProgress, onError))
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
