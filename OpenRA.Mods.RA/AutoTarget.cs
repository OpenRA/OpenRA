#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using System.Drawing;
using System.Linq;

namespace OpenRA.Mods.RA
{
	public class AutoTargetInfo : ITraitInfo, Requires<AttackBaseInfo>
	{
		public readonly bool AllowMovement = true;
		public readonly int ScanRadius = -1;
		public readonly UnitStance InitialStance = UnitStance.AttackAnything;

		public object Create(ActorInitializer init) { return new AutoTarget(init.self, this); }
	}

	public enum UnitStance { HoldFire, ReturnFire, AttackAnything };

	public class AutoTarget : INotifyIdle, INotifyDamage, ITick, IResolveOrder
	{
		readonly AutoTargetInfo Info;
		readonly AttackBase attack;

		[Sync] int nextScanTime = 0;
		[Sync] public UnitStance stance;
		public UnitStance predictedStance;		/* NOT SYNCED: do not refer to this anywhere other than UI code */

		public AutoTarget(Actor self, AutoTargetInfo info)
		{
			Info = info;
			attack = self.Trait<AttackBase>();
			stance = Info.InitialStance;
			predictedStance = stance;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "SetUnitStance")
				stance = (UnitStance)order.TargetLocation.X;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!self.IsIdle) return;
			if (e.Attacker.Destroyed) return;

			if (stance < UnitStance.ReturnFire) return;

			// not a lot we can do about things we can't hurt... although maybe we should automatically run away?
			var attack = self.Trait<AttackBase>();
			if (!attack.HasAnyValidWeapons(Target.FromActor(e.Attacker))) return;

			// don't retaliate against own units force-firing on us. it's usually not what the player wanted.
			if (e.Attacker.AppearsFriendlyTo(self)) return;

			if (e.Damage < 0) return;	// don't retaliate against healers

			attack.AttackTarget(Target.FromActor(e.Attacker), false, Info.AllowMovement);
		}

		public void TickIdle(Actor self)
		{
			if (stance < UnitStance.AttackAnything) return;

			var target = ScanForTarget(self, null);
			if (target != null)
			{
				self.SetTargetLine(Target.FromActor(target), Color.Red, false);
				self.QueueActivity(attack.GetAttackActivity(self,
					Target.FromActor(target),
					Info.AllowMovement));
			}
		}

		public void Tick(Actor self)
		{
			--nextScanTime;
		}

		public Actor ScanForTarget(Actor self, Actor currentTarget)
		{
			var range = Info.ScanRadius > 0 ? Info.ScanRadius : attack.GetMaximumRange();

			if (self.IsIdle || currentTarget == null || !Combat.IsInRange(self.CenterLocation, range, currentTarget))
				if(nextScanTime <= 0)
					return ChooseTarget(self, range);

			return currentTarget;
		}

		public void ScanAndAttack(Actor self, bool allowMovement, bool holdStill)
		{
			var targetActor = ScanForTarget(self, null);
			if (targetActor != null)
				attack.AttackTarget(Target.FromActor(targetActor), false, allowMovement && !holdStill);
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var info = self.Info.Traits.Get<AttackBaseInfo>();
			nextScanTime = (int)(25 * (info.ScanTimeAverage +
				(self.World.SharedRandom.NextDouble() * 2 - 1) * info.ScanTimeSpread));

			var inRange = self.World.FindUnitsInCircle(self.CenterLocation, (int)(Game.CellSize * range));

			return inRange
				.Where(a => a.AppearsHostileTo(self))
				.Where(a => !a.HasTrait<AutoTargetIgnore>())
				.Where(a => attack.HasAnyValidWeapons(Target.FromActor(a)))
				.ClosestTo( self.CenterLocation );
		}
	}

	class AutoTargetIgnoreInfo : TraitInfo<AutoTargetIgnore> { }
	class AutoTargetIgnore { }
}
