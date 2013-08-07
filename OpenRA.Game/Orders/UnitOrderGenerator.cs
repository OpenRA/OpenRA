#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Orders
{
	class UnitOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			var underCursor = world.FindUnitsAtMouse(mi.Location)
				.Where(a => a.HasTrait<ITargetable>())
				.OrderByDescending(a => a.SelectionPriority())
				.FirstOrDefault();

			var orders = world.Selection.Actors
				.Select(a => OrderForUnit(a, xy, mi, underCursor))
				.Where(o => o != null)
				.ToArray();

			var actorsInvolved = orders.Select(o => o.self).Distinct();
			if (actorsInvolved.Any())
				yield return new Order("CreateGroup", actorsInvolved.First().Owner.PlayerActor, false)
				{
					TargetString = actorsInvolved.Select(a => a.ActorID).JoinWith(",")
				};


			foreach (var o in orders)
				yield return CheckSameOrder(o.iot, o.trait.IssueOrder(o.self, o.iot, o.target, mi.Modifiers.HasModifier(Modifiers.Shift)));
		}

		public void Tick(World world) { }
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public void RenderAfterWorld(WorldRenderer wr, World world) { }

		public string GetCursor(World world, CPos xy, MouseInput mi)
		{
			bool useSelect = false;

			var underCursor = world.FindUnitsAtMouse(mi.Location)
				.Where(a => a.HasTrait<ITargetable>())
				.OrderByDescending(a => a.SelectionPriority())
				.FirstOrDefault();

			if (underCursor != null && (mi.Modifiers.HasModifier(Modifiers.Shift) || !world.Selection.Actors.Any()))
			{
				var selectable = underCursor.TraitOrDefault<Selectable>();
				if (selectable != null && selectable.Info.Selectable)
					useSelect = true;
			}

			var orders = world.Selection.Actors
				.Select(a => OrderForUnit(a, xy, mi, underCursor))
				.Where(o => o != null)
				.ToArray();

			var cursorName = orders.Select(o => o.cursor).FirstOrDefault();
			return cursorName ?? (useSelect ? "select" : "default");
		}

		static UnitOrderResult OrderForUnit(Actor self, CPos xy, MouseInput mi, Actor underCursor)
		{
			if (self.Owner != self.World.LocalPlayer)
				return null;

			if (self.Destroyed)
				return null;

			if (mi.Button == Game.mouseButtonPreference.Action)
			{
				foreach( var o in self.TraitsImplementing<IIssueOrder>()
					.SelectMany(trait => trait.Orders
						.Select(x => new { Trait = trait, Order = x } ))
					.OrderByDescending(x => x.Order.OrderPriority))
				{
					var actorsAt = self.World.ActorMap.GetUnitsAt(xy).ToList();

					var modifiers = TargetModifiers.None;
					if (mi.Modifiers.HasModifier(Modifiers.Ctrl))
						modifiers |= TargetModifiers.ForceAttack;
					if (mi.Modifiers.HasModifier(Modifiers.Shift))
						modifiers |= TargetModifiers.ForceQueue;
					if (mi.Modifiers.HasModifier(Modifiers.Alt))
						modifiers |= TargetModifiers.ForceMove;

					string cursor = null;
					var target = underCursor != null ? Target.FromActor(underCursor) : Target.FromCell(xy);
					if (o.Order.CanTarget(self, target, actorsAt, modifiers, ref cursor))
						return new UnitOrderResult(self, o.Order, o.Trait, cursor, target);
				}
			}

			return null;
		}

		static Order CheckSameOrder(IOrderTargeter iot, Order order)
		{
			if (order == null && iot.OrderID != null)
				Game.Debug("BUG: in order targeter - decided on {0} but then didn't order", iot.OrderID);
			else if (iot.OrderID != order.OrderString)
				Game.Debug("BUG: in order targeter - decided on {0} but ordered {1}", iot.OrderID, order.OrderString);
			return order;
		}

		class UnitOrderResult
		{
			public readonly Actor self;
			public readonly IOrderTargeter iot;
			public readonly IIssueOrder trait;
			public readonly string cursor;
			public readonly Target target;

			public UnitOrderResult(Actor self, IOrderTargeter iot, IIssueOrder trait, string cursor, Target target)
			{
				this.self = self;
				this.iot = iot;
				this.trait = trait;
				this.cursor = cursor;
				this.target = target;
			}
		}
	}

	public static class SelectableExts
	{
		public static int SelectionPriority(this Actor a)
		{
			var selectableInfo = a.Info.Traits.GetOrDefault<SelectableInfo>();
			return selectableInfo != null ? selectableInfo.Priority : int.MinValue;
		}
	}
}
