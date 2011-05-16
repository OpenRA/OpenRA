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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.Widgets;
using OpenRA.Mods.RA.Widgets.Delegates;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncInstallLogic : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public CncInstallLogic([ObjectCreator.Param] Widget widget,
		                       [ObjectCreator.Param] Dictionary<string,string> installData,
		                       [ObjectCreator.Param] Action continueLoading)
		{
			var panel = widget.GetWidget("INSTALL_PANEL");
			var args = new WidgetArgs()
            {
				{ "continueLoading", () => { Widget.CloseWindow(); continueLoading(); } },
				{ "installData", installData }
			};

			panel.GetWidget<ButtonWidget>("DOWNLOAD_BUTTON").OnClick = () =>
				Widget.OpenWindow("INSTALL_DOWNLOAD_PANEL", args);

			panel.GetWidget<ButtonWidget>("INSTALL_BUTTON").OnClick = () =>
				Widget.OpenWindow("INSTALL_FROMCD_PANEL", args);

			panel.GetWidget<ButtonWidget>("QUIT_BUTTON").OnClick = Game.Exit;

			// TODO:
			panel.GetWidget<ButtonWidget>("MODS_BUTTON").OnClick = () =>
			{
				Widget.OpenWindow("MODS_PANEL", new WidgetArgs()
                {
					{ "onExit", () => {} },
					// Close this panel
					{ "onSwitch", Widget.CloseWindow },
				});
			};
		}
	}

	public class CncInstallFromCDLogic : IWidgetDelegate
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action continueLoading;
		
		[ObjectCreator.UseCtor]
		public CncInstallFromCDLogic([ObjectCreator.Param] Widget widget,
		                       [ObjectCreator.Param] Action continueLoading)
		{
			this.continueLoading = continueLoading;
			panel = widget.GetWidget("INSTALL_FROMCD_PANEL");
			progressBar = panel.GetWidget<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.GetWidget<LabelWidget>("STATUS_LABEL");
			
			var backButton = panel.GetWidget<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = Widget.CloseWindow;
			backButton.IsVisible = () => false;
			
			var retryButton = panel.GetWidget<ButtonWidget>("RETRY_BUTTON");
			retryButton.OnClick = PromptForCD;
			retryButton.IsVisible = () => false;
			
			// TODO: Search obvious places (platform dependent) for CD
			PromptForCD();
		}
		
		void PromptForCD()
		{
			progressBar.SetIndeterminate(true);
			Game.Utilities.PromptFilepathAsync("Select CONQUER.MIX on the C&C CD", path => Game.RunAfterTick(() => Install(path)));
		}
		
		void Install(string path)
		{
			var dest = new string[] { Platform.SupportDir, "Content", "cnc" }.Aggregate(Path.Combine);
			var copyFiles = new string[] { "CONQUER.MIX", "DESERT.MIX",
					"GENERAL.MIX", "SCORES.MIX", "SOUNDS.MIX", "TEMPERAT.MIX", "WINTER.MIX"};
			
			var extractPackage = "INSTALL/SETUP.Z";
			var extractFiles = new string[] { "cclocal.mix", "speech.mix", "tempicnh.mix", "updatec.mix" };

			progressBar.SetIndeterminate(false);
			
			var installCounter = 0;
			var onProgress = (Action<string>)(s =>
			{
				progressBar.Percentage = installCounter*100/(copyFiles.Count() + extractFiles.Count());
				installCounter++;
				
				statusLabel.GetText = () => s;
			});
			
			var onError = (Action<string>)(s =>
			{
				Game.RunAfterTick(() => 
				{
					statusLabel.GetText = () => "Error: "+s;
					panel.GetWidget("RETRY_BUTTON").IsVisible = () => true;
					panel.GetWidget("BACK_BUTTON").IsVisible = () => true;
				});
			});
			
			string source;
			try 
			{
				source = Path.GetDirectoryName(path);
			}
			catch (ArgumentException)
			{
				onError("Invalid path selected");
				return;
			}
			
			var t = new Thread( _ =>
			{
				if (!InstallUtils.CopyFiles(source, copyFiles, dest, onProgress, onError))
				return;
			
				if (!InstallUtils.ExtractFromPackage(source, extractPackage, extractFiles, dest, onProgress, onError))
			    	return;
				
				Game.RunAfterTick(() =>
				{
					Widget.CloseWindow();
					continueLoading();
				});
			}) { IsBackground = true };
			t.Start();
		}
	}

	public class CncDownloadPackagesLogic : IWidgetDelegate
	{
		Widget panel;
		Dictionary<string,string> installData;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action continueLoading;
		
		[ObjectCreator.UseCtor]
		public CncDownloadPackagesLogic([ObjectCreator.Param] Widget widget,
		                                [ObjectCreator.Param] Dictionary<string,string> installData,
		                                [ObjectCreator.Param] Action continueLoading)
		{
			this.installData = installData;
			this.continueLoading = continueLoading;
			
			panel = widget.GetWidget("INSTALL_DOWNLOAD_PANEL");
			progressBar = panel.GetWidget<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.GetWidget<LabelWidget>("STATUS_LABEL");
			
			ShowDownloadDialog();
		}
		
				
		void ShowDownloadDialog()
		{
			statusLabel.GetText = () => "Initializing...";		
			progressBar.SetIndeterminate(true);
			var retryButton = panel.GetWidget<ButtonWidget>("RETRY_BUTTON");
			retryButton.IsVisible = () => false;
			
			var cancelButton = panel.GetWidget<ButtonWidget>("CANCEL_BUTTON");

			// Save the package to a temp file
			var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			var dest = new string[] { Platform.SupportDir, "Content", "cnc" }.Aggregate(Path.Combine);
			
			Action<DownloadProgressChangedEventArgs> onDownloadProgress = i =>
			{
				if (progressBar.Indeterminate)
					progressBar.SetIndeterminate(false);

				progressBar.Percentage = i.ProgressPercentage;			
				statusLabel.GetText = () => "Downloading {1}/{2} kB ({0}%)".F(i.ProgressPercentage, i.BytesReceived / 1024, i.TotalBytesToReceive / 1024);
			};
			
			Action<string> onExtractProgress = s =>
			{
					Game.RunAfterTick(() => statusLabel.GetText = () => s);
			};
			
			Action<string> onError = s =>
			{
				Game.RunAfterTick(() => 
				{
					statusLabel.GetText = () => "Error: "+s;
					retryButton.IsVisible = () => true;
				});
			};
			
			Action<AsyncCompletedEventArgs, bool> onDownloadComplete = (i, cancelled) =>
			{
				if (i.Error != null)
				{
					var message = i.Error.Message;
					var except = i.Error as System.Net.WebException;
					if (except != null)
					{
						if (except.Status == WebExceptionStatus.ProtocolError)
							message = "File not found on remote server";
						else if (except.Status == WebExceptionStatus.NameResolutionFailure ||
						         except.Status == WebExceptionStatus.Timeout ||
						         except.Status == WebExceptionStatus.ConnectFailure)
							message = "Cannot connect to remote server";
					}
					
					onError(message);
					return;
				}
				else if (cancelled)
				{
					onError("Download cancelled");
					return;
				}
				
				// Automatically extract
				statusLabel.GetText = () => "Extracting...";
				progressBar.SetIndeterminate(true);
				if (InstallUtils.ExtractZip(file, dest, onExtractProgress, onError))
				{
					Game.RunAfterTick(() =>
					{
						Widget.CloseWindow();
						continueLoading();
					});
				}
			};
			
			var dl = new Download(installData["PackageURL"], file, onDownloadProgress, onDownloadComplete);
			
			cancelButton.OnClick = () => { dl.Cancel(); Widget.CloseWindow(); };
			retryButton.OnClick = () => { dl.Cancel(); ShowDownloadDialog(); };
		}
	}
}
