#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using System.Threading;
using OpenRA.FileSystem;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class InstallFromCDLogic : ChromeLogic
	{
		readonly ModData modData;
		readonly string modId;
		readonly Widget panel;
		readonly ProgressBarWidget progressBar;
		readonly LabelWidget statusLabel;
		readonly Action afterInstall;
		readonly ButtonWidget retryButton, backButton;
		readonly Widget installingContainer, insertDiskContainer;
		readonly ContentInstaller installData;

		[ObjectCreator.UseCtor]
		public InstallFromCDLogic(Widget widget, ModData modData, Action afterInstall, string modId)
		{
			this.modData = modData;
			this.modId = modId;
			installData = ModMetadata.AllMods[modId].Content;
			this.afterInstall = afterInstall;
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

		bool IsTFD(string diskpath)
		{
			var test = File.Exists(Path.Combine(diskpath, "data1.hdr"));
			var i = 0;

			while (test && i < 14)
				test &= File.Exists(Path.Combine(diskpath, "data{0}.cab".F(++i)));

			return test;
		}

		void CheckForDisk()
		{
			var path = InstallUtils.GetMountedDisk(IsValidDisk);

			if (path != null)
				Install(path);
			else if ((installData.InstallShieldCABFileIds.Count != 0 || installData.InstallShieldCABFilePackageIds.Count != 0)
				&& (path = InstallUtils.GetMountedDisk(IsTFD)) != null)
					InstallTFD(Platform.ResolvePath(path, "data1.hdr"));
			else
			{
				var text = "Please insert a {0} install CD and click Retry.".F(ModMetadata.AllMods[modId].Title);
				insertDiskContainer.Get<LabelWidget>("INFO2").Text = text;

				insertDiskContainer.IsVisible = () => true;
				installingContainer.IsVisible = () => false;
			}
		}

		void InstallTFD(string source)
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			insertDiskContainer.IsVisible = () => false;
			installingContainer.IsVisible = () => true;
			progressBar.Percentage = 0;

			new Thread(() =>
			{
				using (var cabExtractor = new InstallShieldCABExtractor(modData.ModFiles, source))
				{
					var denom = installData.InstallShieldCABFileIds.Count;
					var extractFiles = installData.ExtractFilesFromCD;

					if (installData.InstallShieldCABFilePackageIds.Count > 0)
						denom += extractFiles.SelectMany(x => x.Value).Count();

					var installPercent = 100 / denom;

					foreach (uint index in installData.InstallShieldCABFileIds)
					{
						var filename = cabExtractor.FileName(index);
						statusLabel.GetText = () => "Extracting {0}".F(filename);
						var dest = Platform.ResolvePath("^", "Content", modId, filename.ToLowerInvariant());
						cabExtractor.ExtractFile(index, dest);
						progressBar.Percentage += installPercent;
					}

					var ArchivesToExtract = installData.InstallShieldCABFilePackageIds.Select(x => x.Split(':'));
					var destDir = Platform.ResolvePath("^", "Content", modId);
					var onError = (Action<string>)(s => { });
					var overwrite = installData.OverwriteFiles;

					var onProgress = (Action<string>)(s => Game.RunAfterTick(() =>
					{
						progressBar.Percentage += installPercent;

						statusLabel.GetText = () => s;
					}));

					foreach (var archive in ArchivesToExtract)
					{
						var filename = cabExtractor.FileName(uint.Parse(archive[0]));
						statusLabel.GetText = () => "Extracting {0}".F(filename);
						var destFile = Platform.ResolvePath("^", "Content", modId, filename.ToLowerInvariant());
						cabExtractor.ExtractFile(uint.Parse(archive[0]), destFile);
						InstallUtils.ExtractFromPackage(modData.ModFiles, source, destFile, extractFiles, destDir, overwrite, installData.OutputFilenameCase, onProgress, onError);
						progressBar.Percentage += installPercent;
					}
				}

				Game.RunAfterTick(() =>
				{
					Ui.CloseWindow();
					afterInstall();
				});
			}) { IsBackground = true }.Start();
		}

		void Install(string source)
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			insertDiskContainer.IsVisible = () => false;
			installingContainer.IsVisible = () => true;
			var dest = Platform.ResolvePath("^", "Content", modId);
			var copyFiles = installData.CopyFilesFromCD;

			var packageToExtract = installData.PackageToExtractFromCD.Split(':');
			var extractPackage = packageToExtract.First();

			var extractFiles = installData.ExtractFilesFromCD;

			var overwrite = installData.OverwriteFiles;
			var installCounter = 0;
			var installTotal = copyFiles.SelectMany(x => x.Value).Count() + extractFiles.SelectMany(x => x.Value).Count();
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
					if (!InstallUtils.CopyFiles(source, copyFiles, dest, overwrite, installData.OutputFilenameCase, onProgress, onError))
					{
						onError("Copying files from CD failed.");
						return;
					}

					if (!string.IsNullOrEmpty(extractPackage))
					{
						if (!InstallUtils.ExtractFromPackage(modData.ModFiles, source, extractPackage, extractFiles, dest,
							overwrite, installData.OutputFilenameCase, onProgress, onError))
						{
							onError("Extracting files from CD failed.");
							return;
						}
					}

					Game.RunAfterTick(() =>
					{
						statusLabel.GetText = () => "Game assets have been extracted.";
						Ui.CloseWindow();
						afterInstall();
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
