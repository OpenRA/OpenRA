using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class GrantConditionOnMinelayingInfo : ConditionalTraitInfo, Requires<MinelayerInfo>
	{
		[FieldLoader.Require]
		[GrantedConditionReference]
		[Desc("Condition to grant.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init)
		{
			return new GrantConditionOnMinelaying(this);
		}
	}

	public class GrantConditionOnMinelaying : ConditionalTrait<GrantConditionOnMinelayingInfo>, INotifyMineLaying
	{
		int conditionToken = Actor.InvalidConditionToken;

		public GrantConditionOnMinelaying(GrantConditionOnMinelayingInfo info)
			: base(info)
		{
		}

		void INotifyMineLaying.MineLaid(Actor self, Actor mine)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		void INotifyMineLaying.MineLaying(Actor self, CPos location)
		{
			conditionToken = self.GrantCondition(Info.Condition);
		}

		void INotifyMineLaying.MineLayingCanceled(Actor self, CPos location)
		{
			if (conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}
	}
}
