using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	static class UnitSheetBuilder
	{
		static readonly Package unitsPackage = new Package( "../../../conquer.mix" );
		public static readonly List<SheetRectangle<Sheet>> McvSheet = new List<SheetRectangle<Sheet>>();

		public static void AddUnit( string name )
		{
			ShpReader reader = new ShpReader( unitsPackage.GetContent( name + ".shp" ) );
			foreach (ImageHeader h in reader)
				McvSheet.Add(CoreSheetBuilder.Add(h.Image, reader.Size));
		}
	}
}
