using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.SupportPowers
{
	class GrantSelfConditionPowerInfo : SupportPowerInfo
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("The condition type to grant when actived.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new GrantSelfConditionPower(init.Self, this); }
	}

	class GrantSelfConditionPower : SupportPower
	{
		readonly GrantSelfConditionPowerInfo info;
		int conditionToken = Actor.InvalidConditionToken;

		public GrantSelfConditionPower(Actor self, GrantSelfConditionPowerInfo info)
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
			PlayLaunchSounds();
			if (conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(info.Condition);
		}

		public override void Deactivate(Actor self, Order order, SupportPowerManager manager)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
