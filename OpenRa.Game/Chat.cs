using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;

namespace OpenRa.Game
{
	class Chat
	{
		const int logLength = 10;

		public List<Pair<string, string>> recentLines = new List<Pair<string, string>>();
		public string typing = "";
		public bool isChatting = false;

		public void Toggle()
		{
			if (isChatting && typing.Length > 0)
				Game.controller.AddOrder(Order.Chat(Game.LocalPlayer, typing));

			typing = "";
			isChatting ^= true;
		}

		public void TypeChar(char c)
		{
			if (c == '\b')
			{
				if (typing.Length > 0)
					typing = typing.Remove(typing.Length - 1);
			}
			else
				typing += c;
		}

		public void AddLine(Pair<string, string> line)
		{
			recentLines.Add(line);
			while (recentLines.Count > logLength) recentLines.RemoveAt(0);
		}
	}
}
