#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Orders
{
	public class UnitOrderGenerator : IOrderGenerator
	{
		static Target TargetForInput(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var actor = world.ScreenMap.ActorsAt(mi)
				.Where(a => !world.FogObscures(a) && a.Info.HasTraitInfo<ITargetableInfo>())
				.WithHighestSelectionPriority(worldPixel);

			if (actor != null)
				return Target.FromActor(actor);

			var frozen = world.ScreenMap.FrozenActorsAt(world.RenderPlayer, mi)
				.Where(a => a.Info.HasTraitInfo<ITargetableInfo>() && a.Visible && a.HasRenderables)
				.WithHighestSelectionPriority(worldPixel);

			if (frozen != null)
				return Target.FromFrozenActor(frozen);

			return Target.FromCell(world, cell);
		}

		public virtual IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var target = TargetForInput(world, cell, worldPixel, mi);
			var actorsAt = world.ActorMap.GetActorsAt(cell).ToList();
			var orders = world.Selection.Actors
				.Select(a => OrderForUnit(a, target, actorsAt, cell, mi))
				.Where(o => o != null)
				.ToList();

			var actorsInvolved = orders.Select(o => o.Actor).Distinct();
			if (!actorsInvolved.Any())
				yield break;

			yield return new Order("CreateGroup", actorsInvolved.First().Owner.PlayerActor, false)
			{
				TargetString = actorsInvolved.Select(a => a.ActorID).JoinWith(",")
			};

			foreach (var o in orders)
				yield return CheckSameOrder(o.Order, o.Trait.IssueOrder(o.Actor, o.Order, o.Target, mi.Modifiers.HasModifier(Modifiers.Shift)));
		}

		public virtual void Tick(World world) { }
		public virtual IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public virtual IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		public virtual string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var useSelect = false;
			var target = TargetForInput(world, cell, worldPixel, mi);
			var actorsAt = world.ActorMap.GetActorsAt(cell).ToList();

			if (target.Type == TargetType.Actor && target.Actor.Info.HasTraitInfo<SelectableInfo>() &&
					(mi.Modifiers.HasModifier(Modifiers.Shift) || !world.Selection.Actors.Any()))
				useSelect = true;

			var ordersWithCursor = world.Selection.Actors
				.Select(a => OrderForUnit(a, target, actorsAt, cell, mi))
				.Where(o => o != null && o.Cursor != null);

			var cursorOrder = ordersWithCursor.MaxByOrDefault(o => o.Order.OrderPriority);

			return cursorOrder != null ? cursorOrder.Cursor : (useSelect ? "select" : "default");
		}

		// Used for classic mouse orders, determines whether or not action at xy is move or select
		public virtual bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			var actor = world.ScreenMap.ActorsAt(xy).WithHighestSelectionPriority(xy);
			if (actor == null)
				return true;

			var target = Target.FromActor(actor);
			var cell = world.Map.CellContaining(target.CenterPosition);
			var actorsAt = world.ActorMap.GetActorsAt(cell).ToList();
			var underCursor = world.Selection.Actors.WithHighestSelectionPriority(xy);

			var o = OrderForUnit(underCursor, target, actorsAt, cell, mi);
			if (o != null)
			{
				var modifiers = TargetModifiers.None;
				if (mi.Modifiers.HasModifier(Modifiers.Ctrl))
					modifiers |= TargetModifiers.ForceAttack;
				if (mi.Modifiers.HasModifier(Modifiers.Shift))
					modifiers |= TargetModifiers.ForceQueue;
				if (mi.Modifiers.HasModifier(Modifiers.Alt))
					modifiers |= TargetModifiers.ForceMove;

				if (o.Order.TargetOverridesSelection(modifiers))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns the most appropriate order for a given actor and target.
		/// First priority is given to orders that interact with the given actors.
		/// Second priority is given to actors in the given cell.
		/// </summary>
		static UnitOrderResult OrderForUnit(Actor self, Target target, List<Actor> actorsAt, CPos xy, MouseInput mi)
		{
			if (mi.Button != Game.Settings.Game.MouseButtonPreference.Action)
				return null;

			if (self.Owner != self.World.LocalPlayer)
				return null;

			if (self.World.IsGameOver)
				return null;

			if (self.Disposed || !target.IsValidFor(self))
				return null;

			var modifiers = TargetModifiers.None;
			if (mi.Modifiers.HasModifier(Modifiers.Ctrl))
				modifiers |= TargetModifiers.ForceAttack;
			if (mi.Modifiers.HasModifier(Modifiers.Shift))
				modifiers |= TargetModifiers.ForceQueue;
			if (mi.Modifiers.HasModifier(Modifiers.Alt))
				modifiers |= TargetModifiers.ForceMove;

			var orders = self.TraitsImplementing<IIssueOrder>()
				.SelectMany(trait => trait.Orders.Select(x => new { Trait = trait, Order = x }))
				.OrderByDescending(x => x.Order.OrderPriority);

			for (var i = 0; i < 2; i++)
			{
				foreach (var o in orders)
				{
					var localModifiers = modifiers;
					string cursor = null;
					if (o.Order.CanTarget(self, target, actorsAt, ref localModifiers, ref cursor))
						return new UnitOrderResult(self, o.Order, o.Trait, cursor, target);
				}

				// No valid orders, so check for orders against the cell
				target = Target.FromCell(self.World, xy);
			}

			return null;
		}

		static Order CheckSameOrder(IOrderTargeter iot, Order order)
		{
			if (order == null && iot.OrderID != null)
				Game.Debug("BUG: in order targeter - decided on {0} but then didn't order", iot.OrderID);
			else if (order != null && iot.OrderID != order.OrderString)
				Game.Debug("BUG: in order targeter - decided on {0} but ordered {1}", iot.OrderID, order.OrderString);
			return order;
		}

		class UnitOrderResult
		{
			public readonly Actor Actor;
			public readonly IOrderTargeter Order;
			public readonly IIssueOrder Trait;
			public readonly string Cursor;
			public readonly Target Target;

			public UnitOrderResult(Actor actor, IOrderTargeter order, IIssueOrder trait, string cursor, Target target)
			{
				Actor = actor;
				Order = order;
				Trait = trait;
				Cursor = cursor;
				Target = target;
			}
		}
	}
}
