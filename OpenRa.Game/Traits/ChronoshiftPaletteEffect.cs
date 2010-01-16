using System.Drawing;
using OpenRa.Graphics;

namespace OpenRa.Traits
{
	class ChronoshiftPaletteEffectInfo : StatelessTraitInfo<ChronoshiftPaletteEffect> { }

	public class ChronoshiftPaletteEffect : IPaletteModifier, ITick
	{
		const int chronoEffectLength = 20;
		int remainingFrames;

		public void DoChronoshift()
		{
			remainingFrames = chronoEffectLength;
		}

		public void Tick(Actor self)
		{
			if (remainingFrames > 0)
				remainingFrames--;
		}

		public void AdjustPalette(Bitmap b)
		{
			if (remainingFrames == 0)
				return;

			var frac = (float)remainingFrames / chronoEffectLength;
			for( var y = 0; y < (int)PaletteType.Chrome; y++ )
				for (var x = 0; x < 256; x++)
				{
					var orig = b.GetPixel(x, y);
					var lum = (int)(255 * orig.GetBrightness());
					var desat = Color.FromArgb(orig.A, lum, lum, lum);
					b.SetPixel(x, y, Graphics.Util.Lerp(frac, orig, desat));
				}
		}
	}
}
