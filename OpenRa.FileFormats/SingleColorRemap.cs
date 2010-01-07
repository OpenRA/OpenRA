using System.Drawing;

namespace OpenRa.FileFormats
{
	public class SingleColorRemap : IPaletteRemap
	{
		Color c;
		public SingleColorRemap(Color c)
		{
			this.c = c;
		}

		public Color GetRemappedColor(Color original, int index)
		{
			return original.A > 0 ? c : original;
		}
	}
}
