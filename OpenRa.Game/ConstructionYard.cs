using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class ConstructionYard : Actor
	{
		static Range<int> normalSequence = UnitSheetBuilder.GetUnit("fact");
		static Range<int> makeSequence = UnitSheetBuilder.GetUnit("factmake");

		Range<int> sequence = makeSequence;
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
				if ((sequence.Start == makeSequence.Start) && ++frame >= sequence.End - sequence.Start)
				{
					frame = 0;
					sequence = normalSequence;
				}

				return new Sprite[] { UnitSheetBuilder.sprites[sequence.Start + frame] };
			}
		}

		public override void Tick(World world, double t) { }
	}
}
