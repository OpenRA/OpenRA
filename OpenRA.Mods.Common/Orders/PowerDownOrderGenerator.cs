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
	class PowerDownOrderGenerator : GlobalButtonOrderGenerator
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
				var actors = SelectToggleConditionActorsInBoxWithDeadzone(world, dragStartMousePos, dragEndMousePos, mi.Modifiers);

				isDragging = false;

				if (!actors.Any())
					yield break;

				foreach (var actor in actors)
					yield return new Order("PowerDown", actor, false);
			}
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
				yield return new RectangleAnnotationRenderable(diag1, diag2, diag1, 1, Color.Yellow);

				/* Following code do two things:
				// 1. Draw health bar for every units/buildings can be power-down inside the box.
				// 2. Draw highlight box for each unit/building that can be power-down inside the box.
				*/
				var actors = SelectToggleConditionActorsInBoxWithDeadzone(world, dragStartMousePos, lastMousePos, modifiers, true);
				int powerChanged = 0;
				bool toggleConditions = actors.Any() ? actors.First().Trait<ToggleConditionOnOrder>().IsEnabled() : false;

				foreach (var actor in actors)
				{
					var decorationBounds = actor.TraitsImplementing<IDecorationBounds>().ToArray();
					var bounds = decorationBounds.FirstNonEmptyBounds(actor, wr);
					powerChanged += actor.TraitsImplementing<Power>().Where(t => !t.IsTraitDisabled).Sum(p => p.Info.Amount);

					yield return new SelectionBarsAnnotationRenderable(actor, bounds, true, false);
					yield return new GlobalButtonOrderSelectionBoxAnnotationRenderable(actor, bounds, Color.Orange);
				}

				if (powerChanged != 0)
				{
					var font = Game.Renderer.Fonts["Bold"];
					if (toggleConditions)
						yield return new TextAnnotationRenderable(font, wr.ProjectedPosition(lastMousePos + new int2(20, 0)), 0, Color.Red, powerChanged.ToString());
					else
						yield return new TextAnnotationRenderable(font, wr.ProjectedPosition(lastMousePos + new int2(20, 0)), 0, Color.Gold, (0 - powerChanged).ToString());
				}
			}

			yield break;
		}

		protected IEnumerable<Actor> SelectToggleConditionActorsInBoxWithDeadzone(World world, int2 a, int2 b, Modifiers modifiers, bool forRendering = false)
		{
			// Because the "WorldInteractionControllerWidget" can show detailed unit's information when mouse over,
			// so we can just leave it alone when render under cursor actor. No needs to render it twice.
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
				// "x.Owner == world.LocalPlayer" only select local player units which is from local player and allied,
				// when used with the line above.
				allActors = world.ScreenMap.ActorsInMouseBox(a, b)
					.Select(x => x.Actor)
					.Where(x => x.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) && x.Owner == world.LocalPlayer && !world.FogObscures(x)
						&& x.TraitsImplementing<ToggleConditionOnOrder>().Any(IsValidTrait));

				if (!allActors.Any())
					return allActors;
			}
			else
			{
				allActors = world.ScreenMap.ActorsAtMouse(b)
					.Select(x => x.Actor)
					.Where(x => x.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) && x.Owner == world.LocalPlayer && !world.FogObscures(x)
						&& x.TraitsImplementing<ToggleConditionOnOrder>().Any(IsValidTrait));
				return allActors;
			}

			if (forRendering)
			{
				allActors = allActors
					.Select(x => x)
					.Where(x => x.TraitOrDefault<ISelectionDecorations>() != null);
			}

			/* Modifiers for Powerdown Mode
			// Default: generally turn on/off with smart selection.
			// Ctrl: Only turn off.
			// Alt: Only turn on.
			*/
			if (modifiers == Modifiers.Ctrl)
			{
				return allActors = allActors
						.Select(x => x)
						.Where(x => !x.Trait<ToggleConditionOnOrder>().IsEnabled());
			}
			else if (modifiers == Modifiers.Alt)
			{
				return allActors = allActors
						.Select(x => x)
						.Where(x => x.Trait<ToggleConditionOnOrder>().IsEnabled());
			}

			// Default modifier:
			else
			{
				/* Smart Selection Of Buildings: at first, check power-down status of things inside,
				// then either select those who are not power-down or select all whose power-down status are actived.
				*/
				if (!allActors.All(x => x.Trait<ToggleConditionOnOrder>().IsEnabled()))
				{
					return allActors
						.Select(x => x)
						.Where(x => !x.Trait<ToggleConditionOnOrder>().IsEnabled());
				}

				return allActors;
			}
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			// "x.Info.HasTraitInfo<SelectableInfo>()" avoids selecting some special actors like "camera" and "mutiplayer starting point".
			var underCursor = world.ScreenMap.ActorsAtMouse(worldPixel)
					.Select(x => x.Actor)
					.Where(x => x.Info.HasTraitInfo<SelectableInfo>() && !world.FogObscures(x));

			// ONLY when the mouse is over an enemy/allied/powerdown-blocked and selectable actor, the cursor will change to "powerdown-blocked",
			// which means cursor is "powerdown" when no normal actors under the mouse.
			if (!underCursor.Any())
				return "powerdown";
			else
			{
				var actor = underCursor.First();
				if (actor.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) && actor.Owner == world.LocalPlayer
						&& actor.TraitsImplementing<ToggleConditionOnOrder>().Any(IsValidTrait))
					return "powerdown";
				else
					return "powerdown-blocked";
			}
		}

		protected bool IsValidTrait(ToggleConditionOnOrder t)
		{
			return !t.IsTraitDisabled && !t.IsTraitPaused;
		}
	}
}
