using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace OpenRa.FileFormats
{
	public class ShroudPaletteRemap : IPaletteRemap
	{
		public Color GetRemappedColor(Color original, int index)
		{
			// false-color version for debug

			//return new[] { 
			//    Color.FromArgb(64,0,0,0), Color.Green, 
			//    Color.Blue, Color.Yellow, 
			//    Color.Green, 
			//    Color.Red, 
			//    Color.Purple, 
			//    Color.Cyan}[index % 8];

			return new[] { 
			    Color.Transparent, Color.Green, 
			    Color.Blue, Color.Yellow, 
			    Color.Black, 
			    Color.FromArgb(192,0,0,0), 
			    Color.FromArgb(128,0,0,0), 
			    Color.FromArgb(64,0,0,0)}[index % 8];
		}
	}
}
