using System.Collections.Generic;
using OpenRA.FileFormats;
using System.Drawing;
using System.Linq;
#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Widgets.Delegates
{
	public class VideoPlayerDelegate : IWidgetDelegate
	{
		string Selected = null;
		public VideoPlayerDelegate()
		{
			var bg = Widget.RootWidget.GetWidget("VIDEOPLAYER_MENU");
			var player = bg.GetWidget<VqaPlayerWidget>("VIDEOPLAYER");
			bg.GetWidget("BUTTON_PLAY").OnMouseUp = mi =>
			{
				if (Selected == null)
					return true;
				
				player.Load(Selected);
				player.Play();
				return true;
			};
			
			bg.GetWidget("BUTTON_STOP").OnMouseUp = mi =>
			{
				player.Stop();
				return true;
			};
		
			bg.GetWidget("BUTTON_CLOSE").OnMouseUp = mi => {
				player.Stop();
				Widget.RootWidget.CloseWindow();
				return true;
			};
			
			// Menu Buttons
			Widget.RootWidget.GetWidget("MAINMENU_BUTTON_VIDEOPLAYER").OnMouseUp = mi => {
				Widget.RootWidget.OpenWindow("VIDEOPLAYER_MENU");
				return true;
			};
			
			var vl = bg.GetWidget<ListBoxWidget>("VIDEO_LIST");
			var itemTemplate = vl.GetWidget<LabelWidget>("VIDEO_TEMPLATE");
			int offset = itemTemplate.Bounds.Y;
			
			// Todo: pull into per-mod yaml / Manifest
			var tempVideos = new Dictionary<string,string>();
			tempVideos.Add("obel.vqa", "Obelisk ZZZZAAAAAP");
			tempVideos.Add("ally1.vqa", "Allies briefing #1");
			tempVideos.Add("ally10.vqa", "Allies briefing #10");
			
			Selected = tempVideos.Keys.FirstOrDefault();
			foreach (var kv in tempVideos)
			{
				var video = kv.Key;
				var title = kv.Value;
				if (!FileSystem.Exists(video))
					continue;

				var template = itemTemplate.Clone() as LabelWidget;
				template.Id = "VIDEO_{0}".F(video);
				template.GetText = () => "   " + title;
				template.GetBackground = () => ((video == Selected) ? "dialog2" : null);
				template.OnMouseDown = mi =>
				{
					if (Selected == video)
						return true;
					player.Stop();
					Selected = video;
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
