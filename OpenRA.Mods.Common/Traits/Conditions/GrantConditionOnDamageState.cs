#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	[Desc("Applies a condition to the actor at specified damage states.")]
	public class GrantConditionOnDamageStateInfo : ITraitInfo, Requires<IHealthInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Play a random sound from this list when enabled.")]
		public readonly string[] EnabledSounds = { };

		[Desc("Play a random sound from this list when disabled.")]
		public readonly string[] DisabledSounds = { };

		[Desc("Levels of damage at which to grant the condition.")]
		public readonly DamageState ValidDamageStates = DamageState.Heavy | DamageState.Critical;

		[Desc("Is the condition irrevocable once it has been activated?")]
		public readonly bool GrantPermanently = false;

		public object Create(ActorInitializer init) { return new GrantConditionOnDamageState(init.Self, this); }
	}

	public class GrantConditionOnDamageState : INotifyDamageStateChanged, INotifyCreated
	{
		readonly GrantConditionOnDamageStateInfo info;
		readonly IHealth health;

		ConditionManager conditionManager;
		int conditionToken = ConditionManager.InvalidConditionToken;

		public GrantConditionOnDamageState(Actor self, GrantConditionOnDamageStateInfo info)
		{
			this.info = info;
			health = self.Trait<IHealth>();
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();
			GrantConditionOnValidDamageState(self, health.DamageState);
		}

		void GrantConditionOnValidDamageState(Actor self, DamageState state)
		{
			if (!info.ValidDamageStates.HasFlag(state) || conditionToken != ConditionManager.InvalidConditionToken)
				return;

			conditionToken = conditionManager.GrantCondition(self, info.Condition);

			var sound = info.EnabledSounds.RandomOrDefault(Game.CosmeticRandom);
			Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			var granted = conditionToken != ConditionManager.InvalidConditionToken;
			if (granted && info.GrantPermanently)
				return;

			if (!granted && !info.ValidDamageStates.HasFlag(e.PreviousDamageState))
				GrantConditionOnValidDamageState(self, health.DamageState);
			else if (granted && !info.ValidDamageStates.HasFlag(e.DamageState) && info.ValidDamageStates.HasFlag(e.PreviousDamageState))
			{
				conditionToken = conditionManager.RevokeCondition(self, conditionToken);

				var sound = info.DisabledSounds.RandomOrDefault(Game.CosmeticRandom);
				Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
			}
		}
	}
}
