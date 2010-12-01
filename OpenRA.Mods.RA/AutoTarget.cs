#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Traits.Activities;
using System.Drawing;

namespace OpenRA.Mods.RA
{
	class AutoTargetInfo : TraitInfo<AutoTarget>
	{
		public readonly bool AllowMovement = true;
	}

	class AutoTarget : INotifyIdle, INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			if (!self.IsIdle) return;
			if (e.Attacker.Destroyed) return;
			
			// not a lot we can do about things we can't hurt... although maybe we should automatically run away?
			var attack = self.Trait<AttackBase>();
			if (!attack.HasAnyValidWeapons(Target.FromActor(e.Attacker))) return;

			// don't retaliate against own units force-firing on us. it's usually not what the player wanted.
			if (self.Owner.Stances[e.Attacker.Owner] == Stance.Ally) return;

			if (e.Damage < 0) return;	// don't retaliate against healers

			self.Trait<AttackBase>().AttackTarget(Target.FromActor(e.Attacker), false, self.Info.Traits.Get<AutoTargetInfo>().AllowMovement);
		}

		public void TickIdle( Actor self )
		{
			var attack = self.Trait<AttackBase>();
			var target = attack.ScanForTarget(self, null);
			if( target != null )
			{
				self.SetTargetLine(Target.FromActor(target), Color.Red, false);
				self.QueueActivity(attack.GetAttackActivity( self,
				                                            Target.FromActor(target),
				                                            self.Info.Traits.Get<AutoTargetInfo>().AllowMovement
				                                            ));
			}
		}
	}
}
