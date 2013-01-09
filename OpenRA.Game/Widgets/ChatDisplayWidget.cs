#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		public readonly bool UseContrast = false;

		const int logLength = 9;
		public string Notification = "";
		public bool DrawBackground = true;
		int ticksUntilRemove = 0;

		internal List<ChatLine> recentLines = new List<ChatLine>();

		public ChatDisplayWidget()
			: base() { }

		public override Rectangle EventBounds { get { return Rectangle.Empty; } }

		public override void Draw()
		{
			var pos = RenderOrigin;
			var chatLogArea = new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height);
			var chatpos = new int2(chatLogArea.X + 10, chatLogArea.Bottom - 6);

			if (DrawBackground)
				WidgetUtils.DrawPanel("dialog3", chatLogArea);

			var font = Game.Renderer.Fonts["Regular"];

			Game.Renderer.EnableScissor(chatLogArea.Left, chatLogArea.Top, chatLogArea.Width, chatLogArea.Height);

			foreach (var line in recentLines.AsEnumerable().Reverse())
			{
				var inset = 0;
				string owner = null;

				if (!string.IsNullOrEmpty(line.Owner))
				{
					owner = line.Owner + ":";
					inset = font.Measure(owner).X + 10;
				}

				var text = WidgetUtils.WrapText(line.Text, chatLogArea.Width - inset, font);
				var textLines = text.Split(new[] { '\n' }).Count();
				chatpos.Y -= 20 * textLines;

				if (owner != null)
				{
					font.DrawTextWithContrast(owner, chatpos,
						line.Color, Color.Black, UseContrast ? 1 : 0);
				}

				font.DrawTextWithContrast(text, chatpos + new int2(inset, 0),
					Color.White, Color.Black, UseContrast ? 1 : 0);
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
	}

	class ChatLine
	{
		public Color Color = Color.White;
		public string Owner, Text;
	}
}