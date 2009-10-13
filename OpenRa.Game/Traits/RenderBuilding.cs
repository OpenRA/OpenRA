using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IjwFramework.Types;
using OpenRa.Game.Graphics;
using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class RenderBuilding : RenderSimple
	{
		static Sprite[] largeBib;	
		static Sprite[] smallBib;

		static int2[] largeBibPos = new[] { new int2(0,0), new int2(1,0), new int2(2,0),
											new int2(0,1), new int2(1,1), new int2(2,1) };

		static int2[] smallBibPos = new[] { new int2(0,0), new int2(1,0),
											new int2(0,1), new int2(1,1)};

		public RenderBuilding(Actor self)
			: base(self)
		{
			anim.PlayThen("make", () => anim.PlayRepeating("idle"));
		}

		public static void Prefetch()
		{
			largeBib = SpriteSheetBuilder.LoadAllSprites("bib2.", "tem", "sno", "int");
			smallBib = SpriteSheetBuilder.LoadAllSprites("bib3.", "tem", "sno", "int");
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			if (((UnitInfo.BuildingInfo)self.unitInfo).Bib)
			{
				var fp = Rules.Footprint.GetFootprint(self.unitInfo.Name );
				var bibOffset = new int2(0, fp.Length - 2);
				var hasSmallBib = fp.First().Length == 2;
				
				var bib = hasSmallBib ? smallBib : largeBib;
				var bibPos = hasSmallBib ? smallBibPos : largeBibPos;

				for (int i = 0; i < bib.Length; i++)
					yield return Pair.New(bib[i], 24f * (float2)(self.Location + bibOffset + bibPos[i]));
			}

			yield return Pair.New(anim.Image, 24f * (float2)self.Location);
		}
	}
}
