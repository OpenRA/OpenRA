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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Orders;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class WorldInteractionControllerWidget : Widget
	{
		protected readonly World World;
		readonly WorldRenderer worldRenderer;
		int2 dragStart, mousePos;
		bool isDragging = false;

		bool IsValidDragbox
		{
			get
			{
				return isDragging && (dragStart - mousePos).Length > Game.Settings.Game.SelectionDeadzone;
			}
		}

		[ObjectCreator.UseCtor]
		public WorldInteractionControllerWidget(World world, WorldRenderer worldRenderer)
		{
			World = world;
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
			if (IsValidDragbox)
			{
				// Render actors in the dragbox
				Game.Renderer.WorldRgbaColorRenderer.DrawRect(dragStart, mousePos,
					1 / worldRenderer.Viewport.Zoom, Color.White);
				foreach (var u in SelectActorsInBoxWithDeadzone(World, dragStart, mousePos))
					DrawRollover(u);
			}
			else
			{
				// Render actors under the mouse pointer
				foreach (var u in SelectActorsInBoxWithDeadzone(World, mousePos, mousePos))
					DrawRollover(u);
			}
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			mousePos = worldRenderer.Viewport.ViewToWorldPx(mi.Location);

			var useClassicMouseStyle = Game.Settings.Game.UseClassicMouseStyle;

			var multiClick = mi.MultiTapCount >= 2;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!TakeMouseFocus(mi))
					return false;

				dragStart = mousePos;
				isDragging = true;

				// Place buildings, use support powers, and other non-unit things
				if (!(World.OrderGenerator is UnitOrderGenerator))
				{
					ApplyOrders(World, mi);
					isDragging = false;
					YieldMouseFocus(mi);
					return true;
				}
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (World.OrderGenerator is UnitOrderGenerator)
				{
					if (useClassicMouseStyle && HasMouseFocus)
					{
						if (!IsValidDragbox && World.Selection.Actors.Any() && !multiClick)
						{
							var selectableActor = World.ScreenMap.ActorsAt(mousePos).Any(x =>
								x.Info.HasTraitInfo<SelectableInfo>() && (x.Owner.IsAlliedWith(World.RenderPlayer) || !World.FogObscures(x)));

							var uog = (UnitOrderGenerator)World.OrderGenerator;
							if (!selectableActor || uog.InputOverridesSelection(World, mousePos, mi))
							{
								// Order units instead of selecting
								ApplyOrders(World, mi);
								isDragging = false;
								YieldMouseFocus(mi);
								return true;
							}
						}
					}

					if (multiClick)
					{
						var unit = World.ScreenMap.ActorsAt(mousePos)
							.WithHighestSelectionPriority(mousePos);

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
						if (isDragging && (!(World.OrderGenerator is GenericSelectTarget) || IsValidDragbox))
						{
							var newSelection = SelectActorsInBoxWithDeadzone(World, dragStart, mousePos);
							World.Selection.Combine(World, newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == mousePos);
						}
					}

					World.CancelInputMode();
				}

				isDragging = false;
				YieldMouseFocus(mi);
			}

			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up)
			{
				// Don't do anything while selecting
				if (!IsValidDragbox)
				{
					if (useClassicMouseStyle)
						World.Selection.Clear();

					ApplyOrders(World, mi);
				}
			}

			return true;
		}

		void ApplyOrders(World world, MouseInput mi)
		{
			if (world.OrderGenerator == null)
				return;

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);
			var worldPixel = worldRenderer.Viewport.ViewToWorldPx(mi.Location);
			var orders = world.OrderGenerator.Order(world, cell, worldPixel, mi).ToArray();
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
						world.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, world, "moveflsh", "idle", "moveflash", true, true)));
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
				if (IsValidDragbox)
					return null;

				var cell = worldRenderer.Viewport.ViewToWorld(screenPos);
				var worldPixel = worldRenderer.Viewport.ViewToWorldPx(screenPos);

				var mi = new MouseInput
				{
					Location = screenPos,
					Button = Game.Settings.Game.MouseButtonPreference.Action,
					Modifiers = Game.GetModifierKeys()
				};

				return World.OrderGenerator.GetCursor(World, cell, worldPixel, mi);
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
				else if (key == Game.Settings.Keys.ToggleMuteKey)
					return ToggleMute();
				else if (key == Game.Settings.Keys.TogglePlayerStanceColorsKey)
					return TogglePlayerStanceColors();
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
			if (worldRenderer.Viewport.Zoom == 1f)
				worldRenderer.Viewport.Zoom = 2f;
			else
			{
				// Reset zoom to regular view if it was anything else before
				// (like a zoom level only reachable by using the scroll wheel).
				worldRenderer.Viewport.Zoom = 1f;
			}

			Game.Settings.Graphics.PixelDouble = worldRenderer.Viewport.Zoom == 2f;

			return true;
		}

		bool ToggleMute()
		{
			Game.Settings.Sound.Mute ^= true;

			if (Game.Settings.Sound.Mute)
			{
				Game.Sound.MuteAudio();
				Game.Debug("Audio muted");
			}
			else
			{
				Game.Sound.UnmuteAudio();
				Game.Debug("Audio unmuted");
			}

			return true;
		}

		bool TogglePlayerStanceColors()
		{
			Game.Settings.Game.UsePlayerStanceColors ^= true;

			return true;
		}
	}
}
