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
		Widget widget; 

		[ObjectCreator.UseCtor]
		public CncReplayBrowserLogic([ObjectCreator.Param] Widget widget,
		                             [ObjectCreator.Param] Action onExit,
		                             [ObjectCreator.Param] Action onStart)
		{
			this.widget = widget;

			widget.GetWidget<CncMenuButtonWidget>("CANCEL_BUTTON").OnClick = onExit;

			var rl = widget.GetWidget<ScrollPanelWidget>("REPLAY_LIST");
			var replayDir = Path.Combine(Platform.SupportDir, "Replays");

			var template = widget.GetWidget("REPLAY_TEMPLATE");
			CurrentReplay = null;

			rl.RemoveChildren();
			if (Directory.Exists(replayDir))
			{
				var files = Directory.GetFiles(replayDir, "*.rep").Reverse();
				foreach (var replayFile in files)
					AddReplay(rl, replayFile, template);
			
				CurrentReplay = files.FirstOrDefault();
			}

			var watch = widget.GetWidget<CncMenuButtonWidget>("WATCH_BUTTON");
			watch.IsDisabled = () => currentReplay == null || currentMap == null;
			watch.OnClick = () =>
			{
				if (currentReplay != null)
				{
					Game.JoinReplay(CurrentReplay);
					onStart();
				}
			};

			widget.GetWidget("REPLAY_INFO").IsVisible = () => currentReplay != null;
		}

		string currentReplay = null;
		Map currentMap = null;
		string CurrentReplay
		{
			get { return currentReplay; }
			set
			{
				currentReplay = value;
				if (currentReplay != null)
				{
					try
					{
						var summary = new ReplaySummary(currentReplay);
						currentMap = summary.Map();

						widget.GetWidget<LabelWidget>("DURATION").GetText =
							() => WidgetUtils.FormatTime(summary.Duration * 3	/* todo: 3:1 ratio isnt always true. */);
						widget.GetWidget<MapPreviewWidget>("MAP_PREVIEW").Map = () => currentMap;
						widget.GetWidget<LabelWidget>("MAP_TITLE").GetText =
							() => currentMap != null ? currentMap.Title : "(Unknown Map)";

						var players = summary.LobbyInfo.Slots.Count(s => summary.LobbyInfo.ClientInSlot(s) != null || s.Bot != null);
						widget.GetWidget<LabelWidget>("PLAYERS").GetText = () => players.ToString();
					}
					catch(Exception e)
					{
						Log.Write("debug", "Exception while parsing replay: {0}", e.ToString());
						currentReplay = null;
						currentMap = null;
					}
				}
			}
		}

		void AddReplay(ScrollPanelWidget list, string filename, Widget template)
		{
			var entry = template.Clone() as ContainerWidget;
			var f = Path.GetFileName(filename);
			entry.GetWidget<LabelWidget>("TITLE").GetText = () => f;
			entry.GetBackground = () => (entry.RenderBounds.Contains(Viewport.LastMousePos) ? "button-hover" : (CurrentReplay == filename) ? "button-pressed" : null);
			entry.OnMouseDown = mi => { if (mi.Button != MouseButton.Left) return false; CurrentReplay = filename; return true; };
			entry.IsVisible = () => true;
			list.AddChild(entry);
		}
	}
}
