using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.FileFormats
{
	public static class ConnectedComponents
	{
		static readonly int2[] Neighbors 
			= { new int2(-1, -1), new int2(0, -1), new int2(1, -1), new int2(-1, 0) };

		public static int[,] Extract(Map m, Func<int, int, int> f)
		{
			var result = new int[m.MapSize, m.MapSize];
			var types = new int[m.MapSize, m.MapSize];
			var d = new Dictionary<int,int>();
			var n = 1;

			for( var j = m.YOffset; j < m.YOffset + m.Height; j++ )
				for (var i = m.XOffset; i < m.XOffset + m.Width; i++)
				{
					types[i, j] = f(i, j);

					var k = n;
					foreach (var a in Neighbors)
						if (types[i + a.X, j + a.Y] == types[i, j] && result[i + a.X, j + a.Y] < k)
							k = result[i + a.X, j + a.Y];

					// todo: finish this
				}

			return result;
		}
	}
}
