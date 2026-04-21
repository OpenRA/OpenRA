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
	[Desc("Grant a condition to the crushing actor.")]
	public class GrantExternalConditionToCrusherInfo : TraitInfo
	{
		[Desc("The condition to apply on a crush attempt. Must be included among the crusher actor's ExternalCondition traits.")]
		public readonly string WarnCrushCondition = null;

		[Desc("Duration of the condition applied on a crush attempt (in ticks). Set to 0 for a permanent condition.")]
		public readonly int WarnCrushDuration = 0;

		[Desc("The condition to apply on a successful crush. Must be included among the crusher actor's ExternalCondition traits.")]
		public readonly string OnCrushCondition = null;

		[Desc("Duration of the condition applied on a successful crush (in ticks). Set to 0 for a permanent condition.")]
		public readonly int OnCrushDuration = 0;

		public override object Create(ActorInitializer init) { return new GrantExternalConditionToCrusher(this); }
	}

	public class GrantExternalConditionToCrusher : INotifyCrushed
	{
		public readonly GrantExternalConditionToCrusherInfo Info;

		public GrantExternalConditionToCrusher(GrantExternalConditionToCrusherInfo info)
		{
			Info = info;
		}

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			crusher.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.OnCrushCondition && t.CanGrantCondition(self))
				?.GrantCondition(crusher, self, Info.OnCrushDuration);
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			crusher.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.WarnCrushCondition && t.CanGrantCondition(self))
				?.GrantCondition(crusher, self, Info.WarnCrushDuration);
		}
	}
}
