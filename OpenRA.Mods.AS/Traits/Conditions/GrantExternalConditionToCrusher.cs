#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grant a condition to the crushing actor.")]
	public class GrantExternalConditionToCrusherInfo : ITraitInfo
	{
		[Desc("The condition to apply. Must be included among the target actor's ExternalCondition traits.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent upgrade.")]
		public readonly int Duration = 0;

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
				.FirstOrDefault(t => t.Info.Condition == Info.Condition && t.CanGrantCondition(crusher, self));

			if (external != null)
				external.GrantCondition(crusher, self);
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, HashSet<string> crushClasses) { }
	}
}
