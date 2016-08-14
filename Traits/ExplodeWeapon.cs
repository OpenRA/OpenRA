#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Explodes a weapon at the actor's position when enabled."
		+ "Reload/burstdelays are used as explosion intervals.")]
	public class ExplodeWeaponInfo : UpgradableTraitInfo, IRulesetLoaded
	{
		[WeaponReference, FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		public readonly bool ResetReloadWhenEnabled = true;

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new ExplodeWeapon(init.Self, this); }

		void IRulesetLoaded.RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			WeaponInfo weaponInfo;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weaponInfo))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			WeaponInfo = weaponInfo;
		}
	}

	class ExplodeWeapon : UpgradableTrait<ExplodeWeaponInfo>, ITick
	{
		readonly Actor self;
		readonly ExplodeWeaponInfo info;
		readonly WeaponInfo weapon;

		int fireDelay;
		int burst;

		public ExplodeWeapon(Actor self, ExplodeWeaponInfo info)
			: base(info)
		{
			this.self = self;
			this.info = info;

			weapon = info.WeaponInfo;
			burst = weapon.Burst;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--fireDelay < 0)
			{
				weapon.Impact(Target.FromPos(self.CenterPosition), self,
					self.TraitsImplementing<IFirepowerModifier>().Select(a => a.GetFirepowerModifier()).ToArray());

				if (--burst > 0)
					fireDelay = weapon.BurstDelay;
				else
				{
					var modifiers = self.TraitsImplementing<IReloadModifier>()
						.Select(m => m.GetReloadModifier());
					fireDelay = Util.ApplyPercentageModifiers(weapon.ReloadDelay, modifiers);
					burst = weapon.Burst;
				}
			}
		}

		protected override void UpgradeEnabled(Actor self)
		{
			if (info.ResetReloadWhenEnabled)
			{
				burst = weapon.Burst;
				fireDelay = 0;
			}
		}
	}
}
