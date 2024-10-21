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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Provides access to the Patrol command.")]
	public class PatrolInfo : TraitInfo, Requires<AttackMoveInfo>
	{
		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Yellow;

		public override object Create(ActorInitializer init) { return new Patrol(init.Self, this); }
	}

	public class Patrol : IResolveOrder, IOrderVoice
	{
		readonly PatrolInfo info;
		readonly AttackMove attackMove;
		readonly IMove move;

		public List<CPos> PatrolWaypoints { get; } = new();

		public Patrol(Actor self, PatrolInfo info)
		{
			this.info = info;
			attackMove = self.Trait<AttackMove>();
			move = self.Trait<IMove>();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "AssaultPatrol" || order.OrderString == "AttackPatrol"
				? info.Voice
				: null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "AssaultPatrol" || order.OrderString == "AttackPatrol")
			{
				var cell = self.World.Map.Clamp(self.World.Map.CellContaining(order.Target.CenterPosition));
				if (!attackMove.Info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(cell))
					return;

				if (!order.Queued)
					PatrolWaypoints.Clear();

				if (PatrolWaypoints.Count == 0)
				{
					PatrolWaypoints.Add(cell);
					var assaultMoving = order.OrderString == "AssaultPatrol";
					self.QueueActivity(order.Queued, new PatrolActivity(move, this, info.TargetLineColor, 0, assaultMoving));
				}
				else if (!PatrolWaypoints[^1].Equals(cell))
					PatrolWaypoints.Add(cell);

				self.ShowTargetLines();
			}

			// Explicitly also clearing/cancelling on queued orders as well as non-queued because otherwise they won't ever be executed.
			else
				PatrolWaypoints.Clear();
		}

		public void AddStartingPoint(CPos start)
		{
			PatrolWaypoints.Insert(0, start);
		}
	}

	public class PatrolOrderGenerator : AttackMoveOrderGenerator
	{
		public PatrolOrderGenerator(IEnumerable<Actor> subjects, MouseButton button)
			: base(subjects, button) { }

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, MouseInput mi)
		{
			if (mi.Button == ExpectedButton)
			{
				var queued = mi.Modifiers.HasModifier(Modifiers.Shift);
				if (!queued)
					world.CancelInputMode();

				var orderName = mi.Modifiers.HasModifier(Modifiers.Ctrl) ? "AssaultPatrol" : "AttackPatrol";

				// Cells outside the playable area should be clamped to the edge for consistency with move orders.
				cell = world.Map.Clamp(cell);
				yield return new Order(orderName, null, Target.FromCell(world, cell), queued, null, subjects.Select(s => s.Actor).ToArray());
			}
		}
	}
}
