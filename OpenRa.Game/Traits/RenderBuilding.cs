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
	class RenderBuilding : RenderSimple, INotifyDamage
	{
		const int SmallBibStart = 1;
		const int LargeBibStart = 5;

		public RenderBuilding(Actor self)
			: base(self)
		{
			if (self.IsMapActor)
				anim.PlayRepeating("idle");
			else
				anim.PlayThen("make", () => anim.PlayRepeating("idle"));

			DoBib(self, false);
		}

		void DoBib(Actor self, bool isRemove)
		{
			var buildingInfo = (UnitInfo.BuildingInfo)self.unitInfo;
			if (buildingInfo.Bib)
			{
				var size = buildingInfo.Dimensions.X;
				var bibOffset = buildingInfo.Dimensions.Y - 1;
				var startIndex = (size == 2) ? SmallBibStart : LargeBibStart;

				for (int i = 0; i < 2 * size; i++)
				{
					var p = self.Location + new int2(i % size, i / size + bibOffset);
					if (isRemove)
					{
						if (Game.map.MapTiles[p.X, p.Y].smudge == (byte)(i + startIndex))
							Game.map.MapTiles[p.X, p.Y].smudge = 0;
					}
					else
						Game.map.MapTiles[p.X, p.Y].smudge = (byte)(i + startIndex);
				}
			}
		}

		public override IEnumerable<Pair<Sprite, float2>> Render(Actor self)
		{
			yield return Pair.New(anim.Image, 24f * (float2)self.Location);
		}

		public void Damaged(Actor self, DamageState state)
		{
			switch( state )
			{
				case DamageState.Normal:
					anim.PlayRepeating("idle");	/* todo: make interaction?? this should only get called on half->ok */
					break;
				case DamageState.Half:
					anim.PlayRepeating("damaged-idle");
					Game.PlaySound("kaboom1.aud", false);		/* todo: maybe sep. sound stuff from visual ?? */
					break;
				case DamageState.Dead:
					DoBib(self, true);
					break;
			}
		}
	}
}
