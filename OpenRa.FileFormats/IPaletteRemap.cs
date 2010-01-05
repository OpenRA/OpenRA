using System;
using System.Drawing;
namespace OpenRa.FileFormats
{
	public interface IPaletteRemap
	{
		Color GetRemappedColor(Color original, int index);
	}
}
