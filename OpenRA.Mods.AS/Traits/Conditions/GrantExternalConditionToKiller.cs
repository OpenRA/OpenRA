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
	[Desc("Grant an external condition to the killer.")]
	public class GrantExternalConditionToKillerInfo : ITraitInfo
	{
		[Desc("The condition to apply. Must be included among the target actor's ExternalCondition traits.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent upgrade.")]
		public readonly int Duration = 0;

		[Desc("Stance the attacking player needs to receive the condition.")]
		public readonly Stance ValidStances = Stance.Neutral | Stance.Enemy;

		[Desc("DeathType(s) that grant the condition. Leave empty to always grant the condition.")]
		public readonly HashSet<string> DeathTypes = new HashSet<string>();

		public virtual object Create(ActorInitializer init) { return new GrantExternalConditionToKiller(init.Self, this); }
	}

	public class GrantExternalConditionToKiller : INotifyKilled
	{
		public readonly GrantExternalConditionToKillerInfo Info;

		public GrantExternalConditionToKiller(Actor self, GrantExternalConditionToKillerInfo info)
		{
			this.Info = info;
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (e.Attacker == null || e.Attacker.Disposed)
				return;

			if (Info.DeathTypes.Count > 0 && !e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			if (!Info.ValidStances.HasStance(e.Attacker.Owner.Stances[self.Owner]))
				return;

			var external = e.Attacker.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.Condition && t.CanGrantCondition(e.Attacker, self));

			if (external != null)
				external.GrantCondition(e.Attacker, self, Info.Duration);
		}
	}
}
