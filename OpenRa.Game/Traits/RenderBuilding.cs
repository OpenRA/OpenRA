using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using OpenRa.Game.Graphics;
using OpenRa.Game.GameRules;
using OpenRa.FileFormats;
using OpenRa.Game;

namespace OpenRa.Game.Traits
{
	class RenderBuilding : RenderSimple
	{
		const int SmallBibStart = 1;
		const int LargeBibStart = 5;

		public RenderBuilding(Actor self)
			: base(self)
		{
			anim.PlayThen("make", () => anim.PlayRepeating("idle"));

			// at this point, we already know where we are, so we can safely place the bib in the smudge
			var buildingInfo = (UnitInfo.BuildingInfo)self.unitInfo;
			if (buildingInfo.Bib)
			{
				var size = buildingInfo.Dimensions.X;
				var bibOffset = buildingInfo.Dimensions.Y - 1;
				var startIndex = (size == 2) ? SmallBibStart : LargeBibStart;

				for (int i = 0; i < 2 * size; i++)
				{
					var p = self.Location + new int2(i % size, i / size + bibOffset);
					Game.map.MapTiles[p.X, p.Y].smudge = (byte)(i + startIndex);
				}
			}
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			yield return Pair.New(anim.Image, 24f * (float2)self.Location);
		}
	}
}
