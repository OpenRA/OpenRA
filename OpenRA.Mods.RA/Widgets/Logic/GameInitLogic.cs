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
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class GameInitLogic : ILogicWithInit
	{
		GameInitInfoWidget Info;
		
		[ObjectCreator.UseCtor]
		public GameInitLogic([ObjectCreator.Param] Widget widget)
		{
			Info = (widget as GameInitInfoWidget);
		}

		void ILogicWithInit.Init()
		{
			Game.ConnectionStateChanged += orderManager =>
				{
					Widget.CloseWindow();
					switch (orderManager.Connection.ConnectionState)
					{
						case ConnectionState.PreConnecting:
							Widget.LoadWidget("MAINMENU_BG", Widget.RootWidget, new WidgetArgs());
							break;
						case ConnectionState.Connecting:
							Widget.OpenWindow("CONNECTING_BG",
								new WidgetArgs() { { "host", orderManager.Host }, { "port", orderManager.Port } });
							break;
						case ConnectionState.NotConnected:
							Widget.OpenWindow("CONNECTION_FAILED_BG",
								new WidgetArgs() { { "orderManager", orderManager } });
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

			TestAndContinue();
		}
		
		void TestAndContinue()
		{
			if (FileSystem.Exists(Info.TestFile))
			{
				Game.LoadShellMap();
				if (Info.InstallMode != "cnc")
				{
					Widget.ResetAll();
					Widget.OpenWindow("MAINMENU_BG");
				}
			}
            else
            {
                MainMenuButtonsLogic.DisplayModSelector();
                ShowInstallMethodDialog();
            }
		}
		
		void ShowInstallMethodDialog()
		{
			var window = Widget.OpenWindow("INIT_CHOOSEINSTALL");
			window.GetWidget<ButtonWidget>("DOWNLOAD").OnClick = () => ShowDownloadDialog();
			window.GetWidget<ButtonWidget>("FROMCD").OnClick = () => PromptForCD();
					
			window.GetWidget<ButtonWidget>("QUIT").OnClick = () => Game.Exit();
		}
		
		void PromptForCD()
		{
			Game.Utilities.PromptFilepathAsync("Select MAIN.MIX on the CD", path =>
			{
				if (!string.IsNullOrEmpty(path))
					Game.RunAfterTick(() => InstallFromCD(Path.GetDirectoryName(path)));
			});
		}
		
		void InstallFromCD(string path)
		{
			var window = Widget.OpenWindow("INIT_COPY");
			var progress = window.GetWidget<ProgressBarWidget>("PROGRESS");
			progress.Indeterminate = true;

			// TODO: Handle cancelling copy
			window.GetWidget<ButtonWidget>("CANCEL").IsVisible = () => false;
			window.GetWidget<ButtonWidget>("CANCEL").OnClick = () => ShowInstallMethodDialog();
			window.GetWidget<ButtonWidget>("RETRY").OnClick = () => PromptForCD();
			
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
			var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Action<DownloadProgressChangedEventArgs> onDownloadChange = i =>
			{
				status.GetText = () => "Downloading {1}/{2} kB ({0}%)".F(i.ProgressPercentage, i.BytesReceived / 1024, i.TotalBytesToReceive / 1024);
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
			window.GetWidget<ButtonWidget>("CANCEL").OnClick = () => { dl.Cancel(); ShowInstallMethodDialog(); };
			window.GetWidget<ButtonWidget>("RETRY").OnClick = () => { dl.Cancel(); ShowDownloadDialog(); };
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
			var status = window.GetWidget<LabelWidget>("STATUS");
			return InstallUtils.ExtractZip( zipFile, dest,
			                        s => status.GetText = () => s,
			                        e => ShowError(window, e));
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
			return InstallUtils.CopyFiles(srcPath, files, destPath,
			                              s => status.GetText = () => s,
			                              e => ShowError(window, e));
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
}
