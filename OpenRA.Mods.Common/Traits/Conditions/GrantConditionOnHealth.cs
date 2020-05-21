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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Applies a condition to the actor at when its health is between 2 specific values.")]
	public class GrantConditionOnHealthInfo : TraitInfo, IRulesetLoaded, Requires<IHealthInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		[Desc("Play a random sound from this list when enabled.")]
		public readonly string[] EnabledSounds = { };

		[Desc("Play a random sound from this list when disabled.")]
		public readonly string[] DisabledSounds = { };

		[Desc("Minimum level of health at which to grant the condition.")]
		public readonly int MinHP = 0;

		[Desc("Maximum level of health at which to grant the condition.",
			"Non-positive values will make it use Health.HP.")]
		public readonly int MaxHP = 0;

		[Desc("Is the condition irrevokable once it has been granted?")]
		public readonly bool GrantPermanently = false;

		public override object Create(ActorInitializer init) { return new GrantConditionOnHealth(init.Self, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var health = ai.TraitInfo<IHealthInfo>();
			if (health.MaxHP < MinHP)
				throw new YamlException("Minimum HP ({0}) for GrantConditionOnHealth can't be more than actor's Maximum HP ({1})".F(MinHP, health.MaxHP));
		}
	}

	public class GrantConditionOnHealth : INotifyCreated, INotifyDamage
	{
		readonly GrantConditionOnHealthInfo info;
		readonly IHealth health;
		readonly int maxHP;

		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnHealth(Actor self, GrantConditionOnHealthInfo info)
		{
			this.info = info;
			health = self.Trait<IHealth>();
			maxHP = info.MaxHP > 0 ? info.MaxHP : health.MaxHP;
		}

		void INotifyCreated.Created(Actor self)
		{
			GrantConditionOnValidHealth(self, health.HP);
		}

		void GrantConditionOnValidHealth(Actor self, int hp)
		{
			if (info.MinHP > hp || maxHP < hp || conditionToken != Actor.InvalidConditionToken)
				return;

			conditionToken = self.GrantCondition(info.Condition);

			var sound = info.EnabledSounds.RandomOrDefault(Game.CosmeticRandom);
			Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			var granted = conditionToken != Actor.InvalidConditionToken;
			if (granted && info.GrantPermanently)
				return;

			if (!granted)
				GrantConditionOnValidHealth(self, health.HP);
			else if (granted && (info.MinHP > health.HP || maxHP < health.HP))
			{
				conditionToken = self.RevokeCondition(conditionToken);

				var sound = info.DisabledSounds.RandomOrDefault(Game.CosmeticRandom);
				Game.Sound.Play(SoundType.World, sound, self.CenterPosition);
			}
		}
	}
}
