using System.Collections.Generic;
using System.Linq;

namespace OpenRA.TilesetBuilder
{
	class Template
	{
		public Dictionary<int2, bool> Cells = new Dictionary<int2, bool>();

		public int Left { get { return Cells.Keys.Min(c => c.X); } }
		public int Top { get { return Cells.Keys.Min(c => c.Y); } }

		public int Right { get { return Cells.Keys.Max(c => c.X) + 1; } }
		public int Bottom { get { return Cells.Keys.Max(c => c.Y) + 1; } }

		public int Width { get { return Right - Left; } }
		public int Height { get { return Bottom - Top; } }
	}
}
