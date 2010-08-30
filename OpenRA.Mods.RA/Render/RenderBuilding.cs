#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class RenderBuildingInfo : RenderSimpleInfo
	{
		public readonly bool HasMakeAnimation = true;
		public override object Create(ActorInitializer init) { return new RenderBuilding(init);}
	}

	public class RenderBuilding : RenderSimple, INotifyDamage, INotifySold
	{
		public RenderBuilding( ActorInitializer init )
			: this( init, () => 0 )
		{
		}

		public RenderBuilding( ActorInitializer init, Func<int> baseFacing )
			: base(init.self, baseFacing)
		{
			var self = init.self;
			if( init.Contains<SkipMakeAnimsInit>() || !self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation )
				anim.PlayThen( "idle", () => self.World.AddFrameEndTask( _ => Complete( self ) ) );
			else
				anim.PlayThen( "make", () => self.World.AddFrameEndTask( _ => Complete( self ) ) );
		}

		void Complete( Actor self )
		{
			anim.PlayRepeating( GetPrefix(self) + "idle" );
			foreach( var x in self.TraitsImplementing<INotifyBuildComplete>() )
				x.BuildingComplete( self );
		}

		public void PlayCustomAnimThen(Actor self, string name, Action a)
		{
			anim.PlayThen(GetPrefix(self) + name,
				() => { anim.PlayRepeating(GetPrefix(self) + "idle"); a(); });
		}
		
		public void PlayCustomAnimRepeating(Actor self, string name)
		{
			anim.PlayThen(GetPrefix(self) + name,
				() => { PlayCustomAnimRepeating(self, name); });
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
			
			if (e.DamageState == DamageState.Dead)	
				self.World.AddFrameEndTask(w => w.Add(new Explosion(w, self.CenterLocation.ToInt2(), "building", false)));
			else if (e.DamageState >= DamageState.Heavy && e.PreviousDamageState < DamageState.Heavy)
			{
				anim.ReplaceAnim("damaged-idle");
				Sound.Play(self.Info.Traits.Get<BuildingInfo>().DamagedSound, self.CenterLocation);
			}
			else if (e.DamageState < DamageState.Heavy)
				anim.ReplaceAnim("idle");
		}

		public void Selling( Actor self )
		{
			if( self.Info.Traits.Get<RenderBuildingInfo>().HasMakeAnimation )
				anim.PlayBackwardsThen( "make", null );
			
			foreach (var s in self.Info.Traits.Get<BuildingInfo>().SellSounds)
					Sound.PlayToPlayer(self.Owner, s, self.CenterLocation);
		}

		public void Sold(Actor self) {}
	}
}
