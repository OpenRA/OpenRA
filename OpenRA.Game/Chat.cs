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

using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA
{
	class Chat
	{
		const int logLength = 10;

		public List<ChatLine> recentLines = new List<ChatLine>();
		public string typing = "";
		public bool isChatting = true;
		public bool isTeamChat = false;

		public void Toggle()
		{
			if( isChatting && typing.Length > 0 )
				Game.IssueOrder( isTeamChat ? Order.TeamChat( typing ) : Order.Chat( typing ) );

			typing = "";
			if( Game.orderManager.GameStarted )
				isChatting ^= true;
		}
		
		public void Reset()
		{
			typing = "";
			isChatting = false;
			recentLines.Clear();
		}

		public void TypeChar(char c)
		{
			if (c == '\b' || c == 0x7f)
			{
				if (typing.Length > 0)
					typing = typing.Remove(typing.Length - 1);
			}
			else if (!char.IsControl(c))
				typing += c;
		}

		public void AddLine(Session.Client p, string text)
		{
			AddLine(Game.world.PlayerColors()[p.PaletteIndex].Color, p.Name, text);
		}

		public void AddLine(Color c, string from, string text)
		{
			recentLines.Add(new ChatLine { Color = c, Owner = from, Text = text });
			var eva = Rules.Info["world"].Traits.Get<EvaAlertsInfo>();
			Sound.Play(eva.ChatBeep);
			while (recentLines.Count > logLength) recentLines.RemoveAt(0);
		}
	}

	class ChatLine { public Color Color = Color.White; public string Owner, Text; }
}
