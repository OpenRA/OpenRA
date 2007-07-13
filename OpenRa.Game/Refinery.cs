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
		static Range<int>? refineryRange = null;

		public Refinery(PointF location, int palette)
		{
			if (refineryRange == null)
				refineryRange = UnitSheetBuilder.AddUnit("proc");

			this.location = location;
			this.palette = palette;
		}

		int GetFrame()
		{
			return 1;//
		}

		public override SheetRectangle<Sheet>[] CurrentImages
		{
			get
			{
				return new SheetRectangle<Sheet>[] { UnitSheetBuilder.sprites[refineryRange.Value.Start + GetFrame()] };
			}
		}
	}
}
