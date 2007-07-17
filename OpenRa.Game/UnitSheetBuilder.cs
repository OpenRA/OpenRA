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

		public static readonly List<Sprite> sprites = new List<Sprite>();

		static Dictionary<string, Range<int>> sequences = new Dictionary<string, Range<int>>();

		static ShpReader Load(string filename)
		{
			foreach( Package p in new Package[] { unitsPackage, otherUnitsPackage } )
				try { return new ShpReader(p.GetContent(filename)); }
				catch { }

			throw new NotImplementedException();
		}

		public static Range<int> GetUnit(string name)
		{
			Range<int> result;
			if (sequences.TryGetValue(name, out result))
				return result;

			return AddUnit(name);
		}

		static Range<int> AddUnit( string name )
		{
			Log.Write("Loading SHP for {0}", name);

			int low = sprites.Count;
			ShpReader reader = Load(name + ".shp");
			foreach (ImageHeader h in reader)
				sprites.Add(SheetBuilder.Add(h.Image, reader.Size));

			Range<int> sequence = new Range<int>(low, sprites.Count - 1);
			sequences.Add(name, sequence);

			Log.Write("Loaded SHP for {0}", name);

			return sequence;
		}
	}
}
