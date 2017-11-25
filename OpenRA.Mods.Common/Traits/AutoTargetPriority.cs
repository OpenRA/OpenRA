#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Specifies the target types and relative priority used by AutoTarget to decide what to target.")]
	public class AutoTargetPriorityInfo : ConditionalTraitInfo, Requires<AutoTargetInfo>
	{
		[Desc("Target types that can be AutoTargeted.")]
		public readonly HashSet<string> ValidTargets = new HashSet<string> { "Ground", "Water", "Air" };

		[Desc("Target types that can't be AutoTargeted.", "Overrules ValidTargets.")]
		public readonly HashSet<string> InvalidTargets = new HashSet<string>();

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
