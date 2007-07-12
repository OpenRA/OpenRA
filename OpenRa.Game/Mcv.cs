using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	class Mcv : Actor
	{
		//int facing; // not currently used

		public Mcv( PointF location )
		{
			this.location = location;
		}

		public override SheetRectangle<Sheet>[] CurrentImages
		{
			get { return new SheetRectangle<Sheet>[] { UnitSheetBuilder.McvSheet[ 0 ] }; }
		}
	}
}
