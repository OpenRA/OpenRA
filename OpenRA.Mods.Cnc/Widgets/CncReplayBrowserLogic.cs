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
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Widgets;
using OpenRA.Mods.RA.Widgets.Delegates;
using OpenRA.Graphics;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncReplayBrowserLogic : IWidgetDelegate
	{
		Widget panel; 

		[ObjectCreator.UseCtor]
		public CncReplayBrowserLogic([ObjectCreator.Param] Widget widget,
		                             [ObjectCreator.Param] Action onExit,
		                             [ObjectCreator.Param] Action onStart)
		{
			panel = widget.GetWidget("REPLAYBROWSER_PANEL");

			panel.GetWidget<CncMenuButtonWidget>("CANCEL_BUTTON").OnClick = onExit;

			var rl = panel.GetWidget<ScrollPanelWidget>("REPLAY_LIST");
			var replayDir = Path.Combine(Platform.SupportDir, "Replays");

			var template = panel.GetWidget("REPLAY_TEMPLATE");

			rl.RemoveChildren();
			if (Directory.Exists(replayDir))
			{
				var files = Directory.GetFiles(replayDir, "*.rep").Reverse();
				foreach (var replayFile in files)
					AddReplay(rl, replayFile, template);
			
				SelectReplay(files.FirstOrDefault());
			}

			var watch = panel.GetWidget<CncMenuButtonWidget>("WATCH_BUTTON");
			watch.IsDisabled = () => currentSummary == null || currentMap == null || currentSummary.Duration == 0;
			watch.OnClick = () =>
			{
				if (currentSummary != null)
				{
					Game.JoinReplay(currentSummary.Filename);
					onStart();
				}
			};

			panel.GetWidget("REPLAY_INFO").IsVisible = () => currentSummary != null;
		}
		
		ReplaySummary currentSummary;
		Map currentMap;
		void SelectReplay(string filename)
		{
			if (filename == null)
				return;

			try
			{
				currentSummary = new ReplaySummary(filename);
				currentMap = currentSummary.Map();

				panel.GetWidget<LabelWidget>("DURATION").GetText =
					() => WidgetUtils.FormatTime(currentSummary.Duration * 3	/* todo: 3:1 ratio isnt always true. */);
				panel.GetWidget<MapPreviewWidget>("MAP_PREVIEW").Map = () => currentMap;
				panel.GetWidget<LabelWidget>("MAP_TITLE").GetText =
					() => currentMap != null ? currentMap.Title : "(Unknown Map)";

				var players = currentSummary.LobbyInfo.Slots.Count(s => currentSummary.LobbyInfo.ClientInSlot(s) != null || s.Bot != null);
				panel.GetWidget<LabelWidget>("PLAYERS").GetText = () => players.ToString();
			}
			catch(Exception e)
			{
				Log.Write("debug", "Exception while parsing replay: {0}", e.ToString());
				currentSummary = null;
				currentMap = null;
			}
		}

		void AddReplay(ScrollPanelWidget list, string filename, Widget template)
		{
			var entry = template.Clone() as ContainerWidget;
			var f = Path.GetFileName(filename);
			entry.GetWidget<LabelWidget>("TITLE").GetText = () => f;
			entry.GetBackground = () => (entry.RenderBounds.Contains(Viewport.LastMousePos) ? "button-hover" : (currentSummary != null && currentSummary.Filename == filename) ? "button-pressed" : null);
			entry.OnMouseDown = mi => { if (mi.Button != MouseButton.Left) return false; SelectReplay(filename); return true; };
			entry.IsVisible = () => true;
			list.AddChild(entry);
		}
	}
}
