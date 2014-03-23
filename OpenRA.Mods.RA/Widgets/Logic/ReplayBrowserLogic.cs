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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ReplayBrowserLogic
	{
		Widget panel;
		ScrollPanelWidget playerList;
		ScrollItemWidget playerTemplate, playerHeader;

		MapPreview selectedMap = MapCache.UnknownMap;
		Dictionary<CPos, Session.Client> selectedSpawns;
		string selectedFilename;
		string selectedDuration;
		bool selectedValid;

		[ObjectCreator.UseCtor]
		public ReplayBrowserLogic(Widget widget, Action onExit, Action onStart)
		{
			panel = widget;

			playerList = panel.Get<ScrollPanelWidget>("PLAYER_LIST");
			playerHeader = playerList.Get<ScrollItemWidget>("HEADER");
			playerTemplate = playerList.Get<ScrollItemWidget>("TEMPLATE");
			playerList.RemoveChildren();

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

			panel.Get("REPLAY_INFO").IsVisible = () => selectedFilename != null;

			var preview = panel.Get<MapPreviewWidget>("MAP_PREVIEW");
			preview.SpawnClients = () => selectedSpawns;
			preview.Preview = () => selectedMap;

			var title = panel.GetOrNull<LabelWidget>("MAP_TITLE");
			if (title != null)
				title.GetText = () => selectedMap.Title;

			var type = panel.GetOrNull<LabelWidget>("MAP_TYPE");
			if (type != null)
				type.GetText = () => selectedMap.Type;

			panel.Get<LabelWidget>("DURATION").GetText = () => selectedDuration;
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
					selectedSpawns = LobbyUtils.GetSpawnClients(conn.LobbyInfo, selectedMap);
					selectedDuration = WidgetUtils.FormatTime(conn.TickCount * Game.NetTickScale);
					selectedValid = conn.TickCount > 0;

					var clients = conn.LobbyInfo.Clients.Where(c => c.Slot != null)
						.GroupBy(c => c.Team)
						.OrderBy(g => g.Key);

					var teams = new Dictionary<string, IEnumerable<Session.Client>>();
					var noTeams = clients.Count() == 1;
					foreach (var c in clients)
					{
						var label = noTeams ? "Players" : c.Key == 0 ? "No Team" : "Team {0}".F(c.Key);
						teams.Add(label, c);
					}

					playerList.RemoveChildren();

					foreach (var kv in teams)
					{
						var group = kv.Key;
						if (group.Length > 0)
						{
							var header = ScrollItemWidget.Setup(playerHeader, () => true, () => {});
							header.Get<LabelWidget>("LABEL").GetText = () => group;
							playerList.AddChild(header);
						}

						foreach (var option in kv.Value)
						{
							var o = option;

							var color = o.Color.RGB;

							var item = ScrollItemWidget.Setup(playerTemplate, () => false, () => { });

							var label = item.Get<LabelWidget>("LABEL");
							label.GetText = () => o.Name;
							label.GetColor = () => color;

							var flag = item.Get<ImageWidget>("FLAG");
							flag.GetImageCollection = () => "flags";
							flag.GetImageName = () => o.Country;

							playerList.AddChild(item);
						}
					}
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
