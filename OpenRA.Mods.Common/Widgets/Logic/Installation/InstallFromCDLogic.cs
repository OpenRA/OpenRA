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
using OpenRA.FileSystem;
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

		bool IsTheFirstDecadeDisk(string diskpath)
		{
			return File.Exists(Path.Combine(diskpath, "data1.hdr"));
		}

		void CheckForDisk()
		{
			var path = InstallUtils.GetMountedDisk(IsValidDisk);

			if (path != null)
				Install(path);
			else if ((installData.InstallShieldPackageName != null && installData.ExtractFromInstallShieldPackage.Length != 0)
				&& (path = InstallUtils.GetMountedDisk(IsTheFirstDecadeDisk)) != null)
				InstallTFD(Platform.ResolvePath(path, "data1.hdr"));
			else
			{
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
				using (var cabExtractor = new InstallShieldCABExtractor(source))
				{
					var dest = "";
					var files = installData.ExtractFromInstallShieldPackage.Select(x => x.Split(':'));
					var extractFiles = installData.ExtractFilesFromCD;
					var installPercent = 100 / files.Count();
					var fileindex = (uint)0;
					var filename = "";
					var onError = (Action<string>)(s => { });

					foreach (var file in files)
					{
						fileindex = cabExtractor.GetIndexByFilename(installData.InstallShieldPackageName.ToLowerInvariant(), file[0].ToLowerInvariant());

						if (fileindex == uint.MinValue)
						{
							onError("File not found: {0}".F(file[0].ToLowerInvariant()));
							return;
						}

						filename = cabExtractor.FileName(fileindex);
						statusLabel.GetText = () => "Extracting: {0}".F(filename.ToLowerInvariant());

						dest = Platform.ResolvePath("^", "Content", Game.ModData.Manifest.Mod.Id, filename.ToLowerInvariant());
						cabExtractor.ExtractFile(fileindex, dest.ToLowerInvariant());
						progressBar.Percentage += installPercent;
					}

					progressBar.Indeterminate = true;

					var archivesToExtract = installData.ExtractFromInstallShieldPackage.Select(x => x.Split(':'));
					var destDir = Platform.ResolvePath("^", "Content", Game.ModData.Manifest.Mod.Id);

					var overwrite = installData.OverwriteFiles;

					var onProgress = (Action<string>)(s => Game.RunAfterTick(() =>
					{
						statusLabel.GetText = () => s;
					}));

					foreach (var archive in archivesToExtract)
					{
						fileindex = cabExtractor.GetIndexByFilename(installData.InstallShieldPackageName.ToLowerInvariant(), archive[0].ToLowerInvariant());

						if (fileindex == uint.MinValue)
						{
							onError("File not found: {0}".F(archive[0].ToLowerInvariant()));
							return;
						}

						filename = cabExtractor.FileName(fileindex);

						var destFile = Platform.ResolvePath("^", "Content", Game.ModData.Manifest.Mod.Id, filename.ToLowerInvariant());
						cabExtractor.ExtractFile(fileindex, destFile.ToLowerInvariant());
						var annotation = archive.Length > 1 ? archive[1] : null;

						try
						{
							InstallUtils.ExtractFromPackage(source, destFile, annotation, extractFiles, destDir, overwrite, onProgress, onError);
							statusLabel.GetText = () => "Extracting: {0}".F(filename.ToLowerInvariant());
						}
						catch (Exception e)
						{
							if (!File.Exists(destFile))
							{
								onError("Installation failed.\n{0}".F(e.Message));
								Log.Write("debug", e.ToString());
								return;
							}
						}
					}
				}

				continueLoading();
			}) { IsBackground = true }.Start();
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
					if (!InstallUtils.CopyFiles(source, copyFiles, dest, overwrite, onProgress, onError))
					{
						onError("Copying files from CD failed.");
						return;
					}

					if (!string.IsNullOrEmpty(extractPackage))
					{
						if (!InstallUtils.ExtractFromPackage(source, extractPackage, annotation, extractFiles, dest, overwrite, onProgress, onError))
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
