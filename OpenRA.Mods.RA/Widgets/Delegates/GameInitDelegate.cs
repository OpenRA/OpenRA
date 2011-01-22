#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Widgets;
using System.Diagnostics;
using System;
using System.Net;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Drawing;

namespace OpenRA.Mods.RA.Widgets.Delegates
{
	public class GameInitDelegate : IWidgetDelegate
	{
		GameInitInfoWidget Info;
		Widget window;
		
		[ObjectCreator.UseCtor]
		public GameInitDelegate([ObjectCreator.Param] Widget widget)
		{
			Info = (widget as GameInitInfoWidget);

			Game.ConnectionStateChanged += orderManager =>
			{
				Widget.CloseWindow();
				switch( orderManager.Connection.ConnectionState )
				{
					case ConnectionState.PreConnecting:
						Widget.OpenWindow("MAINMENU_BG");
						break;
					case ConnectionState.Connecting:
						Widget.OpenWindow( "CONNECTING_BG",
							new Dictionary<string, object> { { "host", orderManager.Host }, { "port", orderManager.Port } } );
						break;
					case ConnectionState.NotConnected:
						Widget.OpenWindow( "CONNECTION_FAILED_BG",
							new Dictionary<string, object> { { "orderManager", orderManager } } );
						break;
					case ConnectionState.Connected:
						var lobby = Game.OpenWindow(orderManager.world, "SERVER_LOBBY");
						lobby.GetWidget<ChatDisplayWidget>("CHAT_DISPLAY").ClearChat();
						lobby.GetWidget("CHANGEMAP_BUTTON").Visible = true;
						lobby.GetWidget("LOCKTEAMS_CHECKBOX").Visible = true;
						lobby.GetWidget("DISCONNECT_BUTTON").Visible = true;
						break;
				}
			};
			
			if (FileSystem.Exists(Info.TestFile))
				ContinueLoading(widget);
			else
			{
				ShowInstallMethodDialog();
			}
			
			var selector = Game.modData.WidgetLoader.LoadWidget( new Dictionary<string,object>(), Widget.RootWidget, "QUICKMODSWITCHER" );
			var switcher = selector.GetWidget<ButtonWidget>("SWITCHER");
			switcher.OnMouseDown = _ => ShowModsDropDown(switcher);
			switcher.GetText = ActiveModTitle;
			selector.GetWidget<LabelWidget>("VERSION").GetText = ActiveModVersion;	
		}
		
		string ActiveModTitle()
		{
			var mod = Game.modData.Manifest.Mods[0];
			return Mod.AllMods[mod].Title;
		}
		
		string ActiveModVersion()
		{
			var mod = Game.modData.Manifest.Mods[0];
			return Mod.AllMods[mod].Version;
		}
		
		bool ShowModsDropDown(ButtonWidget selector)
		{
			var dropDownOptions = new List<Pair<string, Action>>();
			
			foreach (var kv in Mod.AllMods)
			{
				var modList = new List<string>() { kv.Key };
				var m = kv.Key;
				while (!string.IsNullOrEmpty(Mod.AllMods[m].Requires))
				{
					m = Mod.AllMods[m].Requires;
					modList.Add(m);
				}
					
				dropDownOptions.Add(new Pair<string, Action>( kv.Value.Title,
					() => Game.RunAfterTick(() => Game.InitializeWithMods( modList.ToArray() ) )));
			}
				                    
			DropDownButtonWidget.ShowDropDown( selector,
				dropDownOptions,
				(ac, w) => new LabelWidget
				{
					Bounds = new Rectangle(0, 0, w, 24),
					Text = "  {0}".F(ac.First),
					OnMouseUp = mi => { ac.Second(); return true; },
				});
			return true;
		}
		
		void ShowInstallMethodDialog()
		{
			window = Widget.OpenWindow("INIT_CHOOSEINSTALL");
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
			window = Widget.OpenWindow("INIT_COPY");
			var status = window.GetWidget<LabelWidget>("STATUS");
			var progress = window.GetWidget<ProgressBarWidget>("PROGRESS");
			progress.Indeterminate = true;

			// TODO: Handle cancelling copy
			window.GetWidget<ButtonWidget>("CANCEL").IsVisible = () => false;
			window.GetWidget("CANCEL").OnMouseUp = mi => { ShowInstallMethodDialog(); return true; };
			window.GetWidget("RETRY").OnMouseUp = mi => PromptForCD();

			status.GetText = () => "Copying...";
			var error = false;
			Action<string> parseOutput = s => 
		    {
		    	if (s.Substring(0,5) == "Error")
				{
					error = true;
					ShowDownloadError(s);
				}
				if (s.Substring(0,6) == "Status")
					window.GetWidget<LabelWidget>("STATUS").GetText = () => s.Substring(7).Trim();
			};
			
			Action onComplete = () =>
			{
				if (!error)
					Game.RunAfterTick(() => ContinueLoading(Info));
			};
			
			if (Info.InstallMode == "ra")
				Game.Utilities.InstallRAFilesAsync(path, parseOutput, onComplete);
			else 
				ShowDownloadError("Installing from CD not supported");
		}

		void ShowDownloadDialog()
		{
			window = Widget.OpenWindow("INIT_DOWNLOAD");
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
					ShowDownloadError(i.Error.Message);
				else if (!cancelled)
				{
					// Automatically extract
					status.GetText = () => "Extracting...";
					progress.Indeterminate = true;
					var error = false;
					Action<string> parseOutput = s => 
				    {
				    	if (s.Substring(0,5) == "Error")
						{
							error = true;
							ShowDownloadError(s);
						}
						if (s.Substring(0,6) == "Status")
							window.GetWidget<LabelWidget>("STATUS").GetText = () => s.Substring(7).Trim();
					};
					
					Action onComplete = () =>
					{
						if (!error)
							Game.RunAfterTick(() => ContinueLoading(Info));
					};
					
					Game.RunAfterTick(() => Game.Utilities.ExtractZipAsync(file, Info.PackagePath, parseOutput, onComplete));
				}
			};
			
			var dl = new Download(Info.PackageURL, file, onDownloadChange, onDownloadComplete);
			window.GetWidget("CANCEL").OnMouseUp = mi => { dl.Cancel(); ShowInstallMethodDialog(); return true; };
			window.GetWidget("RETRY").OnMouseUp = mi => { dl.Cancel(); ShowDownloadDialog(); return true; };
		}
		
		void ShowDownloadError(string e)
		{
			window.GetWidget<LabelWidget>("STATUS").GetText = () => e;
			window.GetWidget<ButtonWidget>("RETRY").IsVisible = () => true;
			window.GetWidget<ButtonWidget>("CANCEL").IsVisible = () => true;
		}
				
		void ContinueLoading(Widget widget)
		{
			Game.LoadShellMap();
			Widget.RootWidget.Children.Remove(widget);
			Widget.OpenWindow("MAINMENU_BG");
		}
		
		// General support methods
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
}
