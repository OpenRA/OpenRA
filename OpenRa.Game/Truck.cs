using System;
using System.Collections.Generic;
using System.Text;

namespace OpenRa.Game
{
	class Truck : Unit
	{
		static Sequence sequence = SequenceProvider.GetSequence("truk", "idle");

		public Truck(int2 cell, int palette)
			: base(cell, palette, float2.Zero)
		{
		}

		public override Sprite[] CurrentImages
		{
			get { return new Sprite[] { sequence.GetSprite(facing) }; }
		}
	}
}
