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
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Widgets
{
	public class WorldInteractionControllerWidget : Widget
	{
		protected readonly World World;
		readonly WorldRenderer worldRenderer;
		int2? dragStart, dragEnd;
		int2 lastMousePosition;

		[ObjectCreator.UseCtor]
		public WorldInteractionControllerWidget(World world, WorldRenderer worldRenderer)
		{
			this.World = world;
			this.worldRenderer = worldRenderer;
		}

		void DrawRollover(Actor unit)
		{
			// TODO: Integrate this with SelectionDecorations to unhardcode the *Renderable
			if (unit.Info.HasTraitInfo<SelectableInfo>())
				new SelectionBarsRenderable(unit, true, true).Render(worldRenderer);
		}

		public override void Draw()
		{
			if (!IsDragging)
			{
				// Render actors under the mouse pointer
				foreach (var u in SelectActorsInBoxWithDeadzone(World, lastMousePosition, lastMousePosition))
					DrawRollover(u);

				return;
			}

			// Render actors in the dragbox
			var selbox = SelectionBox;
			Game.Renderer.WorldRgbaColorRenderer.DrawRect(selbox.Value.First, selbox.Value.Second,
				1 / worldRenderer.Viewport.Zoom, Color.White);
			foreach (var u in SelectActorsInBoxWithDeadzone(World, selbox.Value.First, selbox.Value.Second))
				DrawRollover(u);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			var xy = worldRenderer.Viewport.ViewToWorldPx(mi.Location);

			var useClassicMouseStyle = Game.Settings.Game.UseClassicMouseStyle;

			var hasBox = SelectionBox != null;
			var multiClick = mi.MultiTapCount >= 2;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!TakeMouseFocus(mi))
					return false;

				dragStart = xy;

				// Place buildings, use support powers, and other non-unit things
				if (!(World.OrderGenerator is UnitOrderGenerator))
				{
					ApplyOrders(World, mi);
					dragStart = dragEnd = null;
					YieldMouseFocus(mi);
					lastMousePosition = xy;
					return true;
				}
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Move && dragStart.HasValue)
				dragEnd = xy;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (World.OrderGenerator is UnitOrderGenerator)
				{
					if (useClassicMouseStyle && HasMouseFocus)
					{
						if (!hasBox && World.Selection.Actors.Any() && !multiClick)
						{
							if (!(World.ScreenMap.ActorsAt(xy).Any(x => x.Info.HasTraitInfo<SelectableInfo>() &&
								(x.Owner.IsAlliedWith(World.RenderPlayer) || !World.FogObscures(x))) && !mi.Modifiers.HasModifier(Modifiers.Ctrl) &&
								!mi.Modifiers.HasModifier(Modifiers.Alt) && UnitOrderGenerator.InputOverridesSelection(World, xy, mi)))
							{
								// Order units instead of selecting
								ApplyOrders(World, mi);
								dragStart = dragEnd = null;
								YieldMouseFocus(mi);
								lastMousePosition = xy;
								return true;
							}
						}
					}

					if (multiClick)
					{
						var unit = World.ScreenMap.ActorsAt(xy)
							.WithHighestSelectionPriority();

						if (unit != null && unit.Owner == (World.RenderPlayer ?? World.LocalPlayer))
						{
							var s = unit.TraitOrDefault<Selectable>();
							if (s != null)
							{
								// Select actors on the screen that have the same selection class as the actor under the mouse cursor
								var newSelection = SelectActorsOnScreen(World, worldRenderer, new HashSet<string> { s.Class }, unit.Owner);

								World.Selection.Combine(World, newSelection, true, false);
							}
						}
					}
					else
					{
						/* The block below does three things:
						// 1. Allows actor selection using a selection box regardless of input mode.
						// 2. Allows actor deselection with a single click in the default input mode (UnitOrderGenerator).
						// 3. Prevents units from getting deselected when exiting input modes (eg. AttackMove or Guard).
						//
						// We cannot check for UnitOrderGenerator here since it's the default order generator that gets activated in
						// World.CancelInputMode. If we did check it, actor de-selection would not be possible by just clicking somewhere,
						// only by dragging an empty selection box.
						*/
						if (dragStart.HasValue && (!(World.OrderGenerator is GenericSelectTarget) || hasBox))
						{
							var newSelection = SelectActorsInBoxWithDeadzone(World, dragStart.Value, xy);
							World.Selection.Combine(World, newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == xy);
						}
					}

					World.CancelInputMode();
				}

				dragStart = dragEnd = null;
				YieldMouseFocus(mi);
			}

			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up)
			{
				// Don't do anything while selecting
				if (!hasBox)
				{
					if (useClassicMouseStyle)
						World.Selection.Clear();

					ApplyOrders(World, mi);
				}
			}

			lastMousePosition = xy;

			return true;
		}

		bool IsDragging
		{
			get
			{
				return dragStart.HasValue && dragEnd.HasValue && (dragStart.Value - dragEnd.Value).Length > Game.Settings.Game.SelectionDeadzone;
			}
		}

		public Pair<int2, int2>? SelectionBox
		{
			get
			{
				if (!IsDragging) return null;
				return Pair.New(dragStart.Value, dragEnd.Value);
			}
		}

		void ApplyOrders(World world, MouseInput mi)
		{
			if (world.OrderGenerator == null)
				return;

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);
			var orders = world.OrderGenerator.Order(world, cell, mi).ToArray();
			world.PlayVoiceForOrders(orders);

			var flashed = false;
			foreach (var order in orders)
			{
				var o = order;
				if (o == null)
					continue;

				if (!flashed && !o.SuppressVisualFeedback)
				{
					if (o.TargetActor != null)
					{
						world.AddFrameEndTask(w => w.Add(new FlashTarget(o.TargetActor)));
						flashed = true;
					}
					else if (o.TargetLocation != CPos.Zero)
					{
						var pos = world.Map.CenterOfCell(cell);
						world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, world, "moveflsh", "moveflash")));
						flashed = true;
					}
				}

				world.IssueOrder(o);
			}
		}

		public override string GetCursor(int2 screenPos)
		{
			return Sync.CheckSyncUnchanged(World, () =>
			{
				// Always show an arrow while selecting
				if (SelectionBox != null)
					return null;

				var cell = worldRenderer.Viewport.ViewToWorld(screenPos);

				var mi = new MouseInput
				{
					Location = screenPos,
					Button = Game.Settings.Game.MouseButtonPreference.Action,
					Modifiers = Game.GetModifierKeys()
				};

				return World.OrderGenerator.GetCursor(World, cell, mi);
			});
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			var player = World.RenderPlayer ?? World.LocalPlayer;

			if (e.Event == KeyInputEvent.Down)
			{
				var key = Hotkey.FromKeyInput(e);

				if (key == Game.Settings.Keys.PauseKey && World.LocalPlayer != null) // Disable pausing for spectators
					World.SetPauseState(!World.Paused);
				else if (key == Game.Settings.Keys.SelectAllUnitsKey && !World.IsGameOver)
				{
					// Select actors on the screen which belong to the current player
					var ownUnitsOnScreen = SelectActorsOnScreen(World, worldRenderer, null, player).SubsetWithHighestSelectionPriority().ToList();

					// Check if selecting actors on the screen has selected new units
					if (ownUnitsOnScreen.Count > World.Selection.Actors.Count())
						Game.Debug("Selected across screen");
					else
					{
						// Select actors in the world that have highest selection priority
						ownUnitsOnScreen = SelectActorsInWorld(World, null, player).SubsetWithHighestSelectionPriority().ToList();
						Game.Debug("Selected across map");
					}

					World.Selection.Combine(World, ownUnitsOnScreen, false, false);
				}
				else if (key == Game.Settings.Keys.SelectUnitsByTypeKey && !World.IsGameOver)
				{
					if (!World.Selection.Actors.Any())
						return false;

					// Get all the selected actors' selection classes
					var selectedClasses = World.Selection.Actors
						.Where(x => !x.IsDead && x.Owner == player)
						.Select(a => a.Trait<Selectable>().Class)
						.ToHashSet();

					// Select actors on the screen that have the same selection class as one of the already selected actors
					var newSelection = SelectActorsOnScreen(World, worldRenderer, selectedClasses, player).ToList();

					// Check if selecting actors on the screen has selected new units
					if (newSelection.Count > World.Selection.Actors.Count())
						Game.Debug("Selected across screen");
					else
					{
						// Select actors in the world that have the same selection class as one of the already selected actors
						newSelection = SelectActorsInWorld(World, selectedClasses, player).ToList();
						Game.Debug("Selected across map");
					}

					World.Selection.Combine(World, newSelection, true, false);
				}
				else if (key == Game.Settings.Keys.CycleStatusBarsKey)
					return CycleStatusBars();
				else if (key == Game.Settings.Keys.TogglePixelDoubleKey)
					return TogglePixelDouble();
			}

			return false;
		}

		static IEnumerable<Actor> SelectActorsOnScreen(World world, WorldRenderer wr, IEnumerable<string> selectionClasses, Player player)
		{
			return SelectActorsByOwnerAndSelectionClass(world.ScreenMap.ActorsInBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight), player, selectionClasses);
		}

		static IEnumerable<Actor> SelectActorsInWorld(World world, IEnumerable<string> selectionClasses, Player player)
		{
			return SelectActorsByOwnerAndSelectionClass(world.Actors.Where(a => a.IsInWorld), player, selectionClasses);
		}

		static IEnumerable<Actor> SelectActorsByOwnerAndSelectionClass(IEnumerable<Actor> actors, Player owner, IEnumerable<string> selectionClasses)
		{
			return actors.Where(a =>
			{
				if (a.Owner != owner)
					return false;

				var s = a.TraitOrDefault<Selectable>();

				// selectionClasses == null means that units, that meet all other criteria, get selected
				return s != null && (selectionClasses == null || selectionClasses.Contains(s.Class));
			});
		}

		static IEnumerable<Actor> SelectActorsInBoxWithDeadzone(World world, int2 a, int2 b)
		{
			// For dragboxes that are too small, shrink the dragbox to a single point (point b)
			if ((a - b).Length <= Game.Settings.Game.SelectionDeadzone)
				a = b;

			return world.ScreenMap.ActorsInBox(a, b)
				.Where(x => x.Info.HasTraitInfo<SelectableInfo>() && (x.Owner.IsAlliedWith(world.RenderPlayer) || !world.FogObscures(x)))
				.SubsetWithHighestSelectionPriority();
		}

		bool CycleStatusBars()
		{
			if (Game.Settings.Game.StatusBars == StatusBarsType.Standard)
				Game.Settings.Game.StatusBars = StatusBarsType.DamageShow;
			else if (Game.Settings.Game.StatusBars == StatusBarsType.DamageShow)
				Game.Settings.Game.StatusBars = StatusBarsType.AlwaysShow;
			else if (Game.Settings.Game.StatusBars == StatusBarsType.AlwaysShow)
				Game.Settings.Game.StatusBars = StatusBarsType.Standard;

			return true;
		}

		bool TogglePixelDouble()
		{
			Game.Settings.Graphics.PixelDouble ^= true;
			worldRenderer.Viewport.Zoom = Game.Settings.Graphics.PixelDouble ? 2 : 1;
			return true;
		}
	}
}
