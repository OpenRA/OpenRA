using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	interface ISelectable
	{
		//Sprite CurrentCursor( int x, int y );
		MoveOrder Order( int x, int y );
	}
}
