#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Target damaged actors automatically.",
		"Can be used together with AttackMedic to make the healer do it's job automatically to nearby units.")]
	class AutoTargetDamagedInfo : ITraitInfo, Requires<AttackBaseInfo>
	{
		public readonly Stance TargetPlayers = Stance.Ally | Stance.Player;
		public readonly string Type = "heal";

		public object Create(ActorInitializer init) { return new AutoTargetDamaged(this); }
	}

	class AutoTargetDamaged : INotifyIdle, INotifyCreated
	{
		readonly AutoTargetDamagedInfo info;
		AttackBase attack;

		public AutoTargetDamaged(AutoTargetDamagedInfo info) { this.info = info; }

		public void Created(Actor self)
		{
			attack = self.Trait<AttackBase>();
		}

		public Actor PrescanForTarget(Actor self, Actor currentTarget)
		{
			if (currentTarget != null && currentTarget.IsInWorld && !currentTarget.IsDead)
				return currentTarget;

			var inRange = self.World.FindActorsInCircle(self.CenterPosition, attack.GetMaximumRange());

			return inRange.Where(a =>
					a != self && a.IsInWorld && !a.IsDead
					&& !a.Info.Traits.WithInterface<AutoTargetIgnoreInfo>().Any(t => t.Type == info.Type)
					&& a.HasApparentDiplomacy(self, info.TargetPlayers)
					&& a.GetDamageState() > DamageState.Undamaged
					&& attack.HasAnyValidWeapons(Target.FromActor(a)))
				.ClosestTo(self);
		}

		public void TickIdle(Actor self)
		{
			var target = PrescanForTarget(self, null);
			if (target != null)
				self.QueueActivity(attack.GetAttackActivity(self, Target.FromActor(target), false));
		}
	}
}
