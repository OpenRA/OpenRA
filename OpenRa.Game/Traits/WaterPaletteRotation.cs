using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace OpenRa.Game.Traits
{
	class WaterPaletteRotation : ITick, IPaletteModifier
	{
		public WaterPaletteRotation(Actor self) { }

		float t = 0;
		public void Tick(Actor self)
		{
			t += .25f;
		}

		public void AdjustPalette(Bitmap b)
		{
			var rotate = (int)t % 7;
			using (var bitmapCopy = new Bitmap(b))
				for (int j = 0; j < 16; j++)
					for (int i = 0; i < 7; i++)
						b.SetPixel(0x60 + (rotate + i) % 7, j, bitmapCopy.GetPixel(0x60 + i, j));
		}
	}
}
