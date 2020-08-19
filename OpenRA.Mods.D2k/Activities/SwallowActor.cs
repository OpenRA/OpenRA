#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2k.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Activities
{
	enum AttackState { Uninitialized, Burrowed, Attacking }

	class SwallowActor : Activity
	{
		const int NearEnough = 1;

		readonly Target target;
		readonly Sandworm sandworm;
		readonly WeaponInfo weapon;
		readonly Armament armament;
		readonly AttackSwallow swallow;
		readonly IPositionable positionable;
		readonly IFacing facing;

		int countdown;
		CPos burrowLocation;
		AttackState stance;
		int attackingToken = Actor.InvalidConditionToken;

		public SwallowActor(Actor self, in Target target, Armament a, IFacing facing)
		{
			this.target = target;
			this.facing = facing;
			armament = a;
			weapon = a.Weapon;
			sandworm = self.Trait<Sandworm>();
			positionable = self.Trait<Mobile>();
			swallow = self.Trait<AttackSwallow>();
		}

		bool AttackTargets(Actor self, IEnumerable<Actor> targets)
		{
			var targetLocation = target.Actor.Location;
			foreach (var t in targets)
			{
				var targetClose = t; // loop variable in closure hazard

				self.World.AddFrameEndTask(_ =>
				{
					// Don't use Kill() because we don't want any of its side-effects (husks, etc)
					targetClose.Dispose();

					// Harvester insurance
					if (targetClose.Info.HasTraitInfo<HarvesterInfo>())
					{
						var insurance = targetClose.Owner.PlayerActor.TraitOrDefault<HarvesterInsurance>();
						if (insurance != null)
							self.World.AddFrameEndTask(__ => insurance.TryActivate());
					}
				});
			}

			positionable.SetPosition(self, targetLocation);

			var attackPosition = self.CenterPosition;
			var affectedPlayers = targets.Select(x => x.Owner).Distinct().ToList();
			Game.Sound.Play(SoundType.World, swallow.Info.WormAttackSound, self.CenterPosition);

			foreach (var player in affectedPlayers)
				self.World.AddFrameEndTask(w => w.Add(new MapNotificationEffect(player, "Speech", swallow.Info.WormAttackNotification, 25, true, attackPosition, Color.Red)));

			var barrel = armament.CheckFire(self, facing, target);
			if (barrel == null)
				return false;

			// armament.CheckFire already calls INotifyAttack.PreparingAttack
			foreach (var notify in self.TraitsImplementing<INotifyAttack>())
				notify.Attacking(self, target, armament, barrel);

			return true;
		}

		public override bool Tick(Actor self)
		{
			switch (stance)
			{
				case AttackState.Uninitialized:
					stance = AttackState.Burrowed;
					countdown = swallow.Info.AttackDelay;
					burrowLocation = self.Location;
					if (attackingToken == Actor.InvalidConditionToken)
						attackingToken = self.GrantCondition(swallow.Info.AttackingCondition);

					break;
				case AttackState.Burrowed:
					if (--countdown > 0)
						return false;

					var targetLocation = target.Actor.Location;

					// The target has moved too far away
					if ((burrowLocation - targetLocation).Length > NearEnough)
					{
						RevokeCondition(self);
						return true;
					}

					// The target reached solid ground
					if (!positionable.CanEnterCell(targetLocation, null, BlockedByActor.None))
					{
						RevokeCondition(self);
						return true;
					}

					var targets = self.World.ActorMap.GetActorsAt(targetLocation)
						.Where(t => !t.Equals(self) && weapon.IsValidAgainst(t, self));

					if (!targets.Any())
					{
						RevokeCondition(self);
						return true;
					}

					stance = AttackState.Attacking;
					countdown = swallow.Info.ReturnDelay;
					sandworm.IsAttacking = true;
					AttackTargets(self, targets);

					break;
				case AttackState.Attacking:
					if (--countdown > 0)
						return false;

					sandworm.IsAttacking = false;

					// There is a chance that the worm would just go away after attacking
					if (self.World.SharedRandom.Next(100) <= sandworm.WormInfo.ChanceToDisappear)
					{
						self.CancelActivity();
						self.World.AddFrameEndTask(w => self.Dispose());
					}

					RevokeCondition(self);
					return true;
			}

			return false;
		}

		void RevokeCondition(Actor self)
		{
			if (attackingToken != Actor.InvalidConditionToken)
				attackingToken = self.RevokeCondition(attackingToken);
		}
	}
}
