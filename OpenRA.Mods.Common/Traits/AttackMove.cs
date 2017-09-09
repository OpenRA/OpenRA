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

using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides access to the attack-move command, which will make the actor automatically engage viable targets while moving to the destination.")]
	class AttackMoveInfo : ITraitInfo, Requires<IMoveInfo>
	{
		[VoiceReference] public readonly string Voice = "Action";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while scanning for targets during an attack-move.")]
		public readonly string AttackMoveScanCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while scanning for targets during an assault-move.")]
		public readonly string AssaultMoveScanCondition = null;

		public object Create(ActorInitializer init) { return new AttackMove(init.Self, this); }
	}

	class AttackMove : INotifyCreated, ITick, IResolveOrder, IOrderVoice, INotifyIdle, ISync
	{
		[Sync] public CPos _targetLocation { get { return TargetLocation.HasValue ? TargetLocation.Value : CPos.Zero; } }
		public CPos? TargetLocation = null;

		readonly IMove move;
		readonly AttackMoveInfo info;

		ConditionManager conditionManager;
		int attackMoveToken = ConditionManager.InvalidConditionToken;
		int assaultMoveToken = ConditionManager.InvalidConditionToken;
		bool assaultMoving = false;

		public AttackMove(Actor self, AttackMoveInfo info)
		{
			move = self.Trait<IMove>();
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void ITick.Tick(Actor self)
		{
			if (conditionManager == null)
				return;

			var activity = self.CurrentActivity as AttackMoveActivity;
			var attackActive = activity != null && !assaultMoving;
			var assaultActive = activity != null && assaultMoving;

			if (attackActive && attackMoveToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(info.AttackMoveScanCondition))
				attackMoveToken = conditionManager.GrantCondition(self, info.AttackMoveScanCondition);
			else if (!attackActive && attackMoveToken != ConditionManager.InvalidConditionToken)
				attackMoveToken = conditionManager.RevokeCondition(self, attackMoveToken);

			if (assaultActive && assaultMoveToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(info.AssaultMoveScanCondition))
				assaultMoveToken = conditionManager.GrantCondition(self, info.AssaultMoveScanCondition);
			else if (!assaultActive && assaultMoveToken != ConditionManager.InvalidConditionToken)
				assaultMoveToken = conditionManager.RevokeCondition(self, assaultMoveToken);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackMove" || order.OrderString == "AssaultMove")
				return info.Voice;

			return null;
		}

		void Activate(Actor self, bool assaultMove)
		{
			assaultMoving = assaultMove;
			self.QueueActivity(new AttackMoveActivity(self, move.MoveTo(TargetLocation.Value, 1)));
		}

		public void TickIdle(Actor self)
		{
			// This might cause the actor to be stuck if the target location is unreachable
			if (TargetLocation.HasValue && self.Location != TargetLocation.Value)
				Activate(self, assaultMoving);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			TargetLocation = null;

			if (order.OrderString == "AttackMove" || order.OrderString == "AssaultMove")
			{
				if (!order.Queued)
					self.CancelActivity();

				TargetLocation = move.NearestMoveableCell(order.TargetLocation);
				self.SetTargetLine(Target.FromCell(self.World, TargetLocation.Value), Color.Red);
				Activate(self, order.OrderString == "AssaultMove");
			}
		}
	}
}
