using System.Collections.Generic;
using System.Drawing;
using OpenRa.FileFormats;

namespace OpenRa
{
	class Chat
	{
		const int logLength = 10;

		public List<Tuple<Color, string, string>> recentLines = new List<Tuple<Color, string, string>>();
		public string typing = "";
		public bool isChatting = true;

		public void Toggle()
		{
			if( isChatting && typing.Length > 0 )
				Game.IssueOrder( Order.Chat( typing ) );

			typing = "";
			if( Game.orderManager.GameStarted )
				isChatting ^= true;
		}
		
		public void Reset()
		{
			typing = "";
			isChatting = false;
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

		public void AddLine(Player p, string text)
		{
			AddLine(p.Color, p.PlayerName, text);
		}

		public void AddLine(Color c, string from, string text)
		{
			Log.Write( "Chat: {0}: {1}", from, text );
			recentLines.Add(Tuple.New(c, from, text));
			Sound.Play("rabeep1.aud");
			while (recentLines.Count > logLength) recentLines.RemoveAt(0);
		}
	}
}
