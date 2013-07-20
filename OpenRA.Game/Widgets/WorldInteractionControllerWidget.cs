#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
		public WorldInteractionControllerWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
		}

		public override void Draw()
		{
			var selbox = SelectionBox;
			if (selbox == null)
			{
				foreach (var u in SelectActorsInBox(world, dragStart, dragStart, _ => true))
					worldRenderer.DrawRollover(u);

				return;
			}

			Game.Renderer.WorldLineRenderer.DrawRect(selbox.Value.First.ToFloat2(), selbox.Value.Second.ToFloat2(), Color.White);
			foreach (var u in SelectActorsInBox(world, selbox.Value.First, selbox.Value.Second, _ => true))
				worldRenderer.DrawRollover(u);
		}

		PPos dragStart, dragEnd;

		public override bool HandleMouseInput(MouseInput mi)
		{
			var xy = Game.viewport.ViewToWorldPx(mi);

			var UseClassicMouseStyle = Game.Settings.Game.UseClassicMouseStyle;

			var HasBox = (SelectionBox != null) ? true : false;
			var MultiClick = (mi.MultiTapCount >= 2) ? true : false;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!TakeFocus(mi))
					return false;
				
				dragStart = dragEnd = xy;

				//place buildings
				if (!UseClassicMouseStyle || (UseClassicMouseStyle && !world.Selection.Actors.Any()) )
					ApplyOrders(world, xy, mi);
			}
			
			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Move)
				dragEnd = xy;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (UseClassicMouseStyle && Focused)
				{
					//order units around
					if (!HasBox && world.Selection.Actors.Any() && !MultiClick)
					{
						ApplyOrders(world, xy, mi);
						LoseFocus(mi);
						return true;
					}
				}

				if (world.OrderGenerator is UnitOrderGenerator)
				{
					if (MultiClick)
					{
						var unit = SelectActorsInBox(world, xy, xy, _ => true).FirstOrDefault();

						var visibleWorld = Game.viewport.ViewBounds(world);
						var topLeft = Game.viewport.ViewToWorldPx(new int2(visibleWorld.Left, visibleWorld.Top));
						var bottomRight = Game.viewport.ViewToWorldPx(new int2(visibleWorld.Right, visibleWorld.Bottom));
						var newSelection2= SelectActorsInBox(world, topLeft, bottomRight, 
						                                      a => unit != null && a.Info.Name == unit.Info.Name && a.Owner == unit.Owner);
							
						world.Selection.Combine(world, newSelection2, true, false);
					}
					else
					{
						var newSelection = SelectActorsInBox(world, dragStart, xy, _ => true);
						world.Selection.Combine(world, newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == xy);
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

				if (!HasBox)	// don't issue orders while selecting
					ApplyOrders(world, xy, mi);
			}
			
			return true;
		}


		public Pair<PPos, PPos>? SelectionBox
		{
			get
			{
				if (dragStart == dragEnd) return null;
				return Pair.New(dragStart, dragEnd);
			}
		}

		public void ApplyOrders(World world, PPos xy, MouseInput mi)
		{
			if (world.OrderGenerator == null) return;

			var orders = world.OrderGenerator.Order(world, xy.ToCPos(), mi).ToArray();
			orders.Do(o => world.IssueOrder(o));

			world.PlayVoiceForOrders(orders);
		}

		public override string GetCursor(int2 pos)
		{
			return Sync.CheckSyncUnchanged(world, () =>
			{
				if (SelectionBox != null)
					return null;	/* always show an arrow while selecting */

				var mi = new MouseInput
				{
					Location = pos,
					Button = Game.mouseButtonPreference.Action,
					Modifiers = Game.GetModifierKeys()
				};

				// TODO: fix this up.
				return world.OrderGenerator.GetCursor(world, Game.viewport.ViewToWorld(mi), mi);
			} );
		}

		public override bool HandleKeyPress(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				if (e.KeyName.Length == 1 && char.IsDigit(e.KeyName[0]))
				{
					world.Selection.DoControlGroup(world, e.KeyName[0] - '0', e.Modifiers, e.MultiTapCount);
					return true;
				}

				// Disable pausing for spectators
				else if (e.KeyName == Game.Settings.Keys.PauseKey && world.LocalPlayer != null)
					world.SetPauseState(!world.Paused);
			}
			return false;
		}

		static readonly Actor[] NoActors = {};
		IEnumerable<Actor> SelectActorsInBox(World world, PPos a, PPos b, Func<Actor, bool> cond)
		{
			return world.FindActorsInBox(a.ToWPos(0), b.ToWPos(0))
				.Where(x => x.HasTrait<Selectable>() && x.Trait<Selectable>().Info.Selectable && !world.FogObscures(x) && cond(x))
				.GroupBy(x => x.GetSelectionPriority())
				.OrderByDescending(g => g.Key)
				.Select(g => g.AsEnumerable())
				.DefaultIfEmpty(NoActors)
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