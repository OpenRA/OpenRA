#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRA.Widgets
{
	public class ChatDisplayWidget : Widget
	{
		public readonly int RemoveTime = 0;
		
		const int logLength = 9;
		public string Notification = "";
		public bool DrawBackground = true;
		int ticksUntilRemove = 0;

		internal List<ChatLine> recentLines = new List<ChatLine>();

		public ChatDisplayWidget()
			: base() { }

		protected ChatDisplayWidget(Widget widget)
			: base(widget) { }

		public override Rectangle EventBounds { get { return Rectangle.Empty; } }
		public override void DrawInner()
		{
			var pos = RenderOrigin;
			var chatLogArea = new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height);
			var chatpos = new int2(chatLogArea.X + 10, chatLogArea.Bottom - 6);
			
			if (DrawBackground)
				WidgetUtils.DrawPanel("dialog3", chatLogArea);
			
			Game.Renderer.EnableScissor(chatLogArea.Left, chatLogArea.Top, chatLogArea.Width, chatLogArea.Height);
			foreach (var line in recentLines.AsEnumerable().Reverse())
			{
				chatpos.Y -= 20;
				var owner = line.Owner + ":";
				var inset = Game.Renderer.RegularFont.Measure(owner).X + 10;
				Game.Renderer.RegularFont.DrawText(owner, chatpos, line.Color);
				Game.Renderer.RegularFont.DrawText(line.Text, chatpos + new int2(inset, 0), Color.White);
			}

			Game.Renderer.DisableScissor();
		}
		
		public void AddLine(Color c, string from, string text)
		{
			recentLines.Add(new ChatLine { Color = c, Owner = from, Text = text });
			ticksUntilRemove = RemoveTime;
			
			if (Notification != null)
				Sound.Play(Notification);

			while (recentLines.Count > logLength) recentLines.RemoveAt(0);
		}
		
		public void RemoveLine()
		{
			if (recentLines.Count > 0) recentLines.RemoveAt(0);	
		}
		
		public void ClearChat()
		{
			recentLines = new List<ChatLine>();	
		}
		
		public override void Tick()
		{
			if (RemoveTime == 0) return;
			if (--ticksUntilRemove > 0) return;
			ticksUntilRemove = RemoveTime;
			RemoveLine();
		}

		public override Widget Clone() { return new ChatDisplayWidget(this); }
	}

	class ChatLine
	{
		public Color Color = Color.White;
		public string Owner, Text;
		public bool wrapped = false;
	}
}