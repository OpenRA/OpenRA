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

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	class CncOptionalInstallFromCDLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		ButtonWidget retryButton, backButton;
		Widget installingContainer, insertDiskContainer, installedContainer;

		string checkFile;
		string[] discFiles;
		string[] destFiles;
		string discHash;

		Boolean hashok = false;

		[ObjectCreator.UseCtor]
		public CncOptionalInstallFromCDLogic(Widget widget)
		{
			switch (CncOptionalInstallMenuLogic.ContentID)
			{
				case 0:
					panel = widget.Get("INSTALL_FROMCD_GDI_PANEL");
					break;

				case 1:
					panel = widget.Get("INSTALL_FROMCD_NOD_PANEL");
					break;

				case 2:
					panel = widget.Get("INSTALL_FROMCD_COVERTOPS_PANEL");
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
			switch (CncOptionalInstallMenuLogic.ContentID)
			{
				case 0: //GDI Disc
					checkFile = "movies.mix";
					discFiles = new string[] { "movies.mix", "scores.mix" };
					destFiles = new string[] { "movies-gdi.mix", "scores.mix" };
					discHash = "BC-D7-05-96-F4-97-12-8C-8B-56-11-97-39-B4-EB-D2-4F-18-A2-0A";
					break;

				case 1: //Nod Disc
					checkFile = "movies.mix";
					discFiles = new string[] { "movies.mix", "scores.mix" };
					destFiles = new string[] { "movies-nod.mix", "scores.mix" };
					discHash = "3B-A8-35-51-E7-74-99-72-B4-4F-9C-B1-79-F9-F5-C9-FE-C3-29-41";
					break;

				case 2: //Covert Operations Disc
					checkFile = "movies.mix";
					discFiles = new string[] { "movies.mix", "scores.mix" };
					destFiles = new string[] { "movies-cop.mix", "scores2.mix" };
					discHash = "26-8F-30-64-C3-C4-BC-A1-E4-C6-C1-26-E8-7A-8D-44-D4-80-17-F5";
					break;

				default:
					break;
			}
		}

		void CheckExistingContent()
		{
			if (!FileSystem.Exists(destFiles[0]))
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
			Func<string, bool> ValidDiskFilter = diskRoot => File.Exists(diskRoot + Path.DirectorySeparatorChar + discFiles[0]) &&
					File.Exists(diskRoot + Path.DirectorySeparatorChar + discFiles[1]);

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

						var file = File.OpenRead(path + Path.DirectorySeparatorChar + discFiles[0]);
						using (var cryptoProvider = new SHA1CryptoServiceProvider())
						{
							string hash = BitConverter
									.ToString(cryptoProvider.ComputeHash(file));
							if (discHash == hash) //locks up if checksum invalid
								hashok = true;
						}
						if (hashok)
						{
							Install(path);
						}
					}
					else
					{
						installingContainer.IsVisible = () => false;
						insertDiskContainer.IsVisible = () => true;
					}
				}) { IsBackground = true }.Start();
		}

		void ResetContentArrays()
		{
			hashok = false;
			discHash = "";
			checkFile = "";
			Array.Clear(discFiles, 0, discFiles.Length);
			Array.Clear(destFiles, 0, destFiles.Length);
		}

		void Install(string source)
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			insertDiskContainer.IsVisible = () => false;
			installingContainer.IsVisible = () => true;

			var dest = new string[] { Platform.SupportDir, "Content", "cnc" }.Aggregate(Path.Combine);

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
					switch (CncOptionalInstallMenuLogic.ContentID)
					{
						case 0:
							//check for existing file
							if (File.Exists(dest + Path.DirectorySeparatorChar + discFiles[1]))
							{
								discFiles = new string[] { "movies.mix" }; //couldn't figure out how to delete array index
							}
							if (!InstallUtils.CopyRenameFiles(source, discFiles, dest, destFiles, onProgress, onError))
								return;

							ResetContentArrays();
							break;

						case 1:
							if (File.Exists(dest + Path.DirectorySeparatorChar + discFiles[1]))
							{
								discFiles = new string[] { "movies.mix" };
							}
							if (!InstallUtils.CopyRenameFiles(source, discFiles, dest, destFiles, onProgress, onError))
								return;

							ResetContentArrays();
							break;

						case 2:
							if (!InstallUtils.CopyRenameFiles(source, discFiles, dest, destFiles, onProgress, onError))
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
