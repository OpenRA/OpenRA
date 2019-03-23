#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Orders;
using OpenRA.Primitives;
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

	class AttackMove : INotifyCreated, ITick, IResolveOrder, IOrderVoice
	{
		public readonly AttackMoveInfo Info;
		readonly IMove move;

		ConditionManager conditionManager;
		int attackMoveToken = ConditionManager.InvalidConditionToken;
		int assaultMoveToken = ConditionManager.InvalidConditionToken;

		public AttackMove(Actor self, AttackMoveInfo info)
		{
			move = self.Trait<IMove>();
			Info = info;
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
			var attackActive = activity != null && !activity.IsAssaultMove;
			var assaultActive = activity != null && activity.IsAssaultMove;

			if (attackActive && attackMoveToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.AttackMoveScanCondition))
				attackMoveToken = conditionManager.GrantCondition(self, Info.AttackMoveScanCondition);
			else if (!attackActive && attackMoveToken != ConditionManager.InvalidConditionToken)
				attackMoveToken = conditionManager.RevokeCondition(self, attackMoveToken);

			if (assaultActive && assaultMoveToken == ConditionManager.InvalidConditionToken && !string.IsNullOrEmpty(Info.AssaultMoveScanCondition))
				assaultMoveToken = conditionManager.GrantCondition(self, Info.AssaultMoveScanCondition);
			else if (!assaultActive && assaultMoveToken != ConditionManager.InvalidConditionToken)
				assaultMoveToken = conditionManager.RevokeCondition(self, assaultMoveToken);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (!Info.MoveIntoShroud && order.Target.Type != TargetType.Invalid)
			{
				var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
				if (!self.Owner.Shroud.IsExplored(cell))
					return null;
			}

			if (order.OrderString == "AttackMove" || order.OrderString == "AssaultMove")
				return Info.Voice;

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "AttackMove" || order.OrderString == "AssaultMove")
			{
				if (!order.Queued)
					self.CancelActivity();

				var cell = self.World.Map.Clamp(self.World.Map.CellContaining(order.Target.CenterPosition));
				if (!Info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(cell))
					return;

				var targetLocation = move.NearestMoveableCell(cell);
				self.SetTargetLine(Target.FromCell(self.World, targetLocation), Color.Red);
				var assaultMoving = order.OrderString == "AssaultMove";
				self.QueueActivity(new AttackMoveActivity(self, () => move.MoveTo(targetLocation, 1), assaultMoving));
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

		public override void Tick(World world)
		{
			if (subjects.All(s => s.Actor.IsDead))
				world.CancelInputMode();
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var prefix = mi.Modifiers.HasModifier(Modifiers.Ctrl) ? "assaultmove" : "attackmove";

			if (world.Map.Contains(cell) && subjects.Any())
			{
				var explored = subjects.First().Actor.Owner.Shroud.IsExplored(cell);
				var blocked = !explored && subjects.Any(a => !a.Trait.Info.MoveIntoShroud);
				return blocked ? prefix + "-blocked" : prefix;
			}

			return prefix + "-blocked";
		}

		public override bool InputOverridesSelection(WorldRenderer wr, World world, int2 xy, MouseInput mi)
		{
			// Custom order generators always override selection
			return true;
		}
	}
}
