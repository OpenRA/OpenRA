#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ChatDisplayWidget : Widget
	{
		public readonly int RemoveTime = 0;
		public readonly bool UseContrast = false;
		public string Notification = "";

		const int LogLength = 9;
		List<ChatLine> recentLines = new List<ChatLine>();

		public override Rectangle EventBounds { get { return Rectangle.Empty; } }

		public override void Draw()
		{
			var pos = RenderOrigin;
			var chatLogArea = new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height);
			var chatpos = new int2(chatLogArea.X + 5, chatLogArea.Bottom - 5);

			var font = Game.Renderer.Fonts["Regular"];
			Game.Renderer.EnableScissor(chatLogArea);

			foreach (var line in recentLines.AsEnumerable().Reverse())
			{
				var inset = 0;
				string owner = null;

				if (!string.IsNullOrEmpty(line.Owner))
				{
					owner = line.Owner + ":";
					inset = font.Measure(owner).X + 5;
				}

				var text = WidgetUtils.WrapText(line.Text, chatLogArea.Width - inset - 6, font);
				chatpos = chatpos.WithY(chatpos.Y - (Math.Max(15, font.Measure(text).Y) + 5));

				if (chatpos.Y < pos.Y)
					break;

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
			recentLines.Add(new ChatLine(from, text, Game.LocalTick + RemoveTime, c));

			if (Notification != null)
				Game.Sound.Play(Notification);

			while (recentLines.Count > LogLength)
				recentLines.RemoveAt(0);
		}

		public void RemoveLine()
		{
			if (recentLines.Count > 0)
				recentLines.RemoveAt(0);
		}

		public override void Tick()
		{
			if (RemoveTime == 0)
				return;

			// This takes advantage of the fact that recentLines is ordered by expiration, from sooner to later
			while (recentLines.Count > 0 && Game.LocalTick >= recentLines[0].Expiration)
				recentLines.RemoveAt(0);
		}
	}

	class ChatLine
	{
		public readonly Color Color;
		public readonly string Owner, Text;
		public readonly int Expiration;

		public ChatLine(string owner, string text, int expiration, Color color)
		{
			Owner = owner;
			Text = text;
			Expiration = expiration;
			Color = color;
		}
	}
}