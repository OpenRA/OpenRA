using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	class Mcv : Actor
	{
		public Mcv( PointF location, int palette )
		{
			this.location = location;
			this.palette = palette;
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
					UnitSheetBuilder.McvSheet[GetFacing()],
					UnitSheetBuilder.McvSheet[63 - GetFacing()]
				};
			}
		}
	}
}
