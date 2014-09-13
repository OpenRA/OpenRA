#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Crates
{
	[Desc("Heals all actors that belong to the owner of the collector.")]
	class HealUnitsCrateActionInfo : CrateActionInfo
	{
		public override object Create(ActorInitializer init) { return new HealUnitsCrateAction(init.self, this); }
	}

	class HealUnitsCrateAction : CrateAction
	{
		public HealUnitsCrateAction(Actor self, HealUnitsCrateActionInfo info)
			: base(self, info) { }

		public override void Activate(Actor collector)
		{
			foreach (var unit in collector.World.Actors.Where(a => a.Owner == collector.Owner))
			{
				var health = unit.TraitOrDefault<Health>();
				if (health != null && !health.IsDead)
					health.InflictDamage(unit, unit, -(health.MaxHP - health.HP), null, true);
			}

			base.Activate(collector);
		}
	}
}
