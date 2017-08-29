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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Orders;
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

		[Desc("Can the actor be ordered to move in to shroud?")]
		public readonly bool MoveIntoShroud = true;

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

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (!info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(order.TargetLocation))
				return null;

			if (order.OrderString == "AttackMove" || order.OrderString == "AssaultMove")
				return info.Voice;

			return null;
		}

		void Activate(Actor self, bool assaultMove)
		{
			assaultMoving = assaultMove;
			self.QueueActivity(new AttackMoveActivity(self, move.MoveTo(TargetLocation.Value, 1)));
		}

		void INotifyIdle.TickIdle(Actor self)
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

				if (!info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(order.TargetLocation))
					return;

				TargetLocation = move.NearestMoveableCell(order.TargetLocation);
				self.SetTargetLine(Target.FromCell(self.World, TargetLocation.Value), Color.Red);
				Activate(self, order.OrderString == "AssaultMove");
			}
		}
	}

	public class AttackMoveOrderGenerator : UnitOrderGenerator
	{
		readonly TraitPair<AttackMove>[] subjects;
		readonly MouseButton expectedButton;

		public AttackMoveOrderGenerator(IEnumerable<Actor> subjects, MouseButton button)
		{
			expectedButton = button;

			this.subjects = subjects.Where(a => !a.IsDead)
				.SelectMany(a => a.TraitsImplementing<AttackMove>()
					.Select(am => new TraitPair<AttackMove>(a, am)))
				.ToArray();
		}

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != expectedButton)
				world.CancelInputMode();

			return OrderInner(world, cell, mi);
		}

		protected virtual IEnumerable<Order> OrderInner(World world, CPos cell, MouseInput mi)
		{
			if (mi.Button == expectedButton)
			{
				world.CancelInputMode();

				var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
				var orderName = mi.Modifiers.HasModifier(Modifiers.Ctrl) ? "AssaultMove" : "AttackMove";

				// Cells outside the playable area should be clamped to the edge for consistency with move orders
				cell = world.Map.Clamp(cell);
				foreach (var s in subjects)
					yield return new Order(orderName, s.Actor, Target.FromCell(world, cell), queued);
			}
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var prefix = mi.Modifiers.HasModifier(Modifiers.Ctrl) ? "assaultmove" : "attackmove";
			return world.Map.Contains(cell) ? prefix : prefix + "-blocked";
		}

		public override bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			// Custom order generators always override selection
			return true;
		}
	}
}
