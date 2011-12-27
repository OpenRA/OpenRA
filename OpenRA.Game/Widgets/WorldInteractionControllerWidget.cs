#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Widgets
{
	public class WorldInteractionControllerWidget : Widget
	{
		protected readonly World world;
		readonly WorldRenderer worldRenderer;

		[ObjectCreator.UseCtor]
		public WorldInteractionControllerWidget([ObjectCreator.Param] World world, [ObjectCreator.Param] WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
		}

		public override void Draw()
		{
			var selbox = SelectionBox;
            if (selbox == null)
            {
                foreach (var u in SelectActorsInBox(world, dragStart, dragStart))
                    worldRenderer.DrawRollover(u);

                return;
            }

			Game.Renderer.WorldLineRenderer.DrawRect( selbox.Value.First, selbox.Value.Second, Color.White );
            foreach (var u in SelectActorsInBox(world, selbox.Value.First, selbox.Value.Second))
                worldRenderer.DrawRollover(u);
		}

		int2 dragStart, dragEnd;

		public override bool HandleMouseInput(MouseInput mi)
		{
			var xy = Game.viewport.ViewToWorldPx(mi);

			var UseClassicMouseStyle = Game.Settings.Keys.UseClassicMouseStyle;

			bool UnitsUnderCursor = (world.FindUnitsAtMouse(mi.Location).FirstOrDefault() != null) ? true : false;
			bool Box = (SelectionBox != null) ? true : false;
			bool MultiClick = (mi.MultiTapCount >= 2) ? true : false;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!TakeFocus(mi))
					return false;

				dragStart = dragEnd = xy;

				ApplyOrders(world, xy, mi);
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Move)
				dragEnd = xy;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (world.OrderGenerator is UnitOrderGenerator)
				{
					if (!UseClassicMouseStyle || (UseClassicMouseStyle && (UnitsUnderCursor || Box)))
					{
						var newSelection = SelectActorsInBox(world, dragStart, xy);
						world.Selection.Combine(world, newSelection,
										mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == xy);
					}

					if (MultiClick)
					{
						var unit = world.FindUnitsAtMouse(mi.Location).FirstOrDefault();
						Rectangle visibleWorld = Game.viewport.ViewBounds(world);
						var newSelection = world.FindUnits(Game.viewport
									.ViewToWorldPx(new int2(visibleWorld.Left, visibleWorld.Top)),
									Game.viewport.ViewToWorldPx(new int2(visibleWorld.Right,
									visibleWorld.Bottom)))
									.Where(a => a.HasTrait<Selectable>()
										&& a.World.LocalShroud.IsVisible(a)
										&& unit != null
										&& a.Info.Name == unit.Info.Name
										&& a.Owner == unit.Owner);

						world.Selection.Combine(world, newSelection, true, false);
					}

					if (UseClassicMouseStyle && !UnitsUnderCursor && !Box)
					{
						ApplyOrders(world, xy, mi);
					}
				}

				dragStart = dragEnd = xy;
				LoseFocus(mi);
			}

			if (mi.Button == MouseButton.None && mi.Event == MouseInputEvent.Move)
				dragStart = dragEnd = xy;

			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Down)
			{
				if (UseClassicMouseStyle)
					world.Selection.Clear();

				if (!Box)	/* don't issue orders while selecting */
					ApplyOrders(world, xy, mi);
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

		public void ApplyOrders(World world, int2 xy, MouseInput mi)
		{
			if (world.OrderGenerator == null) return;

			var orders = world.OrderGenerator.Order(world, Traits.Util.CellContaining(xy), mi).ToArray();
			orders.Do( o => world.IssueOrder( o ) );

			world.PlayVoiceForOrders(orders);
		}

		public override string GetCursor(int2 pos)
		{
			return Sync.CheckSyncUnchanged( world, () =>
			{
				if (SelectionBox != null)
					return null;	/* always show an arrow while selecting */

				var mi = new MouseInput
				{
					Location = pos,
					Button = Game.Settings.Keys.UseClassicMouseStyle ? MouseButton.Left : MouseButton.Right,
					Modifiers = Game.GetModifierKeys()
				};

				// TODO: fix this up.
				return world.OrderGenerator.GetCursor( world, Game.viewport.ViewToWorld(mi).ToInt2(), mi );
			} );
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				if (e.KeyName.Length == 1 && char.IsDigit(e.KeyName[0]))
				{
					world.Selection.DoControlGroup(world, e.KeyName[0] - '0', e.Modifiers);
					return true;
				}

				bool handled = false;

				if (handled) return true;
			}
			return false;
		}

        static readonly Actor[] NoActors = {};
		IEnumerable<Actor> SelectActorsInBox(World world, int2 a, int2 b)
		{
			return world.FindUnits(a, b)
				.Where( x => x.HasTrait<Selectable>() && world.LocalShroud.IsVisible(x) )
				.GroupBy(x => x.GetSelectionPriority())
				.OrderByDescending(g => g.Key)
				.Select( g => g.AsEnumerable() )
				.DefaultIfEmpty( NoActors )
				.FirstOrDefault();
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