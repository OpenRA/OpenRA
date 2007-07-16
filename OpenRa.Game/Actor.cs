using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenRa.FileFormats;
using BluntDirectX.Direct3D;
using System.Windows.Forms;

namespace OpenRa.Game
{
	abstract class Actor
	{
		public float2 renderLocation;
		public int palette;
		public abstract Sprite[] CurrentImages { get; }
		public abstract void Tick( World world, double t );
	}
}
