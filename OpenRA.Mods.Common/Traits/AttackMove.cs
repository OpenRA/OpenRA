#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides access to the attack-move command, which will make the actor automatically engage viable targets while moving to the destination.")]
	class AttackMoveInfo : TraitInfo, Requires<IMoveInfo>
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

		public override object Create(ActorInitializer init) { return new AttackMove(init.Self, this); }
	}

	class AttackMove : IResolveOrder, IOrderVoice
	{
		public readonly AttackMoveInfo Info;
		readonly IMove move;

		public AttackMove(Actor self, AttackMoveInfo info)
		{
			move = self.Trait<IMove>();
			Info = info;
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
				yield return new Order(orderName, null, Target.FromCell(world, cell), queued, null, subjects.Select(s => s.Actor).ToArray());
			}
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
			var prefix = mi.Modifiers.HasModifier(Modifiers.Ctrl) ? "assaultmove" : "attackmove";

			if (world.Map.Contains(cell) && subjects.Any())
			{
				var explored = subjects.First().Actor.Owner.Shroud.IsExplored(cell);
				var blocked = !explored && subjects.Any(a => !a.Trait.Info.MoveIntoShroud);
				return blocked ? prefix + "-blocked" : prefix;
			}

			return prefix + "-blocked";
		}

		public override bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			// Custom order generators always override selection
			return true;
		}

		public override bool ClearSelectionOnLeftClick { get { return false; } }
	}
}
