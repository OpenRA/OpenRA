using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	interface IOrderGenerator
	{
		//Sprite CurrentCursor( int x, int y );
		IOrder Order( int2 xy );
	}
}
