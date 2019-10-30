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
using OpenRA.Traits;

namespace OpenRA.Orders
{
	public class UnitOrderGenerator : IOrderGenerator
	{
		readonly bool useClassicMouse = Game.Settings.Game.UseClassicMouseStyle;

		List<UnitOrderResult> orderResults;
		string lockedCursor;
		bool canDrag;
		int ticks;
		int2 mouseStartPos;
		CPos startCell;
		MouseInput heldMouseInput;
		bool actionButtonHeld;
		public int2 MousePos;

		public bool IsOrderDragging
		{
			get
			{
				if (!canDrag)
					return false;

				if (useClassicMouse)
					return ticks >= Game.Settings.Game.LeftDragOrderDelay;

				return ticks >= Game.Settings.Game.DragOrderDelay && (mouseStartPos - MousePos).Length > Game.Settings.Game.DragOrderDeadZone;
			}
		}

		static Target TargetForInput(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var actor = world.ScreenMap.ActorsAtMouse(mi)
				.Where(a => !a.Actor.IsDead && a.Actor.Info.HasTraitInfo<ITargetableInfo>() && !world.FogObscures(a.Actor))
				.WithHighestSelectionPriority(worldPixel, mi.Modifiers);

			if (actor != null)
				return Target.FromActor(actor);

			var frozen = world.ScreenMap.FrozenActorsAtMouse(world.RenderPlayer, mi)
				.Where(a => a.Info.HasTraitInfo<ITargetableInfo>() && a.Visible && a.HasRenderables)
				.WithHighestSelectionPriority(worldPixel, mi.Modifiers);

			if (frozen != null)
				return Target.FromFrozenActor(frozen);

			return Target.FromCell(world, cell);
		}

		public void HoldActionButton(int2 mousePos, CPos cell, MouseInput mi)
		{
			mouseStartPos = mousePos;
			actionButtonHeld = true;
			startCell = cell;
			heldMouseInput = mi;
		}

		public void HoldTarget(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var target = TargetForInput(world, cell, worldPixel, mi);
			var actorsAt = world.ActorMap.GetActorsAt(cell).ToList();
			orderResults = world.Selection.Actors
				.Select(a => OrderForUnit(a, target, actorsAt, cell, mi))
				.Where(o => o != null)
				.ToList();
		}

		public virtual IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (!IsOrderDragging)
				HoldTarget(world, cell, worldPixel, mi);

			if (orderResults == null)
				yield break;

			var selection = world.Selection.Actors;
			var actorsInvolved = orderResults.Select(o => o.Actor).Distinct();
			if (!actorsInvolved.Intersect(selection).Any())
			{
				orderResults.Clear();
				actionButtonHeld = false;
				yield break;
			}

			// HACK: This is required by the hacky player actions-per-minute calculation
			// TODO: Reimplement APM properly and then remove this
			yield return new Order("CreateGroup", actorsInvolved.First().Owner.PlayerActor, false)
			{
				TargetString = actorsInvolved.Select(a => a.ActorID).JoinWith(",")
			};

			foreach (var o in orderResults)
			{
				if (!o.Actor.IsInWorld || o.Actor.IsDead || !selection.Contains(o.Actor))
					continue;

				yield return CheckSameOrder(o.OrderTargeter, o.Trait.IssueOrder(o.Actor, o.OrderTargeter, o.Target, mi.Modifiers.HasModifier(Modifiers.Shift), cell));
			}

			orderResults.Clear();
			actionButtonHeld = false;
		}

		public virtual void Tick(World world)
		{
			if (actionButtonHeld && !IsOrderDragging && canDrag)
			{
				if (!useClassicMouse || (mouseStartPos - MousePos).Length <= Game.Settings.Game.SelectionDeadzone)
					ticks++;

				if (ticks == (useClassicMouse ? Game.Settings.Game.LeftDragOrderDelay : Game.Settings.Game.DragOrderDelay))
					HoldTarget(world, startCell, mouseStartPos, heldMouseInput);
			}

			if (!actionButtonHeld)
			{
				ticks = 0;
				lockedCursor = null;
			}
		}

		public virtual IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public virtual IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		public virtual IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			if (orderResults == null || orderResults.Count == 0)
				yield break;

			if (!IsOrderDragging)
				yield break;

