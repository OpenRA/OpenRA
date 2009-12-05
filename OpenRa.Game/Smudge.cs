
namespace OpenRa.Game
{
	static class Smudge
	{
		const int firstScorch = 11;
		const int firstCrater = 17;
		const int framesPerCrater = 5;

		public static void AddSmudge(bool isCrater, int x, int y)
		{
			var smudge = Rules.Map.MapTiles[x, y].smudge;
			if (smudge == 0)
				Rules.Map.MapTiles[x, y].smudge = (byte) (isCrater
					? (firstCrater + framesPerCrater * ChooseSmudge())
					: (firstScorch + ChooseSmudge()));

			if (smudge < firstCrater || !isCrater) return; /* bib or scorch; don't change */
			
			/* deepen the crater */
			var amount = (smudge - firstCrater) % framesPerCrater;
			if (amount < framesPerCrater - 1)
				Rules.Map.MapTiles[x, y].smudge++;
		}

		static int lastSmudge = 0;
		static int ChooseSmudge() { lastSmudge = (lastSmudge + 1) % 6; return lastSmudge; }
	}
}
