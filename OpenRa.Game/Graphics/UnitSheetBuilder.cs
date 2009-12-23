using System.Collections.Generic;
using Ijw.DirectX;
using OpenRa.FileFormats;
using IjwFramework.Collections;

namespace OpenRa.Game.Graphics
{
	static class UnitSheetBuilder
	{
		public static void Initialize()
		{
			sprites = new List<Sprite>();
			sequences = new Cache<string, Range<int>>(AddUnit);
		}

		public static List<Sprite> sprites;
		static Cache<string, Range<int>> sequences;

		public static Range<int> GetUnit(string name) { return sequences[name]; }
		
		static Range<int> AddUnit( string name )
		{
			var low = sprites.Count;
			var reader = new ShpReader( FileSystem.OpenWithExts( name, ".tem", ".sno", ".int", ".shp" ) );
			foreach (var h in reader)
				sprites.Add(SheetBuilder.Add(h.Image, reader.Size));
			var sequence = new Range<int>(low, sprites.Count - 1);
			return sequence;
		}
	}
}
