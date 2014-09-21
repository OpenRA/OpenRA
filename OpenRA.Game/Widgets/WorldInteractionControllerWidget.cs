#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
		int2 dragStart, dragEnd;

		[ObjectCreator.UseCtor]
		public WorldInteractionControllerWidget(World world, WorldRenderer worldRenderer)
		{
			this.World = world;
			this.worldRenderer = worldRenderer;
		}

		public override void Draw()
		{
			var selbox = SelectionBox;
			if (selbox == null)
			{
				foreach (var u in SelectActorsInBox(World, dragStart, dragStart, _ => true))
					worldRenderer.DrawRollover(u);

				return;
			}

			Game.Renderer.WorldLineRenderer.DrawRect(selbox.Value.First.ToFloat2(), selbox.Value.Second.ToFloat2(), Color.White);
			foreach (var u in SelectActorsInBox(World, selbox.Value.First, selbox.Value.Second, _ => true))
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

				dragStart = dragEnd = xy;

				// place buildings
				if (!useClassicMouseStyle || !World.Selection.Actors.Any())
					ApplyOrders(World, xy, mi);
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Move)
				dragEnd = xy;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (useClassicMouseStyle && HasMouseFocus)
				{
					// order units around
					if (!hasBox && World.Selection.Actors.Any() && !multiClick)
					{
						ApplyOrders(World, xy, mi);
						YieldMouseFocus(mi);
						return true;
					}
				}

				if (World.OrderGenerator is UnitOrderGenerator)
				{
					if (multiClick)
					{
						var unit = World.ScreenMap.ActorsAt(xy)
							.WithHighestSelectionPriority();

						var newSelection2 = SelectActorsInBox(World, worldRenderer.Viewport.TopLeft, worldRenderer.Viewport.BottomRight, 
							a => unit != null && a.Info.Name == unit.Info.Name && a.Owner == unit.Owner);

						World.Selection.Combine(World, newSelection2, true, false);
					}
					else
					{
						var newSelection = SelectActorsInBox(World, dragStart, xy, _ => true);
						World.Selection.Combine(World, newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == xy);
					}
				}

				dragStart = dragEnd = xy;
				YieldMouseFocus(mi);
			}

			if (mi.Button == MouseButton.None && mi.Event == MouseInputEvent.Move)
				dragStart = dragEnd = xy;

			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Down)
			{
				if (useClassicMouseStyle)
					World.Selection.Clear();

				if (!hasBox) // don't issue orders while selecting
					ApplyOrders(World, xy, mi);
			}

			return true;
		}

		public Pair<int2, int2>? SelectionBox
		{
			get
			{
				if (dragStart == dragEnd) return null;
				return Pair.New(dragStart, dragEnd);
			}
		}

		void ApplyOrders(World world, int2 xy, MouseInput mi)
		{
			if (world.OrderGenerator == null)
				return;

			var pos = worldRenderer.Position(xy);
			var orders = world.OrderGenerator.Order(world, world.Map.CellContaining(pos), mi).ToArray();
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
						world.AddFrameEndTask(w => w.Add(new SpriteEffect(worldRenderer.Position(worldRenderer.Viewport.ViewToWorldPx(mi.Location)), world, "moveflsh", "moveflash")));
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

				var xy = worldRenderer.Viewport.ViewToWorldPx(screenPos);
				var pos = worldRenderer.Position(xy);
				var cell = World.Map.CellContaining(pos);

				var mi = new MouseInput
				{
					Location = screenPos,
					Button = Game.mouseButtonPreference.Action,
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