#region Copyright & License Information
/*
 * Almost identical to DamageByTerrain trait by OpenRA devs.
 * Modded by Boolbada of OP Mod.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.yupgi_alert.Traits
{
	[Desc("This actor receives damage from the given weapon when in radioactive area.")]
	class DamagedByRadioactivityInfo : ConditionalTraitInfo, Requires<HealthInfo>
	{
		[Desc("Damage received per radioactivity level, in per mille, per DamageInterval. (Damage = DamageCoeff * RadioactivityLevel / 1000")]
		// Considering that 1% of level 500 is 5, it is quite tough to have percent. We use per mille here.
		// 5 damage is much larger than Mods.cnc's tiberium damage.
		[FieldLoader.Require] public readonly int DamageCoeff = 0;

		[Desc("Delay (in ticks) between receiving damage.")]
		public readonly int DamageInterval = 16;

		[Desc("Apply the damage using these damagetypes.")]
		public readonly HashSet<string> DamageTypes = new HashSet<string>();

		public override object Create(ActorInitializer init) { return new DamagedByRadioactivity(init.Self, this); }
	}

	class DamagedByRadioactivity : ConditionalTrait<DamagedByRadioactivityInfo>, ITick, ISync
	{
		readonly Health health;
		readonly RadioactivityLayer raLayer;

		[Sync] int damageTicks;

		public DamagedByRadioactivity(Actor self, DamagedByRadioactivityInfo info) : base(info)
		{
			health = self.Trait<Health>();
			raLayer = self.World.WorldActor.Trait<RadioactivityLayer>();
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled || --damageTicks > 0)
				return;

			// Prevents harming cargo.
			if (!self.IsInWorld)
				return;

			var level = raLayer.GetLevel(self.Location);
			if (level <= 0)
				return;

			int dmg = Info.DamageCoeff * level / 1000;
			self.InflictDamage(self.World.WorldActor, new Damage(dmg, Info.DamageTypes));
			damageTicks = Info.DamageInterval;
		}
	}
}
