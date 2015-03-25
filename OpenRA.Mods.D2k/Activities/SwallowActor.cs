#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2k.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Activities
{
	enum AttackState { Burrowed, EmergingAboveGround, ReturningUnderground }

	class SwallowActor : Activity
	{
		const int NearEnough = 1;

		readonly CPos location;
		readonly Target target;
		readonly Sandworm sandworm;
		readonly WeaponInfo weapon;
		readonly RenderUnit renderUnit;
		readonly RadarPings radarPings;
		readonly AttackSwallow swallow;
		readonly IPositionable positionable;

		int countdown;
		AttackState stance;

		public SwallowActor(Actor self, Target target, WeaponInfo weapon)
		{
			this.target = target;
			this.weapon = weapon;
			sandworm = self.Trait<Sandworm>();
			positionable = self.Trait<Mobile>();
			swallow = self.Trait<AttackSwallow>();
			renderUnit = self.Trait<RenderUnit>();
			radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
			countdown = swallow.Info.AttackTime;

			renderUnit.DefaultAnimation.ReplaceAnim("burrowed");
			stance = AttackState.Burrowed;
			location = target.Actor.Location;
		}

		bool WormAttack(Actor worm)
		{
			var targetLocation = target.Actor.Location;

			// The target has moved too far away
			if ((location - targetLocation).Length > NearEnough)
				return false;

			var lunch = worm.World.ActorMap.GetUnitsAt(targetLocation)
				.Where(t => !t.Equals(worm) && weapon.IsValidAgainst(t, worm));

			if (!lunch.Any())
				return false;

			stance = AttackState.EmergingAboveGround;
			sandworm.IsAttacking = true;

			foreach (var actor in lunch)
			{
				var actor1 = actor;	// loop variable in closure hazard

				actor.World.AddFrameEndTask(_ =>
					{
						actor1.Destroy();

						// Harvester insurance
						if (!actor1.HasTrait<Harvester>())
							return;

						var insurance = actor1.Owner.PlayerActor.TraitOrDefault<HarvesterInsurance>();

						if (insurance != null)
							actor1.World.AddFrameEndTask(__ => insurance.TryActivate());
					});
			}

			positionable.SetPosition(worm, targetLocation);
			foreach (var notify in worm.TraitsImplementing<INotifyAttack>())
				notify.Attacking(worm, target, null, null);
			PlayAttackAnimation(worm);

			var attackPosition = worm.CenterPosition;
			var affectedPlayers = lunch.Select(x => x.Owner).Distinct();
			foreach (var affectedPlayer in affectedPlayers)
				NotifyPlayer(affectedPlayer, attackPosition);

			return true;
		}

		void PlayAttackAnimation(Actor self)
		{
			renderUnit.PlayCustomAnim(self, "mouth");
		}

		void NotifyPlayer(Player player, WPos location)
		{
			Sound.PlayNotification(player.World.Map.Rules, player, "Speech", swallow.Info.WormAttackNotification, player.Country.Race);

			if (player == player.World.RenderPlayer)
				radarPings.Add(() => true, location, Color.Red, 50);
		}

		public override Activity Tick(Actor self)
		{
			if (countdown > 0)
			{
				countdown--;
				return this;
			}

			// Wait for the worm to get back underground
			if (stance == AttackState.ReturningUnderground)
			{
				sandworm.IsAttacking = false;

				// There is a chance that the worm would just go away after attacking
				if (self.World.SharedRandom.Next() % 100 <= sandworm.Info.ChanceToDisappear)
				{
					self.CancelActivity();
					self.World.AddFrameEndTask(w => self.Kill(self));
				}
				else
					renderUnit.DefaultAnimation.ReplaceAnim("idle");

				return NextActivity;
			}

			// Wait for the worm to get in position
			if (stance == AttackState.Burrowed)
			{
				// This is so that the worm cancels an attack against a target that has reached solid rock
				if (!positionable.CanEnterCell(target.Actor.Location, null, false))
					return NextActivity;

				if (!WormAttack(self))
				{
					renderUnit.DefaultAnimation.ReplaceAnim("idle");
					return NextActivity;
				}

				countdown = swallow.Info.ReturnTime;
				stance = AttackState.ReturningUnderground;
			}

			return this;
		}
	}
}
