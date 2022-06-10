#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class WorldInteractionControllerWidget : Widget
	{
		protected readonly World World;
		readonly WorldRenderer worldRenderer;
		readonly Color normalSelectionColor;
		readonly Color altSelectionColor;
		readonly Color ctrlSelectionColor;

		public readonly string ClickSound = ChromeMetrics.Get<string>("ClickSound");
		public readonly string ClickDisabledSound = ChromeMetrics.Get<string>("ClickDisabledSound");

		int2 dragStart, mousePos;
		bool isDragging = false;

		bool IsValidDragbox => isDragging && (dragStart - mousePos).Length > Game.Settings.Game.SelectionDeadzone;

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
				rollover = SelectionUtils.SelectActorsInBoxWithDeadzone(World, dragStart, mousePos, modifiers);
			}
			else
			{
				// Render actors under the mouse pointer
				rollover = SelectionUtils.SelectActorsInBoxWithDeadzone(World, mousePos, mousePos, modifiers);
			}

			worldRenderer.World.Selection.SetRollover(rollover);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			mousePos = worldRenderer.Viewport.ViewToWorldPx(mi.Location);

			var useClassicMouseStyle = Game.Settings.Game.UseClassicMouseStyle;

			var multiClick = mi.MultiTapCount >= 2;

			if (!(World.OrderGenerator is UnitOrderGenerator uog))
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

					var eligiblePlayers = SelectionUtils.GetPlayersToIncludeInSelection(World);

					if (unit != null && eligiblePlayers.Contains(unit.Owner))
					{
						var s = unit.TraitOrDefault<ISelectable>();
						if (s != null)
						{
							// Select actors on the screen that have the same selection class as the actor under the mouse cursor
							var newSelection = SelectionUtils.SelectActorsOnScreen(World, worldRenderer, new HashSet<string> { s.Class }, eligiblePlayers);

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
						var newSelection = SelectionUtils.SelectActorsInBoxWithDeadzone(World, dragStart, mousePos, mi.Modifiers);
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
			orders.PlayVoiceForOrders();

			var flashed = false;
			foreach (var o in orders)
			{
				if (o == null)
					continue;

				if (!flashed && !o.SuppressVisualFeedback)
				{
					var visualTarget = o.VisualFeedbackTarget.Type != TargetType.Invalid ? o.VisualFeedbackTarget : o.Target;

					foreach (var notifyOrderIssued in world.WorldActor.TraitsImplementing<INotifyOrderIssued>())
						flashed = notifyOrderIssued.OrderIssued(world, visualTarget);
				}

				world.IssueOrder(o);
			}
		}

		public override string GetCursor(int2 screenPos)
		{
			return Sync.RunUnsynced(World, () =>
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
	}
}
