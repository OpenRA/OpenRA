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
using System.IO;
using System.Linq;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ReplayBrowserLogic
	{
		Widget panel;
		MapPreview selectedMap = MapCache.UnknownMap;
		string selectedFilename;
		string selectedDuration;
		string selectedPlayers;
		bool selectedValid;

		[ObjectCreator.UseCtor]
		public ReplayBrowserLogic(Widget widget, Action onExit, Action onStart)
		{
			panel = widget;

			panel.Get<ButtonWidget>("CANCEL_BUTTON").OnClick = () => { Ui.CloseWindow(); onExit(); };

			var rl = panel.Get<ScrollPanelWidget>("REPLAY_LIST");
			var template = panel.Get<ScrollItemWidget>("REPLAY_TEMPLATE");

			var mod = Game.modData.Manifest.Mod;
			var dir = new[] { Platform.SupportDir, "Replays", mod.Id, mod.Version }.Aggregate(Path.Combine);

			rl.RemoveChildren();
			if (Directory.Exists(dir))
			{
				var files = Directory.GetFiles(dir, "*.rep").Reverse();
				foreach (var replayFile in files)
					AddReplay(rl, replayFile, template);

				SelectReplay(files.FirstOrDefault());
			}

			var watch = panel.Get<ButtonWidget>("WATCH_BUTTON");
			watch.IsDisabled = () => !selectedValid || selectedMap.Status != MapStatus.Available;
			watch.OnClick = () => { WatchReplay(); onStart(); };

			panel.Get("REPLAY_INFO").IsVisible = () => selectedFilename != null;;
			panel.Get<LabelWidget>("DURATION").GetText = () => selectedDuration;
			panel.Get<MapPreviewWidget>("MAP_PREVIEW").Preview = () => selectedMap;
			panel.Get<LabelWidget>("MAP_TITLE").GetText = () => selectedMap.Title;
			panel.Get<LabelWidget>("PLAYERS").GetText = () => selectedPlayers;
		}

		void SelectReplay(string filename)
		{
			if (filename == null)
				return;

			try
			{
				using (var conn = new ReplayConnection(filename))
				{
					selectedFilename = filename;
					selectedMap = Game.modData.MapCache[conn.LobbyInfo.GlobalSettings.Map];
					selectedDuration = WidgetUtils.FormatTime(conn.TickCount * Game.NetTickScale);
					selectedPlayers = conn.LobbyInfo.Slots
						.Count(s => conn.LobbyInfo.ClientInSlot(s.Key) != null)
						.ToString();
					selectedValid = conn.TickCount > 0;
				}
			}
			catch (Exception e)
			{
				Log.Write("debug", "Exception while parsing replay: {0}", e);
				selectedFilename = null;
				selectedValid = false;
				selectedMap = MapCache.UnknownMap;
			}
		}

		void WatchReplay()
		{
			if (selectedFilename != null)
			{
				Game.JoinReplay(selectedFilename);
				Ui.CloseWindow();
			}
		}

		void AddReplay(ScrollPanelWidget list, string filename, ScrollItemWidget template)
		{
			var item = ScrollItemWidget.Setup(template,
				() => selectedFilename == filename,
				() => SelectReplay(filename),
				() => WatchReplay());
			var f = Path.GetFileName(filename);
			item.Get<LabelWidget>("TITLE").GetText = () => f;
			list.AddChild(item);
		}
	}
}
