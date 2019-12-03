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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class RepairOrderGenerator : GlobalButtonOrderGenerator
	{
		int2 dragStartMousePos;
		int2 dragEndMousePos;
		bool isDragging;

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if ((mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down) || (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up) || (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up) || (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Move))
				return OrderInner(world, cell, worldPixel, mi);

			return Enumerable.Empty<Order>();
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			return OrderInner(world, mi, worldPixel);
		}

		protected IEnumerable<Order> OrderInner(World world, MouseInput mi, int2 worldPixel)
		{
			if (mi.Button != MouseButton.Left)
				yield break;

			dragEndMousePos = worldPixel;

			if (mi.Event == MouseInputEvent.Down)
			{
				if (!isDragging)
				{
					isDragging = true;
					dragStartMousePos = worldPixel;
				}

				yield break;
			}

			if (mi.Event == MouseInputEvent.Move)
				yield break;

			// Use "isDragging" here to avoid mis-dragging when player use hot key to switch mode.
			if (isDragging && mi.Event == MouseInputEvent.Up)
			{
				var actors = SelectRepairableActorsInBoxWithDeadzone(world, dragStartMousePos, dragEndMousePos, mi.Modifiers);
				isDragging = false;

				if (!actors.Any())
					yield break;

				foreach (var actor in actors)
					yield return RepairUnderCondition(actor, world, mi);
			}
		}

		protected Order RepairUnderCondition(Actor actor, World world, MouseInput mi)
		{
			// Repair a building.
			if (actor.Info.HasTraitInfo<RepairableBuildingInfo>())
				return new Order("RepairBuilding", world.LocalPlayer.PlayerActor, Target.FromActor(actor), false);

			Actor repairBuilding = null;
			var orderId = "Repair";

			// Repair units.
			var repairable = actor.TraitOrDefault<Repairable>();
			if (repairable != null)
				repairBuilding = repairable.FindRepairBuilding(actor);
			else
			{
				var repairableNear = actor.TraitOrDefault<RepairableNear>();
				if (repairableNear != null)
				{
					orderId = "RepairNear";
					repairBuilding = repairableNear.FindRepairBuilding(actor);
				}
			}

			return new Order(orderId, actor, Target.FromActor(repairBuilding), mi.Modifiers.HasModifier(Modifiers.Shift))
			{
				VisualFeedbackTarget = Target.FromActor(actor)
			};
		}

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			var lastMousePos = wr.Viewport.ViewToWorldPx(Viewport.LastMousePos);
			if (isDragging && (lastMousePos - dragStartMousePos).Length > Game.Settings.Game.SelectionDeadzone)
			{
				var diag1 = wr.ProjectedPosition(lastMousePos);
				var diag2 = wr.ProjectedPosition(dragStartMousePos);
				var modifiers = Game.GetModifierKeys();

				// Draw the rectangle box dragged by mouse.
				yield return new RectangleAnnotationRenderable(diag1, diag2, diag1, 1, Color.FromArgb(0xff009a00));

				/* Following codes do two things:
				// 1. Draw health bar for every repairable units/buildings that can be repaired inside the box under modifier.
				// 2. Draw highlight box for each unit/building that can be repaired inside the box under modifier.
				*/
				var actors = SelectRepairableActorsInBoxWithDeadzone(world, dragStartMousePos, lastMousePos, modifiers, true);
				foreach (var actor in actors)
				{
					var decorationBounds = actor.TraitsImplementing<IDecorationBounds>().ToArray();
					var bounds = decorationBounds.FirstNonEmptyBounds(actor, wr);
					yield return new SelectionBarsAnnotationRenderable(actor, bounds, true, false);
					yield return new GlobalButtonOrderSelectionBoxAnnotationRenderable(actor, bounds, Color.FromArgb(0xff00ea00));
				}
			}

			yield break;
		}

		protected bool CheckRepairable(Actor actor, World world)
		{
			if (actor.GetDamageState() == DamageState.Undamaged)
				return false;

			// 1. Test for buildings repairable.
			if (actor.Info.HasTraitInfo<RepairableBuildingInfo>())
				return true;

			// 2. Test for generic repairable (used on units).
			// Player can only repair their own units. Unlike buildings.
			if (actor.Owner != world.LocalPlayer)
				return false;

			Actor repairBuilding = null;

			var repairable = actor.TraitOrDefault<Repairable>();
			if (repairable != null)
				repairBuilding = repairable.FindRepairBuilding(actor);
			else
			{
				var repairableNear = actor.TraitOrDefault<RepairableNear>();
				if (repairableNear != null)
				{
					repairBuilding = repairableNear.FindRepairBuilding(actor);
				}
			}

			if (repairBuilding != null)
				return true;

			return false;
		}

		protected IEnumerable<Actor> SelectRepairableActorsInBoxWithDeadzone(World world, int2 a, int2 b, Modifiers modifiers, bool forRendering = false)
		{
			// Because the "WorldInteractionControllerWidget" can show detailed unit's information when mouse over,
			// so we can just leave it alone when render under cursor actor. No needs to rend it twice.
			var isDeadzone = true;
			if ((a - b).Length <= Game.Settings.Game.SelectionDeadzone)
			{
				if (forRendering)
					return Enumerable.Empty<Actor>();
				else
					isDeadzone = false;
			}

			IEnumerable<Actor> allActors;

			if (isDeadzone)
			{
				// "x.AppearsFriendlyTo(world.LocalPlayer.PlayerActor)" only select local player and allied units.
				// "x.Owner == world.LocalPlayer" only select local player units which are from local player and allied
				// "x.Info.HasTraitInfo<SelectableInfo>()" make sure ".SubsetWithHighestSelectionPriority(Modifiers.None)" works without exception
				// when used with the line above.
				allActors = world.ScreenMap.ActorsInMouseBox(a, b)
				.Select(x => x.Actor)
				.Where(x => x.Info.HasTraitInfo<SelectableInfo>() && x.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) && !world.FogObscures(x)
					&& CheckRepairable(x, world));

				if (!allActors.Any())
					return allActors;

				/* Smart Selection Of Buildings: at first, check repairing-status of things inside,
				// then either select those who not active or select all whose repairing-status are actived.
				// Because players can repair allies's buildings, so must preprocess here to
				// get Allies' buildings in lowest priority.
				*/

				// Preprocess:
				if (modifiers != Modifiers.Ctrl)
				{
					if (!allActors.All(x => x.Owner == world.LocalPlayer))
					{
						if (!allActors.All(x => x.Owner != world.LocalPlayer))
						{
							allActors = allActors
							.Select(x => x)
							.Where(x => !x.Info.HasTraitInfo<RepairableBuildingInfo>() || x.Owner == world.LocalPlayer);
						}
					}
				}

				// Smart select building part
				if (!allActors.All(x => !x.Info.HasTraitInfo<RepairableBuildingInfo>() || x.Trait<RepairableBuilding>().RepairActive))
				{
					allActors = allActors
						.Select(x => x)
						.Where(x => !x.Info.HasTraitInfo<RepairableBuildingInfo>() || !x.Trait<RepairableBuilding>().RepairActive);
				}
			}

			// When "isDeadzone == false", only choose one or no actor.
			// No more annoying things like Smart Selection or Repair Selection Priority.
			else
			{
				allActors = world.ScreenMap.ActorsAtMouse(b)
				.Select(x => x.Actor)
				.Where(x => x.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) && !world.FogObscures(x)
					&& CheckRepairable(x, world));

				return allActors;
			}

			if (forRendering)
			{
				allActors = allActors
							.Select(x => x)
							.Where(x => x.TraitOrDefault<ISelectionDecorations>() != null);
			}

			/* Repair Selection Priority:
			// Default: buildings > combat vehicles and aircrafts > non-combat vehicles and aircrafts > allied buildings
			// Ctrl: all
			// Alt: combat vehicles and aircrafts > non-combat vehicles and aircrafts > buildings > allied buildings
			*/
			if (modifiers == Modifiers.Ctrl)
			{
				return allActors;
			}
			else if (modifiers == Modifiers.Alt)
			{
				var repairableBuildings = allActors
						.Select(x => x)
						.Where(x => !x.Info.HasTraitInfo<RepairableBuildingInfo>())
						.SubsetWithHighestSelectionPriority(Modifiers.None);
				if (!repairableBuildings.Any())
					return allActors;
				return repairableBuildings;
			}

			// Default modifier:
			else
			{
				var ownerRepairableBuildings = allActors
						.Select(x => x)
						.Where(x => x.Info.HasTraitInfo<RepairableBuildingInfo>() && x.Owner == world.LocalPlayer);
				if (!ownerRepairableBuildings.Any())
				{
					return allActors
						.Select(x => x)
						.SubsetWithHighestSelectionPriority(Modifiers.None);
				}

				return ownerRepairableBuildings;
			}
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return MouseOverActor(world, worldPixel) ? "repair" : "repair-blocked";
		}

		protected bool MouseOverActor(World world, int2 worldPixel)
		{
			// "x.Info.HasTraitInfo<SelectableInfo>()" avoids selecting some special actors like "camera" and "mutiplayer starting point".
			var underCursor = world.ScreenMap.ActorsAtMouse(worldPixel)
				.Select(x => x.Actor)
				.Where(x => x.Info.HasTraitInfo<SelectableInfo>() && !world.FogObscures(x));

			// ONLY when the mouse is over an enemy/unrepairable/non-allied-building and selectable actor, the cursor will change to "repair-blocked",
			// which means cursor is "repair" when no normal actors under the mouse.
			if (!underCursor.Any())
				return true;
			else
			{
				var actor = underCursor.First();
				return actor.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) && CheckRepairable(actor, world);
			}
		}
	}
}
