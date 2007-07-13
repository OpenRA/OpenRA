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

		public static readonly List<SheetRectangle<Sheet>> sprites = new List<SheetRectangle<Sheet>>();

		static ShpReader Load(string filename)
		{
			foreach( Package p in new Package[] { unitsPackage, otherUnitsPackage } )
				try { return new ShpReader(p.GetContent(filename)); }
				catch { }

			throw new NotImplementedException();
		}

		public static Range<int> AddUnit( string name )
		{
			int low = sprites.Count;
			ShpReader reader = Load(name + ".shp");
			foreach (ImageHeader h in reader)
				sprites.Add(CoreSheetBuilder.Add(h.Image, reader.Size));

			return new Range<int>(low, sprites.Count - 1);
		}
	}
}
