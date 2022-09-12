#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Heals all actors that belong to the owner of the collector.")]
	class HealActorsCrateActionInfo : CrateActionInfo
	{
		[Desc("The target type(s) of the actors this crate action will heal. Leave empty to heal all actors.")]
		public readonly BitSet<TargetableType> TargetTypes = default(BitSet<TargetableType>);

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
			// PERF: Apply is faster than enumerating over trait pairs.
			var targetTypes = info.TargetTypes;
			collector.World.ApplyToActorsWithTrait<IHealth>((actor, trait) =>
			{
				if (actor.Owner != collector.Owner)
					return;

				if (!trait.IsDead && (targetTypes.IsEmpty || targetTypes.Overlaps(actor.GetEnabledTargetTypes())))
					trait.InflictDamage(actor, actor, new Damage(-(trait.MaxHP - trait.HP)), true);
			});

			base.Activate(collector);
		}
	}
}
