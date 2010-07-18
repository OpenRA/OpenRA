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
					self.World.AddFrameEndTask(w => w.Add(new Explosion(w, self.CenterLocation.ToInt2(), "building", false)));
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
