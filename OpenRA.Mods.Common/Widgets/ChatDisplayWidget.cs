#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

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
		public readonly int TextLineBoxHeight = 16;
		public readonly int Space = 4;

		const int LogLength = 9;
		List<TextNotification> recentLines = new List<TextNotification>();
		List<int> lineExpirations = new List<int>();

		public override Rectangle EventBounds => Rectangle.Empty;

		public override void Draw()
		{
			var pos = RenderOrigin;
			var chatLogArea = new Rectangle(pos.X, pos.Y, Bounds.Width, Bounds.Height);
			var chatPos = new int2(chatLogArea.X + 5, chatLogArea.Bottom - 8);

			var font = Game.Renderer.Fonts["Regular"];
			Game.Renderer.EnableScissor(chatLogArea);

			foreach (var line in recentLines.AsEnumerable().Reverse())
			{
				var lineHeight = TextLineBoxHeight;
				var inset = 0;
				string name = null;

				if (!string.IsNullOrEmpty(line.Prefix))
				{
					name = line.Prefix + ":";
					inset = font.Measure(name).X + 5;
				}

				var text = WidgetUtils.WrapText(line.Text, chatLogArea.Width - inset - 6, font);
				var textSize = font.Measure(text).Y;
				var offset = font.TopOffset;

				if (chatPos.Y - font.TopOffset < pos.Y)
					break;

				var textLineHeight = lineHeight;

				var dh = textSize - textLineHeight;
				if (dh > 0)
					textLineHeight += dh;

				var textOffset = textLineHeight - (textLineHeight - textSize - offset) / 2;
				var textPos = new int2(chatPos.X + inset, chatPos.Y - textOffset);

				if (name != null)
				{
					var nameSize = font.Measure(name).Y;
					var namePos = chatPos.WithY(chatPos.Y - (textLineHeight - (lineHeight - nameSize - offset) / 2));

					if (UseContrast)
						font.DrawTextWithContrast(name, namePos,
							line.PrefixColor, BackgroundColorDark, BackgroundColorLight, 1);
					else if (UseShadow)
						font.DrawTextWithShadow(name, namePos,
							line.PrefixColor, BackgroundColorDark, BackgroundColorLight, 1);
					else
						font.DrawText(name, namePos, line.PrefixColor);
				}

				if (UseContrast)
					font.DrawTextWithContrast(text, textPos,
						line.TextColor, Color.Black, 1);
				else if (UseShadow)
					font.DrawTextWithShadow(text, textPos,
						line.TextColor, Color.Black, 1);
				else
					font.DrawText(text, textPos, Color.White);

				chatPos = chatPos.WithY(chatPos.Y - Space - textLineHeight);
			}

			Game.Renderer.DisableScissor();
		}

		public void AddLine(TextNotification chatLine)
		{
			recentLines.Add(chatLine);
			lineExpirations.Add(Game.LocalTick + RemoveTime);

			if (Notification != null)
				Game.Sound.Play(SoundType.UI, Notification);

			while (recentLines.Count > LogLength)
				RemoveLine();
		}

		public void RemoveMostRecentLine()
		{
			if (recentLines.Count == 0)
				return;

			recentLines.RemoveAt(recentLines.Count - 1);
			lineExpirations.RemoveAt(lineExpirations.Count - 1);
		}

		public void RemoveLine()
		{
			if (recentLines.Count == 0)
				return;

			recentLines.RemoveAt(0);
			lineExpirations.RemoveAt(0);
		}

		public override void Tick()
		{
			if (RemoveTime == 0)
				return;

			// This takes advantage of the fact that recentLines is ordered by expiration, from sooner to later
			while (recentLines.Count > 0 && Game.LocalTick >= lineExpirations[0])
				RemoveLine();
		}
	}
}
