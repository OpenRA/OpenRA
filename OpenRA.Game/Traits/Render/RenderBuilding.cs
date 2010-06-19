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
using OpenRA.Effects;

namespace OpenRA.Traits
{
	public class RenderBuildingInfo : RenderSimpleInfo
	{
		public readonly bool HasMakeAnimation = true;
		public override object Create(ActorInitializer init) { return new RenderBuilding(init.self);}
	}

	public class RenderBuilding : RenderSimple, INotifyDamage, INotifySold
	{
		public RenderBuilding( Actor self )
			: this( self, () => 0 )
		{
		}

		public RenderBuilding(Actor self, Func<int> baseFacing)
			: base(self, baseFacing)
		{		
			if( Game.skipMakeAnims || !self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation )
				anim.PlayThen( "idle", () => self.World.AddFrameEndTask( _ => Complete( self ) ) );
			else
				anim.PlayThen( "make", () => self.World.AddFrameEndTask( _ => Complete( self ) ) );
		}

		void Complete( Actor self )
		{
			anim.PlayRepeating( GetPrefix(self) + "idle" );
			foreach( var x in self.traits.WithInterface<INotifyBuildComplete>() )
				x.BuildingComplete( self );
		}

		protected string GetPrefix(Actor self)
		{
			return self.GetDamageState() == DamageState.Half ? "damaged-" : "";
		}

		public void PlayCustomAnim(Actor self, string name)
		{
			if (anim.HasSequence(name))
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
			var hasSequence = anim.HasSequence(GetPrefix(self) + name);
			anim.PlayBackwardsThen(hasSequence ? GetPrefix(self) + name : name,
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
					Sound.Play(self.Info.Traits.Get<BuildingInfo>().DamagedSound, self.CenterLocation);
					break;
				case DamageState.Dead:
					self.World.AddFrameEndTask(w => w.Add(new Explosion(w, self.CenterLocation.ToInt2(), 7, false)));
					break;
			}
		}

		public void Selling( Actor self )
		{
			if( !Game.skipMakeAnims && self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation )
				anim.PlayBackwardsThen( "make", null );
			
			foreach (var s in self.Info.Traits.Get<BuildingInfo>().SellSounds)
					Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);
		}

		public void Sold(Actor self) {}
	}
}
