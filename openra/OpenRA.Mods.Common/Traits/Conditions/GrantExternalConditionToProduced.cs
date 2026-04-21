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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants a condition to actors produced by this actor.")]
	public class GrantExternalConditionToProducedInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the produced actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		public override object Create(ActorInitializer init) { return new GrantExternalConditionToProduced(this); }
	}

	public class GrantExternalConditionToProduced : ConditionalTrait<GrantExternalConditionToProducedInfo>, INotifyProduction
	{
		public GrantExternalConditionToProduced(GrantExternalConditionToProducedInfo info)
			: base(info) { }

		void INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)
		{
			if (IsTraitDisabled || other.IsDead)
				return;

			other.TraitsImplementing<ExternalCondition>()
				.FirstOrDefault(t => t.Info.Condition == Info.Condition && t.CanGrantCondition(other))
				?.GrantCondition(other, self, Info.Duration);
		}
	}
}
