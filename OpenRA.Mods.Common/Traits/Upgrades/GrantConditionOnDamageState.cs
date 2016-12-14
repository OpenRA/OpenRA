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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Applies an upgrade to the actor at specified damage states.")]
	public class GrantConditionOnDamageStateInfo : ITraitInfo, Requires<HealthInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Play a random sound from this list when enabled.")]
		public readonly string[] EnabledSounds = { };

		[Desc("Play a random sound from this list when disabled.")]
		public readonly string[] DisabledSounds = { };

		[Desc("Levels of damage at which to grant upgrades.")]
		public readonly DamageState ValidDamageStates = DamageState.Heavy | DamageState.Critical;

		[Desc("Is the condition irrevocable once it has been activated?")]
		public readonly bool GrantPermanently = false;

		public object Create(ActorInitializer init) { return new GrantConditionOnDamageState(init.Self, this); }
	}

	public class GrantConditionOnDamageState : INotifyDamageStateChanged, INotifyCreated
	{
		readonly GrantConditionOnDamageStateInfo info;
		readonly Health health;

		UpgradeManager manager;
		int conditionToken = UpgradeManager.InvalidConditionToken;

		public GrantConditionOnDamageState(Actor self, GrantConditionOnDamageStateInfo info)
		{
			this.info = info;
			health = self.Trait<Health>();
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.TraitOrDefault<UpgradeManager>();
			GrantUpgradeOnValidDamageState(self, health.DamageState);
		}

		void GrantUpgradeOnValidDamageState(Actor self, DamageState state)
		{
			if (!info.ValidDamageStates.HasFlag(state) || conditionToken != UpgradeManager.InvalidConditionToken)
				return;

			conditionToken = manager.GrantCondition(self, info.Condition);

			var sound = info.EnabledSounds.RandomOrDefault(Game.CosmeticRandom);
			Game.Sound.Play(sound, self.CenterPosition);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			var granted = conditionToken != UpgradeManager.InvalidConditionToken;
			if ((granted && info.GrantPermanently) || manager == null)
				return;

			if (!granted && !info.ValidDamageStates.HasFlag(e.PreviousDamageState))
				GrantUpgradeOnValidDamageState(self, health.DamageState);
			else if (granted && !info.ValidDamageStates.HasFlag(e.DamageState) && info.ValidDamageStates.HasFlag(e.PreviousDamageState))
			{
				conditionToken = manager.RevokeCondition(self, conditionToken);

				var sound = info.DisabledSounds.RandomOrDefault(Game.CosmeticRandom);
				Game.Sound.Play(sound, self.CenterPosition);
			}
		}
	}
}
