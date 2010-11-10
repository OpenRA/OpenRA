#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Network;
using System;

namespace OpenRA.Widgets.Delegates
{
	public class ReplayBrowserDelegate : IWidgetDelegate
	{
		Widget widget; 

		[ObjectCreator.UseCtor]
		public ReplayBrowserDelegate( [ObjectCreator.Param] Widget widget )
		{
			this.widget = widget;

			widget.GetWidget("CANCEL_BUTTON").OnMouseUp = mi =>
				{
					Widget.CloseWindow();
					return true;
				};

			/* find some replays? */
			var rl = widget.GetWidget<ListBoxWidget>("REPLAY_LIST");
			var replayDir = Path.Combine(Game.SupportDir, "Replays");

			var template = widget.GetWidget<LabelWidget>("REPLAY_TEMPLATE");
			CurrentReplay = null;

			rl.Children.Clear();
			rl.ContentHeight = 0;
			var offset = template.Bounds.Y;
			foreach (var replayFile in Directory.GetFiles(replayDir, "*.rep"))
				AddReplay(rl, replayFile, template, ref offset);

			widget.GetWidget("WATCH_BUTTON").OnMouseUp = mi =>
				{
					Widget.CloseWindow();
					Game.JoinReplay(CurrentReplay);
					return true;
				};

			widget.GetWidget("REPLAY_INFO").IsVisible = () => currentReplay != null;
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
					var summary = new ReplaySummary(currentReplay);
					widget.GetWidget<LabelWidget>("DURATION").GetText = 
						() => WorldUtils.FormatTime(summary.Duration * 3	/* todo: 3:1 ratio isnt always true. */);
				}
			}
		}

		void AddReplay(ListBoxWidget list, string filename, LabelWidget template, ref int offset)
		{
			var entry = template.Clone() as LabelWidget;
			entry.Id = "REPLAY_";
			entry.GetText = () => "   {0}".F(Path.GetFileName(filename));
			entry.GetBackground = () => (CurrentReplay == filename) ? "dialog2" : null;
			entry.OnMouseDown = mi => { CurrentReplay = filename; return true; };
			entry.Parent = list;
			entry.Bounds = new Rectangle(entry.Bounds.X, offset, template.Bounds.Width, template.Bounds.Height);
			entry.IsVisible = () => true;
			list.AddChild(entry);

			if (offset == template.Bounds.Y)
				CurrentReplay = filename;

			offset += template.Bounds.Height;
			list.ContentHeight += template.Bounds.Height;
		}
	}

	/* a maze of twisty little hacks,... */
	class ReplaySummary
	{
		public readonly int Duration;

		public ReplaySummary(string filename)
		{
			var lastFrame = 0;
			using (var conn = new ReplayConnection(filename))
				conn.Receive((client, packet) =>
					{
						var frame = BitConverter.ToInt32(packet, 0);
						if (packet.Length == 5 && packet[4] == 0xBF)
							return;	// disconnect
						else if (packet.Length >= 5 && packet[4] == 0x65)
							return;	// sync
						else if (frame == 0)
						{
							/* decode this to recover lobbyinfo, etc */
						}
						else
							lastFrame = Math.Max(lastFrame, frame);
					});

			Duration = lastFrame;
		}
	}
}
