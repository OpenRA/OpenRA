#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
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
		static readonly Actor[] NoActors = { };

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

		public override void Draw()
		{
			if (!IsDragging)
			{
				foreach (var u in SelectActorsInBoxWithDeadzone(World, lastMousePosition, lastMousePosition, _ => true))
					worldRenderer.DrawRollover(u);

				return;
			}

			var selbox = SelectionBox;
			Game.Renderer.WorldLineRenderer.DrawRect(selbox.Value.First.ToFloat2(), selbox.Value.Second.ToFloat2(), Color.White);
			foreach (var u in SelectActorsInBoxWithDeadzone(World, selbox.Value.First, selbox.Value.Second, _ => true))
				worldRenderer.DrawRollover(u);
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
							if (!(World.ScreenMap.ActorsAt(xy).Where(x => x.HasTrait<Selectable>() && x.Trait<Selectable>().Info.Selectable &&
								(x.Owner.IsAlliedWith(World.RenderPlayer) || !World.FogObscures(x))).Any() && !mi.Modifiers.HasModifier(Modifiers.Ctrl) &&
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
							var newSelection2 = SelectActorsInBox(World, worldRenderer.Viewport.TopLeft, worldRenderer.Viewport.BottomRight,
								a => a.Owner == unit.Owner && a.Info.Name == unit.Info.Name);

							World.Selection.Combine(World, newSelection2, true, false);
						}
					}
					else if (dragStart.HasValue)
					{
						var newSelection = SelectActorsInBoxWithDeadzone(World, dragStart.Value, xy, _ => true);
						World.Selection.Combine(World, newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == xy);
					}
				}

				dragStart = dragEnd = null;
				YieldMouseFocus(mi);
			}

			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Down)
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
			if (e.Event == KeyInputEvent.Down)
			{
				var key = Hotkey.FromKeyInput(e);

				if (key == Game.Settings.Keys.PauseKey && World.LocalPlayer != null) // Disable pausing for spectators
					World.SetPauseState(!World.Paused);
				else if (key == Game.Settings.Keys.SelectAllUnitsKey && World.LocalPlayer != null)
				{
					var ownUnitsOnScreen = SelectActorsInBox(World, worldRenderer.Viewport.TopLeft, worldRenderer.Viewport.BottomRight,
						a => a.Owner == World.LocalPlayer);
					World.Selection.Combine(World, ownUnitsOnScreen, false, false);
				}
				else if (key == Game.Settings.Keys.SelectUnitsByTypeKey && World.LocalPlayer != null)
				{
					var selectedTypes = World.Selection.Actors
						.Where(x => x.Owner == World.LocalPlayer)
						.Select(a => a.Info);

					Func<Actor, bool> cond = a => a.Owner == World.LocalPlayer && selectedTypes.Contains(a.Info);
					var tl = worldRenderer.Viewport.TopLeft;
					var br = worldRenderer.Viewport.BottomRight;
					var newSelection = SelectActorsInBox(World, tl, br, cond);

					if (newSelection.Count() > selectedTypes.Count())
						Game.Debug("Selected across screen");
					else
					{
						newSelection = World.ActorMap.ActorsInWorld().Where(cond);
						Game.Debug("Selected across map");
					}

					World.Selection.Combine(World, newSelection, true, false);
				}
				else if (key == Game.Settings.Keys.ToggleStatusBarsKey)
					return ToggleStatusBars();
				else if (key == Game.Settings.Keys.TogglePixelDoubleKey)
					return TogglePixelDouble();
			}

			return false;
		}

		static IEnumerable<Actor> SelectActorsInBoxWithDeadzone(World world, int2 a, int2 b, Func<Actor, bool> cond)
		{
			if (a == b || (a - b).Length > Game.Settings.Game.SelectionDeadzone)
				return SelectActorsInBox(world, a, b, cond);
			else
				return SelectActorsInBox(world, b, b, cond);
		}

		static IEnumerable<Actor> SelectActorsInBox(World world, int2 a, int2 b, Func<Actor, bool> cond)
		{
			return world.ScreenMap.ActorsInBox(a, b)
				.Where(x => x.HasTrait<Selectable>() && x.Trait<Selectable>().Info.Selectable && (x.Owner.IsAlliedWith(world.RenderPlayer) || !world.FogObscures(x)) && cond(x))
				.GroupBy(x => x.GetSelectionPriority())
				.OrderByDescending(g => g.Key)
				.Select(g => g.AsEnumerable())
				.DefaultIfEmpty(NoActors)
				.FirstOrDefault();
		}

		bool ToggleStatusBars()
		{
			Game.Settings.Game.AlwaysShowStatusBars ^= true;
			return true;
		}

		bool TogglePixelDouble()
		{
			Game.Settings.Graphics.PixelDouble ^= true;
			worldRenderer.Viewport.Zoom = Game.Settings.Graphics.PixelDouble ? 2 : 1;
			return true;
		}
	}

	static class PriorityExts
	{
		const int PriorityRange = 30;

		public static int GetSelectionPriority(this Actor a)
		{
			var basePriority = a.Info.Traits.Get<SelectableInfo>().Priority;
			var lp = a.World.LocalPlayer;

			if (a.Owner == lp || lp == null)
				return basePriority;

			switch (lp.Stances[a.Owner])
			{
				case Stance.Ally: return basePriority - PriorityRange;
				case Stance.Neutral: return basePriority - 2 * PriorityRange;
				case Stance.Enemy: return basePriority - 3 * PriorityRange;

				default:
					throw new InvalidOperationException();
			}
		}
	}
}