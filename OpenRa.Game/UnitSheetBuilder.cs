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
		static readonly Package otherUnitsPackage = new Package("../../../hires.mix");

		public static readonly List<SheetRectangle<Sheet>> McvSheet = new List<SheetRectangle<Sheet>>();

		static ShpReader Load(string filename)
		{
			foreach( Package p in new Package[] { unitsPackage, otherUnitsPackage } )
				try { return new ShpReader(p.GetContent(filename)); }
				catch { }

			throw new NotImplementedException();
		}

		public static void AddUnit( string name )
		{
			ShpReader reader = Load(name + ".shp");
			foreach (ImageHeader h in reader)
				McvSheet.Add(CoreSheetBuilder.Add(h.Image, reader.Size));
		}
	}
}
