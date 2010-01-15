using System.Drawing;

namespace OpenRa.Game.Traits
{
	class LightPaletteRotatorInfo : StatelessTraitInfo<LightPaletteRotator> { }
	class LightPaletteRotator : ITick, IPaletteModifier
	{
		float t = 0;
		public void Tick(Actor self)
		{
			t += .5f;
		}

		public void AdjustPalette(Bitmap b)
		{
			var rotate = (int)t % 18;
			if (rotate > 9)
				rotate = 18 - rotate;
			
			using (var bitmapCopy = new Bitmap(b))
				for (int j = 0; j < 16; j++)
					b.SetPixel(0x67, j, b.GetPixel(230+rotate, j));
		}
	}
}
