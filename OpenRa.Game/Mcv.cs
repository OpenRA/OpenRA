using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	class Mcv : Actor
	{
		static Range<int>? mcvRange = null;

		public Mcv( float2 location, int palette )
		{
			this.location = location;
			this.palette = palette;

			if (mcvRange == null)
				mcvRange = UnitSheetBuilder.AddUnit("mcv");
		}

		int GetFacing() { return (Environment.TickCount >> 6) % 32; }

		public override Sprite[] CurrentImages
		{
			get
			{
				return new Sprite[] { UnitSheetBuilder.sprites[GetFacing() + mcvRange.Value.Start] };
			}
		}
	}
}
