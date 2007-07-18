using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	class Harvester : Unit
	{
		static Sequence idle = SequenceProvider.GetSequence("harv", "idle");

		public override Sprite[] CurrentImages
		{
			get { return new Sprite[] { idle.GetSprite(facing) }; }
		}

		public Harvester(int2 cell, int palette)
			: base(cell, palette, new float2(12,12))
		{
		}
	}
}
