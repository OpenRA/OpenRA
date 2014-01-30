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
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.TS.Widgets.Logic
{
	class TSOptionalInstallFromCDLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		ButtonWidget retryButton, backButton;
		Widget installingContainer, insertDiskContainer, installedContainer;

		string checkFile;
		string[] discFiles;
		string discHash;

		Boolean hashok = false;

		[ObjectCreator.UseCtor]
		public TSOptionalInstallFromCDLogic(Widget widget)
		{
			switch (TSOptionalInstallMenuLogic.ContentID)
			{
				case 0:
					panel = widget.Get("INSTALL_FROMCD_GDI_PANEL");
					break;

				case 1:
					panel = widget.Get("INSTALL_FROMCD_NOD_PANEL");
					break;

				case 2:
					panel = widget.Get("INSTALL_FROMCD_FIRESTORM_PANEL");
					break;

				default:
					break;
			}
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");

			backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = Ui.CloseWindow;

			retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.OnClick = CheckForDisk;

			installingContainer = panel.Get("INSTALLING");
			insertDiskContainer = panel.Get("INSERT_DISK");
			installedContainer = panel.Get("INSTALLED");
			SetInstallableContent();
			CheckExistingContent();
		}

		void SetInstallableContent()
		{
			switch (TSOptionalInstallMenuLogic.ContentID)
			{
				case 0: //GDI Disc
					checkFile = "ts1.dsk"; //same as comment on ts2.dsk
					discFiles = new string[] { "movies01.mix", "scores.mix" };
					discHash = "CE-33-15-C4-FA-F7-D7-7D-B2-D2-30-7D-2D-17-1E-8D-BE-91-48-97"; 
					break;

				case 1: //NOD Disc
					checkFile = "ts2.dsk"; //these should be CAPS my system doesn't want to see that :\
					discFiles = new string[] { "movies02.mix", "scores.mix" };
					discHash = "4B-2E-EE-3E-28-33-EC-16-DF-FB-41-4D-69-8B-CF-E6-67-9C-65-94";
					break;

				case 2: //Firestorm Expansion
					checkFile = "ts3.dsk";
					discFiles = new string[] { "movies03.mix", "scores01.mix" };
					discHash = "BC-56-44-49-A1-5A-61-E5-D3-50-C2-63-2D-32-7A-B3-86-D9-91-C9";
					break;

				default:
					break;
			}
		}

		void CheckExistingContent()
		{
			if (!FileSystem.Exists(discFiles[0]))
			{
				CheckForDisk();
			}
			else
			{
				installedContainer.IsVisible = () => true;
				retryButton.IsVisible = () => false;
			}
		}

		void CheckForDisk()
		{
			Func<string, bool> ValidDiskFilter = diskRoot => File.Exists(diskRoot + Path.DirectorySeparatorChar + discFiles[0]) 
				&& File.Exists(diskRoot + Path.DirectorySeparatorChar + discFiles[1]);

			var path = InstallUtils.GetMountedDisk(ValidDiskFilter);

			new Thread(() =>
			{
				if (path != null)
				{
					backButton.IsDisabled = () => true;
					retryButton.IsDisabled = () => true;
					insertDiskContainer.IsVisible = () => false;
					installingContainer.IsVisible = () => true;

					statusLabel.GetText = () => "Verifying...";
					progressBar.SetIndeterminate(true);

					var file = File.OpenRead(path + Path.DirectorySeparatorChar + checkFile);
					using (var cryptoProvider = new SHA1CryptoServiceProvider())
					{
						string hash = BitConverter
								.ToString(cryptoProvider.ComputeHash(file));
						if (discHash == hash)
							hashok = true;
					}
					if (hashok)
					{
						Install(path);
					}
				}
				else
				{
					insertDiskContainer.IsVisible = () => true;
					installingContainer.IsVisible = () => false;
				}
			}) { IsBackground = true }.Start();
		}

		void ResetContentArrays()
		{
			discHash = "";
			hashok = false;
			checkFile = "";
			Array.Clear(discFiles, 0, discFiles.Length);
		}

		void Install(string source)
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			insertDiskContainer.IsVisible = () => false;
			installingContainer.IsVisible = () => true;

			var dest = new string[] { Platform.SupportDir, "Content", "ts" }.Aggregate(Path.Combine);

			var installCounter = 0;
			var installTotal = discFiles.Count();
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
					switch (TSOptionalInstallMenuLogic.ContentID)
					{
						case 0:
							if (File.Exists(dest + Path.DirectorySeparatorChar + discFiles[1]))
								discFiles = new string[] { "movies01.mix" };
							if (!InstallUtils.CopyFiles(source, discFiles, dest, onProgress, onError))
								return;

							ResetContentArrays();
							break;

						case 1:
							if (File.Exists(dest + Path.DirectorySeparatorChar + discFiles[1]))
								discFiles = new string[] { "movies02.mix" };
							if (!InstallUtils.CopyFiles(source, discFiles, dest, onProgress, onError))
								return;

							ResetContentArrays();
							break;

						case 2:
							if (!InstallUtils.CopyFiles(source, discFiles, dest, onProgress, onError))
								return;

							ResetContentArrays();
							break;

						default:
							break;
					}

					Game.RunAfterTick(() =>
					{
						Ui.CloseWindow();
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
