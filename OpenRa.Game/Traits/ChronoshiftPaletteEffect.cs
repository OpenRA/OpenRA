using System.Drawing;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class ChronoshiftPaletteEffect : IPaletteModifier, ITick
	{
		const int chronoEffectLength = 20;
		int remainingFrames;

		public ChronoshiftPaletteEffect(Actor self) { }

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
					b.SetPixel(x, y, Lerp(frac, orig, desat));
				}
		}

		static Color Lerp(float t, Color a, Color b)
		{
			return Color.FromArgb(
				LerpChannel(t, a.A, b.A),
				LerpChannel(t, a.R, b.R),
				LerpChannel(t, a.G, b.G),
				LerpChannel(t, a.B, b.B));
		}

		static int LerpChannel(float t, int a, int b)
		{
			return (int)((1 - t) * a + t * b);
		}
	}
}
