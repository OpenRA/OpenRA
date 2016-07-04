#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2k.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Activities
{
	enum AttackState { Uninitialized, Burrowed, Attacking }

	class SwallowActor : Activity
	{
		const int NearEnough = 1;

		readonly Target target;
		readonly Sandworm sandworm;
		readonly UpgradeManager manager;
		readonly WeaponInfo weapon;
		readonly RadarPings radarPings;
		readonly AttackSwallow swallow;
		readonly IPositionable positionable;

		int countdown;
		CPos burrowLocation;
		AttackState stance;

		public SwallowActor(Actor self, Target target, WeaponInfo weapon)
		{
			this.target = target;
			this.weapon = weapon;
			sandworm = self.Trait<Sandworm>();
			positionable = self.Trait<Mobile>();
			swallow = self.Trait<AttackSwallow>();
			manager = self.Trait<UpgradeManager>();
			radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
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
			Game.Sound.Play(swallow.Info.WormAttackSound, self.CenterPosition);

			Game.RunAfterDelay(1000, () =>
			{
				if (!Game.IsCurrentWorld(self.World))
					return;

				foreach (var player in affectedPlayers)
				{
					Game.Sound.PlayNotification(player.World.Map.Rules, player, "Speech", swallow.Info.WormAttackNotification, player.Faction.InternalName);

					if (player == player.World.RenderPlayer)
						radarPings.Add(() => true, attackPosition, Color.Red, 50);
				}
			});

			foreach (var notify in self.TraitsImplementing<INotifyAttack>())
			{
				notify.PreparingAttack(self, target, null, null);
				notify.Attacking(self, target, null, null);
			}

			return true;
		}

		public override Activity Tick(Actor self)
		{
			switch (stance)
			{
				case AttackState.Uninitialized:
					GrantUpgrades(self);
					stance = AttackState.Burrowed;
					countdown = swallow.Info.AttackDelay;
					burrowLocation = self.Location;
					break;
				case AttackState.Burrowed:
					if (--countdown > 0)
						return this;

					var targetLocation = target.Actor.Location;

					// The target has moved too far away
					if ((burrowLocation - targetLocation).Length > NearEnough)
					{
						RevokeUpgrades(self);
						return NextActivity;
					}

					// The target reached solid ground
					if (!positionable.CanEnterCell(targetLocation, null, false))
					{
						RevokeUpgrades(self);
						return NextActivity;
					}

					var targets = self.World.ActorMap.GetActorsAt(targetLocation)
						.Where(t => !t.Equals(self) && weapon.IsValidAgainst(t, self));

					if (!targets.Any())
					{
						RevokeUpgrades(self);
						return NextActivity;
					}

					stance = AttackState.Attacking;
					countdown = swallow.Info.ReturnDelay;
					sandworm.IsAttacking = true;
					AttackTargets(self, targets);

					break;
				case AttackState.Attacking:
					if (--countdown > 0)
						return this;

					sandworm.IsAttacking = false;

					// There is a chance that the worm would just go away after attacking
					if (self.World.SharedRandom.Next(100) <= sandworm.WormInfo.ChanceToDisappear)
					{
						self.CancelActivity();
						self.World.AddFrameEndTask(w => self.Dispose());
					}

					RevokeUpgrades(self);
					return NextActivity;
			}

			return this;
		}

		void GrantUpgrades(Actor self)
		{
			foreach (var up in swallow.Info.AttackingUpgrades)
				manager.GrantUpgrade(self, up, this);
		}

		void RevokeUpgrades(Actor self)
		{
			foreach (var up in swallow.Info.AttackingUpgrades)
				manager.RevokeUpgrade(self, up, this);
		}
	}
}
