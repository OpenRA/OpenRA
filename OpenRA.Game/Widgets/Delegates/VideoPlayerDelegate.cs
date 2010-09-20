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
using OpenRA.FileFormats;

namespace OpenRA.Widgets.Delegates
{
	public class VideoPlayerDelegate : IWidgetDelegate
	{
		string Selected;

		public VideoPlayerDelegate()
		{
			var bg = Widget.RootWidget.GetWidget("VIDEOPLAYER_MENU");
			var player = bg.GetWidget<VqaPlayerWidget>("VIDEOPLAYER");
			
			var pp = bg.GetWidget("BUTTON_PLAYPAUSE");
			pp.OnMouseUp = mi =>
			{
				if (player.Paused)
					player.Play();
				else
					player.Pause();
				
				return true;
			};
			
			pp.GetWidget("PLAY").IsVisible = () => player.Paused;
			pp.GetWidget("PAUSE").IsVisible = () => !player.Paused;

			bg.GetWidget("BUTTON_STOP").OnMouseUp = mi =>
			{
				player.Stop();
				return true;
			};
		
			bg.GetWidget("BUTTON_CLOSE").OnMouseUp = mi => {
				player.Stop();
				Widget.CloseWindow();
				return true;
			};
			
			// Menu Buttons
			Widget.RootWidget.GetWidget("MAINMENU_BUTTON_VIDEOPLAYER").OnMouseUp = mi => {
				Widget.OpenWindow("VIDEOPLAYER_MENU");
				return true;
			};
			
			var vl = bg.GetWidget<ListBoxWidget>("VIDEO_LIST");
			var itemTemplate = vl.GetWidget<LabelWidget>("VIDEO_TEMPLATE");
			int offset = itemTemplate.Bounds.Y;
			
			foreach (var kv in Rules.Movies)
			{
				var video = kv.Key;
				var title = kv.Value;
				if (!FileSystem.Exists(video))
					continue;

				if (Selected == null)
					player.Load(Selected = video);

				var template = itemTemplate.Clone() as LabelWidget;
				template.Id = "VIDEO_{0}".F(video);
				template.GetText = () => "   " + title;
				template.GetBackground = () => ((video == Selected) ? "dialog2" : null);
				template.OnMouseDown = mi =>
				{
					Selected = video;
					player.Load(video);
					return true;
				};
				template.Parent = vl;

				template.Bounds = new Rectangle(template.Bounds.X, offset, template.Bounds.Width, template.Bounds.Height);
				template.IsVisible = () => true;
				vl.AddChild(template);

				offset += template.Bounds.Height;
				vl.ContentHeight += template.Bounds.Height;
			}
		}
	}
}
