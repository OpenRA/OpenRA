using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class ConstructionYard : Actor
	{
		const string name = "fact";

		static Sequence idle = SequenceProvider.GetSequence(name, "idle");
		static Sequence make = SequenceProvider.GetSequence(name, "make");

		Sequence current = make;
		int frame = -1;

		public ConstructionYard(float2 location, int palette)
		{
			this.renderLocation = location;
			this.palette = palette;
		}

		public override Sprite[] CurrentImages
		{
			get
			{
				if ((current == make) && ++frame >= current.Length)
				{
					frame = 0;
					current = idle;
				}

				return new Sprite[] { current.GetSprite(frame) };
			}
		}
	}
}
