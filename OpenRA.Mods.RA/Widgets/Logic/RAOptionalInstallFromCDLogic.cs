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

namespace OpenRA.Mods.RA.Widgets.Logic
{
	class RAOptionalInstallFromCDLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		ButtonWidget retryButton, backButton;
		Widget installingContainer, insertDiskContainer, installedContainer;

		string extractPackage;
		string[] extractFiles;
		string[] discFiles;
		string discHash;

		Boolean hashok = false;

		[ObjectCreator.UseCtor]
		public RAOptionalInstallFromCDLogic(Widget widget)
		{
			switch (RAOptionalInstallMenuLogic.ContentID)
			{
				case 0:
					panel = widget.Get("INSTALL_FROMCD_ALLIES_PANEL");
					break;

				case 1:
					panel = widget.Get("INSTALL_FROMCD_SOVIET_PANEL");
					break;

				case 3:
					panel = widget.Get("INSTALL_FROMCD_COUNTERSTRIKE_PANEL");
					break;

				case 4:
					panel = widget.Get("INSTALL_FROMCD_AFTERMATH_PANEL");
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
			switch (RAOptionalInstallMenuLogic.ContentID)
			{
				case 0: //Allies Disc
					extractPackage = "main.mix";
					extractFiles = new string[] { "movies1.mix" };
					discFiles = new string[] { "main.mix", "redalert.mix" };
					discHash = "99-10-43-79-47-2B-BC-FB-70-C7-E3-78-DE-18-D5-AA-86-91-8B-D4"; //this is the sha1 of the main.mix on my cd others may be different
					break;

				case 1: //Soviet Disc
					extractPackage = "main.mix";
					extractFiles = new string[] { "movies2.mix"};
					discFiles = new string[] { "main.mix", "redalert.mix" };
					discHash = "E2-37-44-A1-EC-2E-EF-CE-DA-9B-58-4D-B6-66-80-F7-7E-77-34-AD";
					break;

				case 2:
					//scores.mix conflicts
					break;

				case 3:
					//scores.mix conflicts
					break;

				default:
					break;
			}
		}

		void CheckExistingContent()
		{
			if (!FileSystem.Exists(extractFiles[0]))
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
					File.Exists(new string[] { diskRoot, "install", discFiles[1] }.Aggregate(Path.Combine));

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
			extractPackage = "";
			Array.Clear(extractFiles, 0, extractFiles.Length);
			Array.Clear(discFiles, 0, discFiles.Length);
		}

		void Install(string source)
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			insertDiskContainer.IsVisible = () => false;
			installingContainer.IsVisible = () => true;

			var dest = new string[] { Platform.SupportDir, "Content", "ra" }.Aggregate(Path.Combine);

			var installCounter = 0;
			var installTotal = extractFiles.Count();
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
					switch (RAOptionalInstallMenuLogic.ContentID)
					{
						case 0:
							if (!InstallUtils.ExtractFromPackage(source, extractPackage, extractFiles, dest, onProgress, onError))
								return;

							ResetContentArrays();
							break;

						case 1:
							if (!InstallUtils.ExtractFromPackage(source, extractPackage, extractFiles, dest, onProgress, onError))
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
