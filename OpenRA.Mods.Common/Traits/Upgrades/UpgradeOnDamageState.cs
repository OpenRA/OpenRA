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
	public class UpgradeOnDamageStateInfo : ITraitInfo, Requires<UpgradeManagerInfo>, Requires<HealthInfo>
	{
		[UpgradeGrantedReference, FieldLoader.Require]
		[Desc("The upgrades to grant.")]
		public readonly string[] Upgrades = { };

		[Desc("Play a random sound from this list when enabled.")]
		public readonly string[] EnabledSounds = { };

		[Desc("Play a random sound from this list when disabled.")]
		public readonly string[] DisabledSounds = { };

		[Desc("Levels of damage at which to grant upgrades.")]
		public readonly DamageState ValidDamageStates = DamageState.Heavy | DamageState.Critical;

		[Desc("Are upgrades irrevocable once the conditions have been met?")]
		public readonly bool GrantPermanently = false;

		public object Create(ActorInitializer init) { return new UpgradeOnDamageState(init.Self, this); }
	}

	public class UpgradeOnDamageState : INotifyDamageStateChanged, INotifyCreated
	{
		readonly UpgradeOnDamageStateInfo info;
		readonly UpgradeManager um;
		readonly Health health;
		bool granted;

		public UpgradeOnDamageState(Actor self, UpgradeOnDamageStateInfo info)
		{
			this.info = info;
			um = self.Trait<UpgradeManager>();
			health = self.Trait<Health>();
		}

		void INotifyCreated.Created(Actor self)
		{
			GrantUpgradeOnValidDamageState(self, health.DamageState);
		}

		void GrantUpgradeOnValidDamageState(Actor self, DamageState state)
		{
			if (!info.ValidDamageStates.HasFlag(state))
				return;

			granted = true;
			var rand = Game.CosmeticRandom;
			var sound = info.EnabledSounds.RandomOrDefault(rand);
			Game.Sound.Play(sound, self.CenterPosition);
			foreach (var u in info.Upgrades)
				um.GrantUpgrade(self, u, this);
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (granted && info.GrantPermanently)
				return;

			if (!granted && !info.ValidDamageStates.HasFlag(e.PreviousDamageState))
				GrantUpgradeOnValidDamageState(self, health.DamageState);
			else if (granted && !info.ValidDamageStates.HasFlag(e.DamageState) && info.ValidDamageStates.HasFlag(e.PreviousDamageState))
			{
				granted = false;
				var rand = Game.CosmeticRandom;
				var sound = info.DisabledSounds.RandomOrDefault(rand);
				Game.Sound.Play(sound, self.CenterPosition);
				foreach (var u in info.Upgrades)
					um.RevokeUpgrade(self, u, this);
			}
		}
	}
}
