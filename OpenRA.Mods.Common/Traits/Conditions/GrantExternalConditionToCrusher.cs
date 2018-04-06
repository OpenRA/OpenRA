﻿#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grant a condition to the crushing actor.")]
	public class GrantExternalConditionToCrusherInfo : ITraitInfo
	{
		[Desc("The condition to apply on a crush attempt. Must be included among the crusher actor's ExternalCondition traits.")]
		public readonly string WarnCrushCondition = null;

		[Desc("Duration of the condition applied on a crush attempt (in ticks). Set to 0 for a permanent condition.")]
		public readonly int WarnCrushDuration = 0;

		[Desc("The condition to apply on a successful crush. Must be included among the crusher actor's ExternalCondition traits.")]
		public readonly string OnCrushCondition = null;

		[Desc("Duration of the condition applied on a successful crush (in ticks). Set to 0 for a permanent condition.")]
		public readonly int OnCrushDuration = 0;

		public virtual object Create(ActorInitializer init) { return new GrantExternalConditionToCrusher(init.Self, this); }
	}

	public class GrantExternalConditionToCrusher : INotifyCrushed
	{
		public readonly GrantExternalConditionToCrusherInfo Info;

		public GrantExternalConditionToCrusher(Actor self, GrantExternalConditionToCrusherInfo info)
		{
			this.Info = info;
		}

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			var external = crusher.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.OnCrushCondition && t.CanGrantCondition(crusher, self));

			if (external != null)
				external.GrantCondition(crusher, self, Info.OnCrushDuration);
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			var external = crusher.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.WarnCrushCondition && t.CanGrantCondition(crusher, self));

			if (external != null)
				external.GrantCondition(crusher, self, Info.WarnCrushDuration);
		}
	}
}
