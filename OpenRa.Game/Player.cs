using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	class Player
	{
		public int Palette;
		public string PlayerName;

		public Player( int palette, string playerName )
		{
			this.Palette = palette;
			this.PlayerName = playerName;
		}
	}
}
