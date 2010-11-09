#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.IO;
using System.Drawing;
namespace OpenRA.Widgets.Delegates
{
	public class ReplayBrowserDelegate : IWidgetDelegate
	{
		[ObjectCreator.UseCtor]
		public ReplayBrowserDelegate( [ObjectCreator.Param] Widget widget )
		{
			/* todo */

			widget.GetWidget("CANCEL_BUTTON").OnMouseUp = mi =>
				{
					Widget.CloseWindow();
					return true;
				};

			/* find some replays? */
			var rl = widget.GetWidget<ListBoxWidget>("REPLAY_LIST");
			var replayDir = Path.Combine(Game.SupportDir, "Replays");

			var template = widget.GetWidget<LabelWidget>("REPLAY_TEMPLATE");
			currentReplay = null;

			rl.Children.Clear();
			rl.ContentHeight = 0;
			var offset = template.Bounds.Y;
			foreach (var replayFile in Directory.GetFiles(replayDir, "*.rep"))
				AddReplay(rl, replayFile, template, ref offset);

			widget.GetWidget("WATCH_BUTTON").OnMouseUp = mi =>
				{
					Widget.CloseWindow();
					Game.JoinReplay(currentReplay);
					return true;
				};
		}

		string currentReplay = null;

		void AddReplay(ListBoxWidget list, string filename, LabelWidget template, ref int offset)
		{
			var entry = template.Clone() as LabelWidget;
			entry.Id = "REPLAY_";
			entry.GetText = () => "   {0}".F(Path.GetFileName(filename));
			entry.GetBackground = () => (currentReplay == filename) ? "dialog2" : null;
			entry.OnMouseDown = mi => { currentReplay = filename; return true; };
			entry.Parent = list;
			entry.Bounds = new Rectangle(entry.Bounds.X, offset, template.Bounds.Width, template.Bounds.Height);
			entry.IsVisible = () => true;
			list.AddChild(entry);

			if (offset == template.Bounds.Y)
				currentReplay = filename;

			offset += template.Bounds.Height;
			list.ContentHeight += template.Bounds.Height;
		}
	}
}
