#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	enum AttackState { Burrowed, EmergingAboveGround, ReturningUnderground }

	class SwallowActor : Activity
	{
		const int NearEnough = 1;

		readonly CPos location;
		readonly Target target;
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

			foreach (var actor in lunch)
				actor.World.AddFrameEndTask(_ => actor.Destroy());

			positionable.SetPosition(worm, targetLocation);
			PlayAttackAnimation(worm);

			var attackPosition = worm.CenterPosition;
			var affectedPlayers = lunch.Select(x => x.Owner).Distinct();
			foreach (var affectedPlayer in affectedPlayers)
				NotifyPlayer(affectedPlayer, attackPosition);

			return true;
		}

		void PlayAttackAnimation(Actor self)
		{
			renderUnit.PlayCustomAnim(self, "sand");
			renderUnit.PlayCustomAnim(self, "mouth");
		}

		void NotifyPlayer(Player player, WPos location)
		{
			Sound.PlayNotification(player.World.Map.Rules, player, "Speech", swallow.Info.WormAttackNotification, player.Country.Race);
			radarPings.Add(() => true, location, Color.Red, 50);
		}

		public override Activity Tick(Actor self)
		{
			if (countdown > 0)
			{
				countdown--;
				return this;
			}

			if (stance == AttackState.ReturningUnderground)     // Wait for the worm to get back underground
			{
				if (self.World.SharedRandom.Next() % 2 == 0)	// There is a 50-50 chance that the worm would just go away
				{
					self.CancelActivity();
					self.World.AddFrameEndTask(w => w.Remove(self));
					var wormManager = self.World.WorldActor.TraitOrDefault<WormManager>();
					if (wormManager != null)
						wormManager.DecreaseWorms();
				}
				else
				{
					renderUnit.DefaultAnimation.ReplaceAnim("idle");
				}
				return NextActivity;
			}

			if (stance == AttackState.Burrowed)   // Wait for the worm to get in position
			{
				// This is so that the worm cancels an attack against a target that has reached solid rock
				if (!positionable.CanEnterCell(target.Actor.Location, null, false))
					return NextActivity;

				var success = WormAttack(self);
				if (!success)
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