			foreach (var o in orderResults)
			{
				if (!o.Actor.IsInWorld || o.Actor.IsDead)
					continue;

				foreach (var r in o.OrderTargeter.RenderAnnotations(wr, world, o.Actor, o.Target))
					yield return r;
			}
		}

		public virtual string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (IsOrderDragging && lockedCursor != null && lockedCursor.Any())
				return lockedCursor;

			var target = TargetForInput(world, cell, worldPixel, mi);
			var actorsAt = world.ActorMap.GetActorsAt(cell).ToList();

			bool useSelect;
			if (useClassicMouse && !InputOverridesSelection(world, worldPixel, mi))
				useSelect = target.Type == TargetType.Actor && target.Actor.Info.HasTraitInfo<SelectableInfo>();
			else
			{
				var ordersWithCursor = world.Selection.Actors
					.Select(a => OrderForUnit(a, target, actorsAt, cell, mi))
					.Where(o => o != null && o.Cursor != null);

				var cursorOrder = ordersWithCursor.MaxByOrDefault(o => o.OrderTargeter.OrderPriority);
				if (cursorOrder != null)
				{
					lockedCursor = cursorOrder.Cursor;
					canDrag = cursorOrder.OrderTargeter.CanDrag;
					return lockedCursor;
				}

				useSelect = target.Type == TargetType.Actor && target.Actor.Info.HasTraitInfo<SelectableInfo>() &&
				    (mi.Modifiers.HasModifier(Modifiers.Shift) || !world.Selection.Actors.Any());
			}

			return useSelect ? "select" : "default";
		}

		public void Deactivate() { }

		bool IOrderGenerator.HandleKeyPress(KeyInput e) { return false; }

		// Used for classic mouse orders, determines whether or not action at xy is move or select
		public virtual bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			var actor = world.ScreenMap.ActorsAtMouse(xy)
				.Where(a => !a.Actor.IsDead)
				.WithHighestSelectionPriority(xy, mi.Modifiers);

			if (actor == null)
				return true;

			var target = Target.FromActor(actor);
			var cell = world.Map.CellContaining(target.CenterPosition);
			var actorsAt = world.ActorMap.GetActorsAt(cell).ToList();

			var modifiers = TargetModifiers.None;
			if (mi.Modifiers.HasModifier(Modifiers.Ctrl))
				modifiers |= TargetModifiers.ForceAttack;
			if (mi.Modifiers.HasModifier(Modifiers.Shift))
				modifiers |= TargetModifiers.ForceQueue;
			if (mi.Modifiers.HasModifier(Modifiers.Alt))
				modifiers |= TargetModifiers.ForceMove;

			foreach (var a in world.Selection.Actors)
			{
				var o = OrderForUnit(a, target, actorsAt, cell, mi);
				if (o != null && o.OrderTargeter.TargetOverridesSelection(a, target, actorsAt, cell, modifiers))
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

			// The Select(x => x) is required to work around an issue on mono 5.0
			// where calling OrderBy* on SelectManySingleSelectorIterator can in some
			// circumstances (which we were unable to identify) replace entries in the
			// enumeration with duplicates of other entries.
			// Other action that replace the SelectManySingleSelectorIterator with a
			// different enumerator type (e.g. .Where(true) or .ToList()) also work.
			var orders = self.TraitsImplementing<IIssueOrder>()
				.SelectMany(trait => trait.OrderTargeters.Select(x => new { Trait = trait, OrderTargeter = x }))
				.Select(x => x)
				.OrderByDescending(x => x.OrderTargeter.OrderPriority);

			for (var i = 0; i < 2; i++)
			{
				foreach (var o in orders)
				{
					var localModifiers = modifiers;
					string cursor = null;
					if (o.OrderTargeter.CanTarget(self, target, actorsAt, ref localModifiers, ref cursor))
						return new UnitOrderResult(self, o.OrderTargeter, o.Trait, cursor, target);
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
			public readonly IOrderTargeter OrderTargeter;
			public readonly IIssueOrder Trait;
			public readonly string Cursor;
			public readonly Target Target;

			public UnitOrderResult(Actor actor, IOrderTargeter orderTargeter, IIssueOrder trait, string cursor, Target target)
			{
				Actor = actor;
				OrderTargeter = orderTargeter;
				Trait = trait;
				Cursor = cursor;
				Target = target;
			}
		}

		public virtual bool ClearSelectionOnLeftClick { get { return true; } }
	}
}
