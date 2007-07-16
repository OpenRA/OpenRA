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
		static Range<int> sequence = UnitSheetBuilder.GetUnit("proc");

		public Refinery(float2 location, int palette)
		{
			this.renderLocation = location;
			this.palette = palette;
		}

		int GetFrame() { return 1; }

		public override Sprite[] CurrentImages
		{
			get
			{
				return new Sprite[] { UnitSheetBuilder.sprites[sequence.Start + GetFrame()] };
			}
		}
	}
}
