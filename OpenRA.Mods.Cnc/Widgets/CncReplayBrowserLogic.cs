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

			var template = widget.GetWidget<LabelWidget>("REPLAY_TEMPLATE");
			CurrentReplay = null;

			rl.RemoveChildren();
			if (Directory.Exists(replayDir))
				foreach (var replayFile in Directory.GetFiles(replayDir, "*.rep").Reverse())
					AddReplay(rl, replayFile, template);
			
			var watch = widget.GetWidget<CncMenuButtonWidget>("WATCH_BUTTON");
			watch.IsDisabled = () => currentReplay == null;
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

		Map MapFromSummary(ReplaySummary rs)
		{
			if (rs.LobbyInfo == null)
				return null;
			
			var map = rs.LobbyInfo.GlobalSettings.Map;
			if (!Game.modData.AvailableMaps.ContainsKey(map))
				return null;

			return Game.modData.AvailableMaps[map];
		}

		string currentReplay = null;
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
						var mapStub = MapFromSummary(summary);

						widget.GetWidget<LabelWidget>("DURATION").GetText =
							() => WidgetUtils.FormatTime(summary.Duration * 3	/* todo: 3:1 ratio isnt always true. */);
						widget.GetWidget<MapPreviewWidget>("MAP_PREVIEW").Map = () => mapStub;
						widget.GetWidget<LabelWidget>("MAP_TITLE").GetText =
							() => mapStub != null ? mapStub.Title : "(Unknown Map)";
					}
					catch(Exception e)
					{
						Log.Write("debug", "Exception while parsing replay: {0}", e.ToString());
						currentReplay = null;
					}
				}
			}
		}

		void AddReplay(ScrollPanelWidget list, string filename, LabelWidget template)
		{
			var entry = template.Clone() as LabelWidget;
			entry.Id = "REPLAY_";
			entry.GetText = () => "   {0}".F(Path.GetFileName(filename));
			entry.GetBackground = () => (CurrentReplay == filename) ? "dialog2" : null;
			entry.OnMouseDown = mi => { if (mi.Button != MouseButton.Left) return false; CurrentReplay = filename; return true; };
			entry.IsVisible = () => true;
			list.AddChild(entry);
		}
	}
}
