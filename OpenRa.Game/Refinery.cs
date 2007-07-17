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
		const string name = "proc";
		static Sequence idle = SequenceProvider.GetSequence(name, "idle");

		public Refinery(float2 location, int palette)
		{
			this.renderLocation = location;
			this.palette = palette;
		}

		public override Sprite[] CurrentImages
		{
			get { return new Sprite[] { idle.GetSprite(0) }; }
		}
	}
}
