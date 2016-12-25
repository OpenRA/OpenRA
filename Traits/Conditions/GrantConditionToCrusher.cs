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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grant a condition to the crushing actor.")]
	public class GrantConditionToCrusherInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to apply.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent upgrade.")]
		public readonly int Duration = 0;

		public virtual object Create(ActorInitializer init) { return new GrantConditionToCrusher(init.Self, this); }
	}

	public class GrantConditionToCrusher : INotifyCrushed
	{
		public readonly GrantConditionToCrusherInfo Info;

		public GrantConditionToCrusher(Actor self, GrantConditionToCrusherInfo info)
		{
			this.Info = info;
		}

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			var um = crusher.TraitOrDefault<ConditionManager>();
			if (um == null)
				return;

			um.GrantCondition(self, Info.Condition, duration: Info.Duration);
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, HashSet<string> crushClasses) { }
	}
}
