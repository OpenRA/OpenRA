#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
			var underCursor = world.ScreenMap.ActorsAt(mi)
				.Where(a => !world.FogObscures(a) && a.Info.HasTraitInfo<ITargetableInfo>())
				.WithHighestSelectionPriority();

			Target target;
			if (underCursor != null)
				target = Target.FromActor(underCursor);
			else
			{
				var frozen = world.ScreenMap.FrozenActorsAt(world.RenderPlayer, mi)
					.Where(a => a.Info.HasTraitInfo<ITargetableInfo>() && a.Visible && a.HasRenderables)
					.WithHighestSelectionPriority();
				target = frozen != null ? Target.FromFrozenActor(frozen) : Target.FromCell(world, xy);
			}

			var orders = world.Selection.Actors
				.Select(a => OrderForUnit(a, target, mi))
				.Where(o => o != null)
				.ToList();

			var actorsInvolved = orders.Select(o => o.Actor).Distinct();
			if (actorsInvolved.Any())
				yield return new Order("CreateGroup", actorsInvolved.First().Owner.PlayerActor, false)
				{
					TargetString = actorsInvolved.Select(a => a.ActorID).JoinWith(",")
				};

			foreach (var o in orders)
				yield return CheckSameOrder(o.Order, o.Trait.IssueOrder(o.Actor, o.Order, o.Target, mi.Modifiers.HasModifier(Modifiers.Shift)));
		}

		public void Tick(World world) { }
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world) { yield break; }

		public string GetCursor(World world, CPos xy, MouseInput mi)
		{
			var useSelect = false;
			var underCursor = world.ScreenMap.ActorsAt(mi)
				.Where(a => !world.FogObscures(a) && a.Info.HasTraitInfo<ITargetableInfo>())
				.WithHighestSelectionPriority();

			if (underCursor != null && (mi.Modifiers.HasModifier(Modifiers.Shift) || !world.Selection.Actors.Any()))
			{
				if (underCursor.Info.HasTraitInfo<SelectableInfo>())
					useSelect = true;
			}

			Target target;
			if (underCursor != null)
				target = Target.FromActor(underCursor);
			else
			{
				var frozen = world.ScreenMap.FrozenActorsAt(world.RenderPlayer, mi)
					.Where(a => a.Info.HasTraitInfo<ITargetableInfo>() && a.Visible && a.HasRenderables)
					.WithHighestSelectionPriority();
				target = frozen != null ? Target.FromFrozenActor(frozen) : Target.FromCell(world, xy);
			}

			var ordersWithCursor = world.Selection.Actors
				.Select(a => OrderForUnit(a, target, mi))
				.Where(o => o != null && o.Cursor != null);

			var cursorOrder = ordersWithCursor.MaxByOrDefault(o => o.Order.OrderPriority);

			return cursorOrder != null ? cursorOrder.Cursor : (useSelect ? "select" : "default");
		}

		// Used for classic mouse orders, determines whether or not action at xy is move or select
		public static bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			var target = Target.FromActor(world.ScreenMap.ActorsAt(xy).WithHighestSelectionPriority());
			var underCursor = world.Selection.Actors.WithHighestSelectionPriority();

			var o = OrderForUnit(underCursor, target, mi);

			if (o != null && o.Order.OverrideSelection)
				return false;

			return true;
		}

		static UnitOrderResult OrderForUnit(Actor self, Target target, MouseInput mi)
		{
			if (self.Owner != self.World.LocalPlayer)
				return null;

			if (self.World.IsGameOver)
				return null;

			if (self.Disposed || !target.IsValidFor(self))
				return null;

			if (mi.Button == Game.Settings.Game.MouseButtonPreference.Action)
			{
				foreach (var o in self.TraitsImplementing<IIssueOrder>()
					.SelectMany(trait => trait.Orders
						.Select(x => new { Trait = trait, Order = x }))
					.OrderByDescending(x => x.Order.OrderPriority))
				{
					var actorsAt = self.World.ActorMap.GetActorsAt(self.World.Map.CellContaining(target.CenterPosition)).ToList();

					var modifiers = TargetModifiers.None;
					if (mi.Modifiers.HasModifier(Modifiers.Ctrl))
						modifiers |= TargetModifiers.ForceAttack;
					if (mi.Modifiers.HasModifier(Modifiers.Shift))
						modifiers |= TargetModifiers.ForceQueue;
					if (mi.Modifiers.HasModifier(Modifiers.Alt))
						modifiers |= TargetModifiers.ForceMove;

					string cursor = null;
					if (o.Order.CanTarget(self, target, actorsAt, ref modifiers, ref cursor))
						return new UnitOrderResult(self, o.Order, o.Trait, cursor, target);
				}
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
				this.Actor = actor;
				this.Order = order;
				this.Trait = trait;
				this.Cursor = cursor;
				this.Target = target;
			}
		}
	}
}
