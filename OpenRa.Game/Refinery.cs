using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using BluntDirectX.Direct3D;
using System.Drawing;

namespace OpenRa.Game
{
	class Refinery : Building
	{
		public Refinery( int2 location, Player owner )
			: base( "proc", location, owner )
		{
		}
	}
}
