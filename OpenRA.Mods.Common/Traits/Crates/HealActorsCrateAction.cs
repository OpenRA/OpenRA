#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Heals all actors that belong to the owner of the collector.")]
	class HealActorsCrateActionInfo : CrateActionInfo
	{
		[Desc("The target type(s) of the actors this crate action will heal. Leave empty to heal all actors.")]
		public readonly BitSet<TargetableType> TargetTypes = default;

		public override object Create(ActorInitializer init) { return new HealActorsCrateAction(init.Self, this); }
	}

	class HealActorsCrateAction : CrateAction
	{
		readonly HealActorsCrateActionInfo info;

		public HealActorsCrateAction(Actor self, HealActorsCrateActionInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor collector)
		{
			foreach (var healable in collector.World.ActorsWithTrait<IHealth>().Where(tp => tp.Actor.Owner == collector.Owner))
				if (!healable.Trait.IsDead && (info.TargetTypes.IsEmpty || info.TargetTypes.Overlaps(healable.Actor.GetEnabledTargetTypes())))
					healable.Trait.InflictDamage(healable.Actor, healable.Actor, new Damage(-(healable.Trait.MaxHP - healable.Trait.HP)), true);

			base.Activate(collector);
		}
	}
}
