#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class ChatDisplayWidget : Widget
	{
		public readonly int RemoveTime = 0;
		public readonly bool UseContrast = false;
		public readonly bool UseShadow = false;
		public readonly Color BackgroundColorDark = ChromeMetrics.Get<Color>("TextContrastColorDark");
		public readonly Color BackgroundColorLight = ChromeMetrics.Get<Color>("TextContrastColorLight");
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
				string name = null;

				if (!string.IsNullOrEmpty(line.Name))
				{
					name = line.Name + ":";
					inset = font.Measure(name).X + 5;
				}

				var text = WidgetUtils.WrapText(line.Text, chatLogArea.Width - inset - 6, font);
				chatpos = chatpos.WithY(chatpos.Y - (Math.Max(15, font.Measure(text).Y) + 5));

				if (chatpos.Y < pos.Y)
					break;

				if (name != null)
				{
					if (UseContrast)
						font.DrawTextWithContrast(name, chatpos,
							line.NameColor, BackgroundColorDark, BackgroundColorLight, 1);
					else if (UseShadow)
						font.DrawTextWithShadow(name, chatpos,
							line.NameColor, BackgroundColorDark, BackgroundColorLight, 1);
					else
						font.DrawText(name, chatpos, line.NameColor);
				}

				if (UseContrast)
					font.DrawTextWithContrast(text, chatpos + new int2(inset, 0),
						line.TextColor, Color.Black, 1);
				else if (UseShadow)
					font.DrawTextWithShadow(text, chatpos + new int2(inset, 0),
						line.TextColor, Color.Black, 1);
				else
					font.DrawText(text, chatpos + new int2(inset, 0), Color.White);
			}

			Game.Renderer.DisableScissor();
		}

		public void AddLine(string name, Color nameColor, string text, Color textColor)
		{
			recentLines.Add(new ChatLine(name, nameColor, text, textColor, Game.LocalTick + RemoveTime));

			if (Notification != null)
				Game.Sound.Play(SoundType.UI, Notification);

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
		public readonly Color NameColor;
		public readonly Color TextColor;
		public readonly string Name, Text;
		public readonly int Expiration;

		public ChatLine(string name, Color nameColor, string text, Color textColor, int expiration)
		{
			Name = name;
			Text = text;
			Expiration = expiration;
			NameColor = nameColor;
			TextColor = textColor;
		}
	}
}
