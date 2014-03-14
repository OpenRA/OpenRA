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
			watch.IsDisabled = () => currentReplay == null || currentMap == null || currentReplay.Duration == 0;
			watch.OnClick = () => { WatchReplay(); onStart(); };

			panel.Get("REPLAY_INFO").IsVisible = () => currentReplay != null;
		}

		Replay currentReplay;
		MapPreview currentMap = MapCache.UnknownMap;

		void SelectReplay(string filename)
		{
			if (filename == null)
				return;

			try
			{
				currentReplay = new Replay(filename);
				currentMap = Game.modData.MapCache[currentReplay.LobbyInfo.GlobalSettings.Map];

				panel.Get<LabelWidget>("DURATION").GetText =
					() => WidgetUtils.FormatTime(currentReplay.Duration);
				panel.Get<MapPreviewWidget>("MAP_PREVIEW").Preview = () => currentMap;
				panel.Get<LabelWidget>("MAP_TITLE").GetText =
					() => currentMap != null ? currentMap.Title : "(Unknown Map)";

				var players = currentReplay.LobbyInfo.Slots
					.Count(s => currentReplay.LobbyInfo.ClientInSlot(s.Key) != null);
				panel.Get<LabelWidget>("PLAYERS").GetText = () => players.ToString();
			}
			catch (Exception e)
			{
				Log.Write("debug", "Exception while parsing replay: {0}", e);
				currentReplay = null;
				currentMap = MapCache.UnknownMap;
			}
		}

		void WatchReplay()
		{
			if (currentReplay != null)
			{
				Game.JoinReplay(currentReplay.Filename);
				Ui.CloseWindow();
			}
		}

		void AddReplay(ScrollPanelWidget list, string filename, ScrollItemWidget template)
		{
			var item = ScrollItemWidget.Setup(template,
				() => currentReplay != null && currentReplay.Filename == filename,
				() => SelectReplay(filename),
				() => WatchReplay());
			var f = Path.GetFileName(filename);
			item.Get<LabelWidget>("TITLE").GetText = () => f;
			list.AddChild(item);
		}
	}
}
