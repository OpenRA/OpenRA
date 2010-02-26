#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using OpenRa.Effects;

namespace OpenRa.Traits
{
	public class RenderBuildingInfo : RenderSimpleInfo
	{
		public readonly bool HasMakeAnimation = true;
		public override object Create(Actor self) { return new RenderBuilding(self);}
	}

	public class RenderBuilding : RenderSimple, INotifyDamage, INotifySold
	{
		static readonly int[] bibStarts = { 0, 0, 1, 5, 11 };

		public RenderBuilding( Actor self )
			: this( self, () => 0 )
		{
		}

		public RenderBuilding(Actor self, Func<int> baseFacing)
			: base(self, baseFacing)
		{		
			if( Game.skipMakeAnims || !self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation )
				Complete( self );
			else
				anim.PlayThen( "make", () => self.World.AddFrameEndTask( _ => Complete( self ) ) );

			DoBib(self, false);
		}

		void Complete( Actor self )
		{
			anim.PlayRepeating( GetPrefix(self) + "idle" );
			foreach( var x in self.traits.WithInterface<INotifyBuildComplete>() )
				x.BuildingComplete( self );
		}

		void DoBib(Actor self, bool isRemove)
		{
			var buildingInfo = self.Info.Traits.Get<BuildingInfo>();
			if (buildingInfo.Bib)
			{
				var size = buildingInfo.Dimensions.X;
				var bibOffset = buildingInfo.Dimensions.Y - 1;
				var startIndex = bibStarts[size];

				for (int i = 0; i < 2 * size; i++)
				{
					var p = self.Location + new int2(i % size, i / size + bibOffset);
					if (isRemove)
					{
						if (self.World.Map.MapTiles[p.X, p.Y].smudge == (byte)(i + startIndex))
							self.World.Map.MapTiles[ p.X, p.Y ].smudge = 0;
					}
					else
						self.World.Map.MapTiles[p.X, p.Y].smudge = (byte)(i + startIndex);
				}
			}
		}

		protected string GetPrefix(Actor self)
		{
			return self.GetDamageState() == DamageState.Half ? "damaged-" : "";
		}

		public void PlayCustomAnim(Actor self, string name)
		{
			anim.PlayThen(GetPrefix(self) + name, 
				() => anim.PlayRepeating(GetPrefix(self) + "idle"));
		}

		public void PlayCustomAnimThen(Actor self, string name, Action a)
		{
			anim.PlayThen(GetPrefix(self) + name,
				() => { anim.PlayRepeating(GetPrefix(self) + "idle"); a(); });
		}

		public void PlayCustomAnimBackwards(Actor self, string name, Action a)
		{
			anim.PlayBackwardsThen(GetPrefix(self) + name,
				() => { anim.PlayRepeating(GetPrefix(self) + "idle"); a(); });
		}

		public virtual void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged)
				return;

			switch( e.DamageState )
			{
				case DamageState.Normal:
					anim.ReplaceAnim("idle");
					break;
				case DamageState.Half:
					anim.ReplaceAnim("damaged-idle");
					Sound.Play("kaboom1.aud");
					break;
				case DamageState.Dead:
					DoBib(self, true);
					self.World.AddFrameEndTask(w => w.Add(new Explosion(w, self.CenterLocation.ToInt2(), 7, false)));
					break;
			}
		}

		public void Selling( Actor self )
		{
			if( !Game.skipMakeAnims && self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation )
				anim.PlayBackwardsThen( "make", null );
			
			Sound.PlayToPlayer(self.Owner, "cashturn.aud");
		}

		public void Sold(Actor self) { DoBib(self, true); }
	}
}
