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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class WorldInteractionControllerWidget : Widget
	{
		public readonly HotkeyReference SelectAllKey = new HotkeyReference();
		public readonly HotkeyReference SelectSameTypeKey = new HotkeyReference();

		protected readonly World World;
		readonly WorldRenderer worldRenderer;
		readonly Color normalSelectionColor;
		readonly Color altSelectionColor;
		readonly Color ctrlSelectionColor;

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
			if (!ChromeMetrics.TryGet("AltSelectionColor", out altSelectionColor))
				altSelectionColor = Color.White;

			if (!ChromeMetrics.TryGet("CtrlSelectionColor", out ctrlSelectionColor))
				ctrlSelectionColor = Color.White;

			if (!ChromeMetrics.TryGet("NormalSelectionColor", out normalSelectionColor))
				normalSelectionColor = Color.White;
		}

		public override void Draw()
		{
			var modifiers = Game.GetModifierKeys();
			IEnumerable<Actor> rollover;
			if (IsValidDragbox)
			{
				var a = worldRenderer.Viewport.WorldToViewPx(dragStart);
				var b = worldRenderer.Viewport.WorldToViewPx(mousePos);

				var color = normalSelectionColor;
				if (modifiers.HasFlag(Modifiers.Alt) && !modifiers.HasFlag(Modifiers.Ctrl))
					color = altSelectionColor;
				else if (modifiers.HasFlag(Modifiers.Ctrl) && !modifiers.HasFlag(Modifiers.Alt))
					color = ctrlSelectionColor;

				Game.Renderer.RgbaColorRenderer.DrawRect(a, b, 1, color);

				// Render actors in the dragbox
				rollover = SelectActorsInBoxWithDeadzone(World, dragStart, mousePos, modifiers);
			}
			else
			{
				// Render actors under the mouse pointer
				rollover = SelectActorsInBoxWithDeadzone(World, mousePos, mousePos, modifiers);
			}

			worldRenderer.World.Selection.SetRollover(rollover);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			mousePos = worldRenderer.Viewport.ViewToWorldPx(mi.Location);

			var useClassicMouseStyle = Game.Settings.Game.UseClassicMouseStyle;

			var multiClick = mi.MultiTapCount >= 2;
			var uog = World.OrderGenerator as UnitOrderGenerator;

			if (uog == null)
			{
				ApplyOrders(World, mi);
				isDragging = false;
				YieldMouseFocus(mi);
				return true;
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!TakeMouseFocus(mi))
					return false;

				dragStart = mousePos;
				isDragging = true;
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (useClassicMouseStyle && HasMouseFocus)
				{
					if (!IsValidDragbox && World.Selection.Actors.Any() && !multiClick && uog.InputOverridesSelection(World, mousePos, mi))
					{
						// Order units instead of selecting
						ApplyOrders(World, mi);
						isDragging = false;
						YieldMouseFocus(mi);
						return true;
					}
				}

				if (multiClick)
				{
					var unit = World.ScreenMap.ActorsAtMouse(mousePos)
						.WithHighestSelectionPriority(mousePos, mi.Modifiers);

					// Players to be included in the selection (the viewer or all players in "Disable shroud" / "All players" mode)
					var viewer = World.RenderPlayer ?? World.LocalPlayer;
					var isShroudDisabled = viewer == null || (World.RenderPlayer == null && World.LocalPlayer.Spectating);
					var isEveryone = viewer != null && viewer.NonCombatant && viewer.Spectating;
					var eligiblePlayers = isShroudDisabled || isEveryone ? World.Players : new[] { viewer };

					if (unit != null && eligiblePlayers.Contains(unit.Owner))
					{
						var s = unit.TraitOrDefault<ISelectable>();
						if (s != null)
						{
							// Select actors on the screen that have the same selection class as the actor under the mouse cursor
							var newSelection = SelectActorsOnScreen(World, worldRenderer, new HashSet<string> { s.Class }, eligiblePlayers);

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
					if (isDragging && (uog.ClearSelectionOnLeftClick || IsValidDragbox))
					{
						var newSelection = SelectActorsInBoxWithDeadzone(World, dragStart, mousePos, mi.Modifiers);
						World.Selection.Combine(World, newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == mousePos);
					}
				}

				World.CancelInputMode();

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
			foreach (var o in orders)
			{
				if (o == null)
					continue;

				if (!flashed && !o.SuppressVisualFeedback)
				{
					var visualTarget = o.VisualFeedbackTarget.Type != TargetType.Invalid ? o.VisualFeedbackTarget : o.Target;
					if (visualTarget.Type == TargetType.Actor)
					{
						world.AddFrameEndTask(w => w.Add(new FlashTarget(visualTarget.Actor)));
						flashed = true;
					}
					else if (visualTarget.Type == TargetType.FrozenActor)
					{
						visualTarget.FrozenActor.Flash();
						flashed = true;
					}
					else if (visualTarget.Type == TargetType.Terrain)
					{
						world.AddFrameEndTask(w => w.Add(new SpriteAnnotation(visualTarget.CenterPosition, world, "moveflsh", "idle", "moveflash")));
						flashed = true;
					}
				}

				world.IssueOrder(o);
			}
		}

		public override string GetCursor(int2 screenPos)
		{
			return Sync.RunUnsynced(Game.Settings.Debug.SyncCheckUnsyncedCode, World, () =>
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
			if (e.Event == KeyInputEvent.Down)
			{
				// Players to be included in the selection (the viewer or all players in "Disable shroud" / "All players" mode)
				var viewer = World.RenderPlayer ?? World.LocalPlayer;
				var isShroudDisabled = viewer == null || (World.RenderPlayer == null && World.LocalPlayer.Spectating);
				var isEveryone = viewer != null && viewer.NonCombatant && viewer.Spectating;
				var eligiblePlayers = isShroudDisabled || isEveryone ? World.Players : new[] { viewer };

				if (SelectAllKey.IsActivatedBy(e) && !World.IsGameOver)
				{
					// Select actors on the screen which belong to the current player(s)
					var ownUnitsOnScreen = SelectActorsOnScreen(World, worldRenderer, null, eligiblePlayers).SubsetWithHighestSelectionPriority(e.Modifiers).ToList();

					// Check if selecting actors on the screen has selected new units
					if (ownUnitsOnScreen.Count > World.Selection.Actors.Count())
						Game.AddSystemLine("Selected across screen");
					else
					{
						// Select actors in the world that have highest selection priority
						ownUnitsOnScreen = SelectActorsInWorld(World, null, eligiblePlayers).SubsetWithHighestSelectionPriority(e.Modifiers).ToList();
						Game.AddSystemLine("Selected across map");
					}

					World.Selection.Combine(World, ownUnitsOnScreen, false, false);
				}
				else if (SelectSameTypeKey.IsActivatedBy(e) && !World.IsGameOver)
				{
					if (!World.Selection.Actors.Any())
						return false;

					var ownedActors = World.Selection.Actors
						.Where(x => !x.IsDead && eligiblePlayers.Contains(x.Owner))
						.ToList();

					if (!ownedActors.Any())
						return false;

					// Get all the selected actors' selection classes
					var selectedClasses = ownedActors
						.Select(a => a.Trait<ISelectable>().Class)
						.ToHashSet();

					// Select actors on the screen that have the same selection class as one of the already selected actors
					var newSelection = SelectActorsOnScreen(World, worldRenderer, selectedClasses, eligiblePlayers).ToList();

					// Check if selecting actors on the screen has selected new units
					if (newSelection.Count > World.Selection.Actors.Count())
						Game.AddSystemLine("Selected across screen");
					else
					{
						// Select actors in the world that have the same selection class as one of the already selected actors
						newSelection = SelectActorsInWorld(World, selectedClasses, eligiblePlayers).ToList();
						Game.AddSystemLine("Selected across map");
					}

					World.Selection.Combine(World, newSelection, true, false);
				}
			}

			return false;
		}

		static IEnumerable<Actor> SelectActorsOnScreen(World world, WorldRenderer wr, IEnumerable<string> selectionClasses, IEnumerable<Player> players)
		{
			var actors = world.ScreenMap.ActorsInMouseBox(wr.Viewport.TopLeft, wr.Viewport.BottomRight).Select(a => a.Actor);
			return SelectActorsByOwnerAndSelectionClass(actors, players, selectionClasses);
		}

		static IEnumerable<Actor> SelectActorsInWorld(World world, IEnumerable<string> selectionClasses, IEnumerable<Player> players)
		{
			return SelectActorsByOwnerAndSelectionClass(world.Actors.Where(a => a.IsInWorld), players, selectionClasses);
		}

		static IEnumerable<Actor> SelectActorsByOwnerAndSelectionClass(IEnumerable<Actor> actors, IEnumerable<Player> owners, IEnumerable<string> selectionClasses)
		{
			return actors.Where(a =>
			{
				if (!owners.Contains(a.Owner))
					return false;

				var s = a.TraitOrDefault<ISelectable>();

				// selectionClasses == null means that units, that meet all other criteria, get selected
				return s != null && (selectionClasses == null || selectionClasses.Contains(s.Class));
			});
		}

		static IEnumerable<Actor> SelectHighestPriorityActorAtPoint(World world, int2 a, Modifiers modifiers)
		{
			var selected = world.ScreenMap.ActorsAtMouse(a)
				.Where(x => x.Actor.Info.HasTraitInfo<ISelectableInfo>() && (x.Actor.Owner.IsAlliedWith(world.RenderPlayer) || !world.FogObscures(x.Actor)))
				.WithHighestSelectionPriority(a, modifiers);

			if (selected != null)
				yield return selected;
		}

		static IEnumerable<Actor> SelectActorsInBoxWithDeadzone(World world, int2 a, int2 b, Modifiers modifiers)
		{
			// For dragboxes that are too small, shrink the dragbox to a single point (point b)
			if ((a - b).Length <= Game.Settings.Game.SelectionDeadzone)
				a = b;

			if (a == b)
				return SelectHighestPriorityActorAtPoint(world, a, modifiers);

			return world.ScreenMap.ActorsInMouseBox(a, b)
				.Select(x => x.Actor)
				.Where(x => x.Info.HasTraitInfo<ISelectableInfo>() && (x.Owner.IsAlliedWith(world.RenderPlayer) || !world.FogObscures(x)))
				.SubsetWithHighestSelectionPriority(modifiers);
		}
	}
}
