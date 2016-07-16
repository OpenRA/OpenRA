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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Heals all actors that belong to the owner of the collector.")]
	class HealUnitsCrateActionInfo : CrateActionInfo
	{
		public override object Create(ActorInitializer init) { return new HealUnitsCrateAction(init.Self, this); }
	}

	class HealUnitsCrateAction : CrateAction
	{
		public HealUnitsCrateAction(Actor self, HealUnitsCrateActionInfo info)
			: base(self, info) { }

		public override void Activate(Actor collector)
		{
			foreach (var healable in collector.World.ActorsWithTrait<Health>().Where(tp => tp.Actor.Owner == collector.Owner))
				if (!healable.Trait.IsDead)
					healable.Trait.InflictDamage(healable.Actor, healable.Actor, new Damage(-(healable.Trait.MaxHP - healable.Trait.HP)), true);

			base.Activate(collector);
		}
	}
}
