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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Specifies the target types and relative priority used by AutoTarget to decide what to target.")]
	public class AutoTargetPriorityInfo : ConditionalTraitInfo, Requires<AutoTargetInfo>
	{
		[Desc("Target types that can be AutoTargeted.")]
		public readonly BitSet<TargetableType> ValidTargets = new BitSet<TargetableType>("Ground", "Water", "Air");

		[Desc("Target types that can't be AutoTargeted.", "Overrules ValidTargets.")]
		public readonly BitSet<TargetableType> InvalidTargets;

		[Desc("Relationships between actor's and target's owner needed for AutoTargeting.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally | PlayerRelationship.Neutral | PlayerRelationship.Enemy;

		[Desc("ValidTargets with larger priorities will be AutoTargeted before lower priorities.")]
		public readonly int Priority = 1;

		public override object Create(ActorInitializer init) { return new AutoTargetPriority(this); }
	}

	public class AutoTargetPriority : ConditionalTrait<AutoTargetPriorityInfo>
	{
		public AutoTargetPriority(AutoTargetPriorityInfo info)
			: base(info) { }
	}
}
