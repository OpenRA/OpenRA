using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using BluntDirectX.Direct3D;
using System.Drawing;

namespace OpenRa.Game
{
	class Refinery : Actor
	{
		Animation a = new Animation( "proc" );

		public Refinery(float2 location, int palette)
		{
			a.PlayToEnd( "idle" );
			this.renderLocation = location;
			this.palette = palette;
		}

		public override Sprite[] CurrentImages
		{
			get { return a.Images; }
		}
	}
}
