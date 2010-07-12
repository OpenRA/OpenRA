#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.FileFormats;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System;

namespace OpenRA.Widgets
{
	class ChatDisplayWidget : Widget
	{
		const int logLength = 10;
		public string Notification = "";

		public List<ChatLine> recentLines = new List<ChatLine>();
		
		public ChatDisplayWidget()
			: base() {}
		
		public ChatDisplayWidget(Widget widget)
			:base(widget) {}

		public override void DrawInner(World world)
		{
			var pos = RenderOrigin;
			var chatLogArea = new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height);
			var chatpos = new int2(chatLogArea.X + 10, chatLogArea.Bottom - 6);
			WidgetUtils.DrawPanel("dialog3", chatLogArea);
			
			var renderer = Game.chrome.renderer;
			var font = renderer.RegularFont;
			
			renderer.Device.EnableScissor(chatLogArea.Left, chatLogArea.Top, chatLogArea.Width, chatLogArea.Height);
			foreach (var line in recentLines.AsEnumerable().Reverse())
			{
				chatpos.Y -= 20;
				var owner = line.Owner + ":";
				var inset = font.Measure(owner).X + 10;
				font.DrawText(owner, chatpos, line.Color);
				font.DrawText(line.Text, chatpos + new int2(inset, 0), Color.White);
			}

			renderer.RgbaSpriteRenderer.Flush();
			renderer.Device.DisableScissor();
		}

		public void AddLine(Color c, string from, string text)
		{
			recentLines.Add(new ChatLine { Color = c, Owner = from, Text = text });
			
			if (Notification != null)
				Sound.Play(Notification);
			
			while (recentLines.Count > logLength) recentLines.RemoveAt(0);
		}
		
		public override Widget Clone()
		{	
			return new ChatDisplayWidget(this);
		}
	}
	class ChatLine { public Color Color = Color.White; public string Owner, Text; public bool wrapped = false; }

}