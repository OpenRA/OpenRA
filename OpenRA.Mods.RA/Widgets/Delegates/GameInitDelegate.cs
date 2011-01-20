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
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;

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
		}
		
		void ShowInstallMethodDialog()
		{
			window = Widget.OpenWindow("INIT_CHOOSEINSTALL");
			window.GetWidget("DOWNLOAD").OnMouseUp = mi => { ShowDownloadDialog(); return true; };
			window.GetWidget("FROMCD").OnMouseUp = mi =>
			{
				SelectDisk(path => System.Console.WriteLine(path));
				return true;
			};
					
			window.GetWidget("QUIT").OnMouseUp = mi => { Game.Exit(); return true; };
		}
		
		void ShowDownloadDialog()
		{
			window = Widget.OpenWindow("INIT_DOWNLOAD");        
			var status = window.GetWidget<LabelWidget>("STATUS");
			status.GetText = () => "Initializing...";
			
			
			// TODO: Download to a temp location or the support dir
			var file = Info.PackageName;
			
			var progress = window.GetWidget<ProgressBarWidget>("PROGRESS");
			
			window.GetWidget<ButtonWidget>("EXTRACT").OnMouseUp = mi =>
			{ 
				if (ExtractZip(file, Info.PackagePath))
					ContinueLoading(Info);
				return true;
			};
			

			if (File.Exists(file))
			{
				window.GetWidget<ButtonWidget>("EXTRACT").IsVisible = () => true;
				status.GetText = () => "Download Cached";
				progress.Percentage = 100;
			}
			else
			{
				var dl = DownloadUrl(Info.PackageURL, file,
		            (_,i) => {
						status.GetText = () => "Downloading {1}/{2} kB ({0}%)".F(i.ProgressPercentage, i.BytesReceived/1024, i.TotalBytesToReceive/1024);
						progress.Percentage = i.ProgressPercentage;
					},
		            (_,i) => {
						if (i.Error != null)
						{
							ShowDownloadError(i.Error.Message);
						}
						else
						{
							status.GetText = () => "Download Complete";
							window.GetWidget<ButtonWidget>("EXTRACT").IsVisible = () => true;
							window.GetWidget("CANCEL").IsVisible = () => false;
						}
					}
				);
				window.GetWidget("CANCEL").IsVisible = () => true;
				window.GetWidget("RETRY").IsVisible = () => true;
				
				window.GetWidget("CANCEL").OnMouseUp = mi => { dl.CancelAsync(); ShowInstallMethodDialog(); return true; };
				window.GetWidget("RETRY").OnMouseUp = mi => { dl.CancelAsync(); ShowDownloadDialog(); return true; };
			}
		}
		
		void ShowDownloadError(string e)
		{
			window.GetWidget<LabelWidget>("STATUS").GetText = () => e;
			window.GetWidget<ButtonWidget>("RETRY").IsVisible = () => true;
			window.GetWidget<ButtonWidget>("CANCEL").IsVisible = () => true;
		}
				
		// TODO: This needs to live on a different process if we want to run it as root
		public bool ExtractZip(string zipFile, string path)
		{
			if (!File.Exists(zipFile)) { ShowDownloadError("Download Corrupted"); return false; }
			List<string> extracted = new List<string>();
			try
			{
				ZipEntry entry;
				var z = new ZipInputStream(File.OpenRead(zipFile));
				while ((entry = z.GetNextEntry()) != null)
				{
					if (!entry.IsFile) continue;			
					if (!Directory.Exists(Path.Combine(path, Path.GetDirectoryName(entry.Name))))
						Directory.CreateDirectory(Path.Combine(path, Path.GetDirectoryName(entry.Name)));
					
					window.GetWidget<LabelWidget>("STATUS").GetText = () => "Status: Extracting {0}".F(entry.Name);
					
					var destPath = path + Path.DirectorySeparatorChar + entry.Name;
					Console.WriteLine("Extracting to {0}",destPath);
					extracted.Add(path);
					using (var f = File.Create(destPath))
					{
						int bufSize = 2048;
						byte[] buf = new byte[bufSize];
						while ((bufSize = z.Read(buf, 0, buf.Length)) > 0)
							f.Write(buf, 0, bufSize);
					}
				}
				z.Close();
			}
			catch (SharpZipBaseException)
			{
				foreach(var f in extracted)
					File.Delete(f);
				
				ShowDownloadError("Download Corrupted");
				return false;
			}
			return true;
		}
		
		void SelectDisk(Action<string> withPath)
		{
			Process p = new Process();
			p.StartInfo.FileName = "OpenRA.Launcher.Mac/build/Release/OpenRA.app/Contents/MacOS/OpenRA";
			p.StartInfo.Arguments = "--filepicker --title \"Select CD\" --message \"Select the {0} CD\" --require-directory --button-text \"Select\"".F(Info.GameTitle);
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.CreateNoWindow = true;
			p.EnableRaisingEvents = true;
			p.Exited += (_,e) =>
			{
				withPath(p.StandardOutput.ReadToEnd());
			};
			p.Start();
		}
		
		void ContinueLoading(Widget widget)
		{
			Game.LoadShellMap();
			Widget.RootWidget.Children.Remove(widget);
			Widget.OpenWindow("MAINMENU_BG");
		}
				
		public static WebClient DownloadUrl(string url, string path, DownloadProgressChangedEventHandler onProgress, AsyncCompletedEventHandler onComplete)
		{
			WebClient wc = new WebClient();
			wc.Proxy = null;

			wc.DownloadProgressChanged += onProgress;
			wc.DownloadFileCompleted += onComplete;
			wc.DownloadFileCompleted += (_,a) => {Game.OnQuit -= () => wc.CancelAsync();};
			wc.DownloadFileAsync(new Uri(url), path);
			Game.OnQuit += () => wc.CancelAsync();
			return wc;
		}
	}
}
