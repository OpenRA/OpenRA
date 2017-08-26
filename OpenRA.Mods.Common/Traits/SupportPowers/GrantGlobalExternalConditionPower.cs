#region Copyright & License Information
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

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Used to grant a support power to all own actors.")]
	class GrantGlobalExternalConditionPowerInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		public override object Create(ActorInitializer init) { return new GrantGlobalExternalConditionPower(init.Self, this); }
	}

	class GrantGlobalExternalConditionPower : SupportPower
	{
		readonly GrantGlobalExternalConditionPowerInfo info;

		public GrantGlobalExternalConditionPower(Actor self, GrantGlobalExternalConditionPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.IssueOrder(new Order(order, manager.Self, false));
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var eligbleActors = self.World.ActorsHavingTrait<ExternalCondition>().Where(a => a.Owner == self.Owner);
			foreach (var a in eligbleActors)
			{
				var external = a.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == info.Condition && t.CanGrantCondition(a, self));

				if (external != null)
					external.GrantCondition(a, self, info.Duration);
			}
		}
	}
}
