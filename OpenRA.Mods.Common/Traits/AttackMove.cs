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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides access to the attack-move command, which will make the actor automatically engage viable targets while moving to the destination.")]
	public class AttackMoveInfo : TraitInfo, Requires<IMoveInfo>
	{
		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.OrangeRed;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while an attack-move is active.")]
		public readonly string AttackMoveCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while an assault-move is active.")]
		public readonly string AssaultMoveCondition = null;

		[Desc("Can the actor be ordered to move in to shroud?")]
		public readonly bool MoveIntoShroud = true;

		[CursorReference]
		public readonly string AttackMoveCursor = "attackmove";

		[CursorReference]
		public readonly string AttackMoveBlockedCursor = "attackmove-blocked";

		[CursorReference]
		public readonly string AssaultMoveCursor = "assaultmove";

		[CursorReference]
		public readonly string AssaultMoveBlockedCursor = "assaultmove-blocked";

		public override object Create(ActorInitializer init) { return new AttackMove(init.Self, this); }
	}

	sealed class AttackMove : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly AttackMoveInfo Info;
		readonly IMove move;

		public AttackMove(Actor self, AttackMoveInfo info)
		{
			move = self.Trait<IMove>();
			Info = info;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (Game.Settings.Game.AttackMoveIsDefault)
					yield return new AttackMoveOrderTargeter(Info);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "AttackMove")
				return new Order(order.OrderID, self, target, queued);

			return null;
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
				if (!order.Target.IsValidFor(self))
					return;

				var cell = self.World.Map.Clamp(self.World.Map.CellContaining(order.Target.CenterPosition));
				if (!Info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(cell))
					return;

				var targetLocation = move.NearestMoveableCell(cell);
				var assaultMoving = order.OrderString == "AssaultMove";

				// TODO: this should scale with unit selection group size.
				self.QueueActivity(order.Queued, new AttackMoveActivity(self, () => move.MoveTo(targetLocation, 8, targetLineColor: Info.TargetLineColor), assaultMoving));
				self.ShowTargetLines();
			}
		}
	}

	public class AttackMoveOrderGenerator : UnitOrderGenerator
	{
		TraitPair<AttackMove>[] subjects;

		protected override MouseActionType ActionType => MouseActionType.ConfirmOrder;

		public AttackMoveOrderGenerator(World world, IEnumerable<Actor> subjects)
			: base(world)
		{
			this.subjects = subjects.Where(a => !a.IsDead)
				.SelectMany(a => a.TraitsImplementing<AttackMove>()
					.Select(am => new TraitPair<AttackMove>(a, am)))
				.ToArray();
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
			if (!queued)
				world.CancelInputMode();
			else
				HasIssuedQueuedCommand = true;

			var orderName = mi.Modifiers.HasModifier(Modifiers.Ctrl) ? "AssaultMove" : "AttackMove";

			// Cells outside the playable area should be clamped to the edge for consistency with move orders
			cell = world.Map.Clamp(cell);
			yield return new Order(orderName, null, Target.FromCell(world, cell), queued, null, subjects.Select(s => s.Actor).ToArray());
		}

		public override void SelectionChanged(World world, IEnumerable<Actor> selected)
		{
			subjects = selected.Where(s => !s.IsDead).SelectMany(a => a.TraitsImplementing<AttackMove>()
					.Select(am => new TraitPair<AttackMove>(a, am)))
				.ToArray();

			// AttackMove doesn't work without AutoTarget, so require at least one unit in the selection to have it
			if (!subjects.Any(s => s.Actor.Info.HasTraitInfo<AutoTargetInfo>()))
				world.CancelInputMode();
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var isAssaultMove = mi.Modifiers.HasModifier(Modifiers.Ctrl);

			var subject = subjects.FirstOrDefault();
			if (subject.Actor != null)
			{
				var info = subject.Trait.Info;
				if (world.Map.Contains(cell))
				{
					var explored = subject.Actor.Owner.Shroud.IsExplored(cell);
					var cannotMove = subjects.FirstOrDefault(a => !a.Trait.Info.MoveIntoShroud).Trait;
					var blocked = !explored && cannotMove != null;

					if (isAssaultMove)
						return blocked ? cannotMove.Info.AssaultMoveBlockedCursor : info.AssaultMoveCursor;

					return blocked ? cannotMove.Info.AttackMoveBlockedCursor : info.AttackMoveCursor;
				}

				if (isAssaultMove)
					return info.AssaultMoveBlockedCursor;
				else
					return info.AttackMoveBlockedCursor;
			}

			return null;
		}

		public override bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			// Custom order generators always override selection
			return true;
		}

		public override bool ClearSelectionOnLeftClick => false;
	}

	sealed class AttackMoveOrderTargeter : IOrderTargeter
	{
		readonly AttackMoveInfo info;

		public AttackMoveOrderTargeter(AttackMoveInfo info)
		{
			this.info = info;
		}

		public string OrderID => "AttackMove";

		// Below Attack's force-attack-ground priority (6) so Ctrl+click on empty
		// ground can still force-fire, but above Move's priority (4) so a plain
		// click defaults to attack-move instead of a plain move.
		public int OrderPriority => 5;
		public bool IsQueued { get; private set; }

		public bool CanTarget(Actor self, in Target target, ref TargetModifiers modifiers, ref string cursor)
		{
			// Alt (ForceMove) is the general "give me a plain move" override used throughout
			// the game - both when held directly and via the dedicated Force Move button/hotkey.
			if (modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			// While the player is using the repurposed attack-move button/hotkey to force a
			// plain default click (see MoveOrderGenerator), attack-move must not compete with it.
			if (self.World.OrderGenerator is MoveOrderGenerator)
				return false;

			// AttackMove only overrides a plain click on empty terrain - clicking directly on
			// a unit/building is left entirely to Attack/Enter/Repair/etc.'s own targeters.
			if (target.Type != TargetType.Terrain)
				return false;

			// AttackMove is meaningless without AutoTarget, and only makes sense if the order is accepted.
			if (!self.Info.HasTraitInfo<AutoTargetInfo>() || !self.AcceptsOrder(OrderID))
				return false;

			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

			var location = self.World.Map.CellContaining(target.CenterPosition);
			var explored = self.Owner.Shroud.IsExplored(location);

			cursor = !self.World.Map.Contains(location) || (!explored && !info.MoveIntoShroud)
				? info.AttackMoveBlockedCursor
				: info.AttackMoveCursor;

			return true;
		}

		public bool TargetOverridesSelection(Actor self, in Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers)
		{
			return modifiers.HasModifier(TargetModifiers.ForceMove);
		}
	}
}
