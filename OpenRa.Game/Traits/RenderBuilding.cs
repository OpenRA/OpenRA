using System;
using System.Collections.Generic;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
{
	class RenderBuilding : RenderSimple, INotifyDamage
	{
		const int SmallBibStart = 1;
		const int LargeBibStart = 5;

		public RenderBuilding(Actor self)
			: base(self)
		{
			Make(() => 
			{ 
				anim.PlayRepeating("idle");
			}, self);

			DoBib(self, false);
		}

		protected void Make( Action after, Actor self )
		{
			Action newAfter = () =>
			{
				after();
				foreach (var x in self.traits.WithInterface<INotifyBuildComplete>())
					x.BuildingComplete(self);
			};

			if (Game.skipMakeAnims)
				newAfter();
			else
				anim.PlayThen("make", newAfter);
		}

		void DoBib(Actor self, bool isRemove)
		{
			var buildingInfo = self.traits.Get<Building>().unitInfo;
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
						if (Rules.Map.MapTiles[p.X, p.Y].smudge == (byte)(i + startIndex))
							Rules.Map.MapTiles[ p.X, p.Y ].smudge = 0;
					}
					else
						Rules.Map.MapTiles[p.X, p.Y].smudge = (byte)(i + startIndex);
				}
			}
		}

		public override IEnumerable<Tuple<Sprite, float2, int>> Render(Actor self)
		{
			var pal = self.Owner == null ? 0 : self.Owner.Palette;
			yield return Tuple.New(anim.Image, 24f * (float2)self.Location, pal);
		}

		public virtual void Damaged(Actor self, DamageState state)
		{
			switch( state )
			{
				case DamageState.Normal:
					anim.PlayRepeating("idle");
					break;
				case DamageState.Half:
					anim.PlayRepeating("damaged-idle");
					Sound.Play("kaboom1.aud");
					break;
				case DamageState.Dead:
					DoBib(self, true);
					Game.world.AddFrameEndTask(w => w.Add(new Explosion(self.CenterLocation.ToInt2(), 7, false)));
					break;
			}
		}
	}
}
