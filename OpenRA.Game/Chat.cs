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
using System.Text;

namespace OpenRA
{
	class Chat
	{
		const int logLength = 10;
		public List<ChatLine> recentLines = new List<ChatLine>();

		public void AddLine(Session.Client p, string text)
		{
			AddLine(Game.world.PlayerColors()[p.PaletteIndex].Color, p.Name, text);
		}

		public void AddLine(Color c, string from, string text)
		{
			var sizeOwner = Game.chrome.renderer.RegularFont.Measure(from);
			var sizeText = Game.chrome.renderer.RegularFont.Measure(text);
			
			if (sizeOwner.X + sizeText.X + 10 <= Chrome.ChatWidth)
				recentLines.Add(new ChatLine { Color = c, Owner = from, Text = text });
			else
			{
				StringBuilder sb = new StringBuilder();
				foreach (var w in text.Split(' '))
				{
					if ( Game.chrome.renderer.RegularFont.Measure(sb.ToString() + ' ' + w).X > Chrome.ChatWidth )
					{
						recentLines.Add(new ChatLine { Color = c, Owner = from, Text = sb.ToString() } );
						sb = new StringBuilder();
						sb.Append(w);
					}
					else 
						sb.Append( ' '  + w);
				}
				if (sb.Length != 0)	
					recentLines.Add(new ChatLine { Color = c, Owner = from, Text = sb.ToString() } );
			}
			
			var eva = Rules.Info["world"].Traits.Get<EvaAlertsInfo>();
			Sound.Play(eva.ChatBeep);
			while (recentLines.Count > logLength) recentLines.RemoveAt(0);
		}
	}

	class ChatLine { public Color Color = Color.White; public string Owner, Text; public bool wrapped = false; }
}
