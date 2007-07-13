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

		public Mcv( PointF location, int palette )
		{
			this.location = location;
			this.palette = palette;

			if (mcvRange == null)
				mcvRange = UnitSheetBuilder.AddUnit("mcv");
		}

		int GetFacing()
		{
			int x = (Environment.TickCount >> 6) % 32;
			return x;
			//return x < 32 ? x : 63 - x;
		}

		public override SheetRectangle<Sheet>[] CurrentImages
		{
			get
			{
				return new SheetRectangle<Sheet>[] 
				{ 
					UnitSheetBuilder.sprites[GetFacing() + mcvRange.Value.Start]
				};
			}
		}
	}
}
