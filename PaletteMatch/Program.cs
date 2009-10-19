using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OpenRa.FileFormats;
using System.Drawing;

namespace PaletteMatch
{
	/* a simple little hack to work out a sane matching between TD and RA palettes (or, indeed, from RA -> RA) */
	/* usage: PaletteMatch srcpal destpal */

	static class Program
	{
		static void Main(string[] args)
		{
			var tdPalette = WithStream(args[0], s => new Palette(s));
			var raPalette = WithStream(args[1], s => new Palette(s));

			var ms = tdPalette.Entries().Select(
				(a, i) => new
				{
					Src = i,
					Dest = raPalette.Entries().Select(
						(b, j) => new { Color = b, Index = j })
						.OrderBy(x => x.Color, new ColorDistanceComparer(a)).First().Index
				});

			foreach( var m in ms )
				Console.WriteLine("{0:x2} -> {1:x2}", m.Src, m.Dest);
		}

		static IEnumerable<Color> Entries(this Palette p)
		{
			for (var i = 0; i < 256; i++)
				yield return p.GetColor(i);
		}

		static T WithStream<T>(string filename, Func<Stream, T> f)
		{
			using (var s = File.OpenRead(filename))
				return f(s);
		}
	}

	class ColorDistanceComparer : IComparer<Color>
	{
		readonly Color r;

		public ColorDistanceComparer(Color r)
		{
			this.r = r;
		}

		float Distance(Color a)
		{
			var b = a.GetBrightness() - r.GetBrightness();
			var h = a.GetHue() - r.GetHue();
			var s = a.GetSaturation() - r.GetSaturation();

			return b * b + h * h + s * s;
		}

		public int Compare(Color x, Color y)
		{
			return Math.Sign(Distance(x) - Distance(y));
		}
	}

}
