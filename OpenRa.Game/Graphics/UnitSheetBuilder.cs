using System.Collections.Generic;
using Ijw.DirectX;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	static class UnitSheetBuilder
	{
		public static void Initialize()
		{
			sprites = new List<Sprite>();
			sequences = new Dictionary<string, Range<int>>();
		}

		public static List<Sprite> sprites;
		static Dictionary<string, Range<int>> sequences;

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

			ShpReader reader = new ShpReader( FileSystem.OpenWithExts( name, ".tem", ".sno", ".int", ".shp" ) );
			foreach (ImageHeader h in reader)
				sprites.Add(SheetBuilder.Add(h.Image, reader.Size));

			Range<int> sequence = new Range<int>(low, sprites.Count - 1);
			sequences.Add(name, sequence);

			Log.Write("Loaded SHP for {0}", name);

			return sequence;
		}
	}
}
