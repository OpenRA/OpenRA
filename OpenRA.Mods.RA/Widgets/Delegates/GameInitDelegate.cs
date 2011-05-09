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
using System.Net;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Widgets;
using System.Threading;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class GameInitDelegate : IWidgetDelegateEx
	{
		GameInitInfoWidget Info;
		
		[ObjectCreator.UseCtor]
		public GameInitDelegate([ObjectCreator.Param] Widget widget)
		{
			Info = (widget as GameInitInfoWidget);
		}

        public void Init()
        {
			if (Info.InstallMode != "cnc")
			{		
				Game.ConnectionStateChanged += orderManager =>
	            {
	                Widget.CloseWindow();
	                switch (orderManager.Connection.ConnectionState)
	                {
	                    case ConnectionState.PreConnecting:
	                        Widget.LoadWidget("MAINMENU_BG", new Dictionary<string, object>());
	                        break;
	                    case ConnectionState.Connecting:
	                        Widget.OpenWindow("CONNECTING_BG",
	                            new Dictionary<string, object> { { "host", orderManager.Host }, { "port", orderManager.Port } });
	                        break;
	                    case ConnectionState.NotConnected:
	                        Widget.OpenWindow("CONNECTION_FAILED_BG",
	                            new Dictionary<string, object> { { "orderManager", orderManager } });
	                        break;
	                    case ConnectionState.Connected:
	                        var lobby = Game.OpenWindow(orderManager.world, "SERVER_LOBBY");
	                        lobby.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").ClearChat();
	                        lobby.GetWidget("CHANGEMAP_BUTTON").Visible = true;
	                        lobby.GetWidget("LOCKTEAMS_CHECKBOX").Visible = true;
	                        lobby.GetWidget("ALLOWCHEATS_CHECKBOX").Visible = true;
	                        lobby.GetWidget("DISCONNECT_BUTTON").Visible = true;
	                        break;
	                }
	            };
			}
			TestAndContinue();
        }
		
		void TestAndContinue()
		{
			if (FileSystem.Exists(Info.TestFile))
			{
				Game.LoadShellMap();
				if (Info.InstallMode != "cnc")
				{
					Widget.RootWidget.RemoveChildren();
					Widget.OpenWindow("MAINMENU_BG");
				}
			}
            else
            {
                MainMenuButtonsDelegate.DisplayModSelector();
                ShowInstallMethodDialog();
            }
		}
		
		void ShowInstallMethodDialog()
		{
			var window = Widget.OpenWindow("INIT_CHOOSEINSTALL");
			window.GetWidget("DOWNLOAD").OnMouseUp = mi => { ShowDownloadDialog(); return true; };
			window.GetWidget("FROMCD").OnMouseUp = mi => PromptForCD();
					
			window.GetWidget("QUIT").OnMouseUp = mi => { Game.Exit(); return true; };
		}
		
		bool PromptForCD()
		{
			Game.Utilities.PromptFilepathAsync("Select MAIN.MIX on the CD", path =>
			{
				if (!string.IsNullOrEmpty(path))
					Game.RunAfterTick(() => InstallFromCD(Path.GetDirectoryName(path)));
			});
			return true;
		}
		
		void InstallFromCD(string path)
		{
			var window = Widget.OpenWindow("INIT_COPY");
			var progress = window.GetWidget<ProgressBarWidget>("PROGRESS");
			progress.Indeterminate = true;

			// TODO: Handle cancelling copy
			window.GetWidget<ButtonWidget>("CANCEL").IsVisible = () => false;
			window.GetWidget("CANCEL").OnMouseUp = mi => { ShowInstallMethodDialog(); return true; };
			window.GetWidget("RETRY").OnMouseUp = mi => PromptForCD();
			
			var t = new Thread( _ =>
			{
				switch (Info.InstallMode)
				{
					case "ra":
						if (InstallRAPackages(window, path, Info.ResolvedPackagePath))
				    		Game.RunAfterTick(TestAndContinue);
					break;
					case "cnc":
						if (InstallCncPackages(window, path, Info.ResolvedPackagePath))
				    		Game.RunAfterTick(TestAndContinue);
					break;
					default:
						ShowError(window, "Installing from CD not supported");
					break;
				}
			}) { IsBackground = true };
			t.Start();
		}

		void ShowDownloadDialog()
		{
			var window = Widget.OpenWindow("INIT_DOWNLOAD");
			var status = window.GetWidget<LabelWidget>("STATUS");
			status.GetText = () => "Initializing...";
			var progress = window.GetWidget<ProgressBarWidget>("PROGRESS");

			// Save the package to a temp file
			var file = Path.GetTempPath() + Path.DirectorySeparatorChar + Path.GetRandomFileName();					
			Action<DownloadProgressChangedEventArgs> onDownloadChange = i =>
			{
				status.GetText = () => "Downloading {1}/{2} kB ({0}%)".F(i.ProgressPercentage, i.BytesReceived/1024, i.TotalBytesToReceive/1024);
				progress.Percentage = i.ProgressPercentage;
			};
			
			Action<AsyncCompletedEventArgs, bool> onDownloadComplete = (i, cancelled) =>
			{
				if (i.Error != null)
					ShowError(window, i.Error.Message);
				else if (!cancelled)
				{
					// Automatically extract
					status.GetText = () => "Extracting...";
					progress.Indeterminate = true;

					if (ExtractZip(window, file, Info.ResolvedPackagePath))
						Game.RunAfterTick(TestAndContinue);
				}
			};
			
			var dl = new Download(Info.PackageURL, file, onDownloadChange, onDownloadComplete);
			window.GetWidget("CANCEL").OnMouseUp = mi => { dl.Cancel(); ShowInstallMethodDialog(); return true; };
			window.GetWidget("RETRY").OnMouseUp = mi => { dl.Cancel(); ShowDownloadDialog(); return true; };
		}
		
		void ShowError(Widget window, string e)
		{
			if (window.GetWidget<LabelWidget>("STATUS") != null)	/* ugh */
			{
				window.GetWidget<LabelWidget>("STATUS").GetText = () => e;
				window.GetWidget<ButtonWidget>("RETRY").IsVisible = () => true;
				window.GetWidget<ButtonWidget>("CANCEL").IsVisible = () => true;
			}
		}
		
		bool ExtractZip(Widget window, string zipFile, string dest)
		{
			if (!File.Exists(zipFile))
			{
				ShowError(window, "Invalid path: "+zipFile);
				return false;
			}
			
			var status = window.GetWidget<LabelWidget>("STATUS");
			List<string> extracted = new List<string>();
			try
			{
				new ZipInputStream(File.OpenRead(zipFile)).ExtractZip(dest, extracted, s => status.GetText = () => "Extracting "+s);
			}
			catch (SharpZipBaseException)
			{
				foreach(var f in extracted)
					File.Delete(f);
				ShowError(window, "Archive corrupt");
				return false;
			}
			status.GetText = () => "Extraction complete";
			return true;
		}

		bool ExtractFromPackage(Widget window, string srcPath, string package, string[] files, string destPath)
		{
			var status = window.GetWidget<LabelWidget>("STATUS");
			
			return InstallUtils.ExtractFromPackage(srcPath, package, files, destPath,
			                                       s => status.GetText = () => s,
			                                       e => ShowError(window, e));
		}
		
		bool CopyFiles(Widget window, string srcPath, string[] files, string destPath)
		{
			var status = window.GetWidget<LabelWidget>("STATUS");

			foreach (var file in files)
			{
				var fromPath = Path.Combine(srcPath, file);
				if (!File.Exists(fromPath))
				{
					ShowError(window, "Cannot find "+file);
					return false;
				}
				status.GetText = () => "Extracting "+file.ToLowerInvariant();
				File.Copy(fromPath,	Path.Combine(destPath, Path.GetFileName(file).ToLowerInvariant()), true);
			}
			return true;
		}
		
		bool InstallRAPackages(Widget window, string source, string dest)
		{
			if (!CopyFiles(window, Path.Combine(source, "INSTALL"), new string[] {"REDALERT.MIX"}, dest))
				return false;
			return ExtractFromPackage(window, source, "MAIN.MIX",
				new string[] { "conquer.mix", "russian.mix", "allies.mix", "sounds.mix",
					"scores.mix", "snow.mix", "interior.mix", "temperat.mix" }, dest);
		}
		
		bool InstallCncPackages(Widget window, string source, string dest)
		{
			if (!CopyFiles(window, source,
		    		new string[] { "CONQUER.MIX", "DESERT.MIX", "GENERAL.MIX", "SCORES.MIX",
						"SOUNDS.MIX", "TEMPERAT.MIX", "WINTER.MIX"},
					dest))
				return false;
			return ExtractFromPackage(window, source, "INSTALL/SETUP.Z",
				new string[] { "cclocal.mix", "speech.mix", "tempicnh.mix", "updatec.mix" }, dest);
		}
    }
	
	public class Download
	{
		WebClient wc;
		bool cancelled;
		
		public Download(string url, string path, Action<DownloadProgressChangedEventArgs> onProgress, Action<AsyncCompletedEventArgs, bool> onComplete)
		{
			wc = new WebClient();
			wc.Proxy = null;

			wc.DownloadProgressChanged += (_,a) => onProgress(a);
			wc.DownloadFileCompleted += (_,a) => onComplete(a, cancelled);
			
			Game.OnQuit += () => Cancel();
			wc.DownloadFileCompleted += (_,a) => {Game.OnQuit -= () => Cancel();};
			
			wc.DownloadFileAsync(new Uri(url), path); 
		}
		
		public void Cancel()
		{
			Game.OnQuit -= () => Cancel();
			wc.CancelAsync();
			cancelled = true;
		}
	}
}
