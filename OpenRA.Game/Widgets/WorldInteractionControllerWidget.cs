#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
		readonly World world;
		readonly WorldRenderer worldRenderer;
		[ObjectCreator.UseCtor]
		public WorldInteractionControllerWidget([ObjectCreator.Param] World world, [ObjectCreator.Param] WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
		}
		
		public override void DrawInner()
		{
			foreach( var u in SelectActorsInBox(world, Game.CellSize * dragStart, Game.CellSize * dragStart ))
				worldRenderer.DrawRollover( u );

			var selbox = SelectionBox;
			if (selbox == null) return;

			var a = selbox.Value.First;
			var b = new float2(selbox.Value.Second.X - a.X, 0);
			var c = new float2(0, selbox.Value.Second.Y - a.Y);

			Game.Renderer.LineRenderer.DrawLine(a, a + b, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(a + b, a + b + c, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(a + b + c, a + c, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(a, a + c, Color.White, Color.White);
			
			foreach (var u in SelectActorsInBox(world, selbox.Value.First, selbox.Value.Second))
				worldRenderer.DrawSelectionBox(u, Color.Yellow);
		}
		
		float2 dragStart, dragEnd;

		// TODO: WorldInteractionController doesn't support delegate methods for mouse input
		public override bool HandleMouseInput(MouseInput mi)
		{			
			var xy = Game.viewport.ViewToWorld(mi);
			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				dragStart = dragEnd = xy;
				ApplyOrders(world, xy, mi);
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Move)
				dragEnd = xy;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (world.OrderGenerator is UnitOrderGenerator)
				{
					var newSelection = SelectActorsInBox(world, Game.CellSize * dragStart, Game.CellSize * xy);
					world.Selection.Combine(world, newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == xy);
				}

				dragStart = dragEnd = xy;
			}

			if (mi.Button == MouseButton.None && mi.Event == MouseInputEvent.Move)
				dragStart = dragEnd = xy;

			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Down)
				ApplyOrders(world, xy, mi);
			
			return true;
		}
		
		public Pair<float2, float2>? SelectionBox
		{
			get
			{
				if (dragStart == dragEnd) return null;
				return Pair.New(Game.CellSize * dragStart, Game.CellSize * dragEnd);
			}
		}
		
		public void ApplyOrders(World world, float2 xy, MouseInput mi)
		{
			if (world.OrderGenerator == null) return;

			var orders = world.OrderGenerator.Order(world, xy.ToInt2(), mi).ToArray();
			orders.Do( o => world.IssueOrder( o ) );

			world.PlayVoiceForOrders(orders);
		}
		
		public override string GetCursor(int2 pos)
		{
			return Sync.CheckSyncUnchanged( world, () =>
			{
				var mi = new MouseInput
				{
					Location = pos,
					Button = MouseButton.Right,
					Modifiers = Game.GetModifierKeys()
				};

				return world.OrderGenerator.GetCursor( world, Game.viewport.ViewToWorld(mi).ToInt2(), mi );
			} );
		}

		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				if (e.KeyName.Length == 1 && char.IsDigit(e.KeyName[0]))
				{
					world.Selection.DoControlGroup(world, e.KeyName[0] - '0', e.Modifiers);
					return true;
				}

				bool handled = false;

				foreach (var t in world.WorldActor.TraitsImplementing<INotifyKeyPress>())
					handled = (t.KeyPressed(world.WorldActor, e)) ? true : handled;

				if (handled) return true;
			}
			return false;
		}
		
		IEnumerable<Actor> SelectActorsInBox(World world, float2 a, float2 b)
		{
			return world.FindUnits(a, b)
				.Where( x => x.HasTrait<Selectable>() && world.LocalShroud.IsVisible(x) )
				.GroupBy(x => (x.Owner == world.LocalPlayer) ? x.Info.Traits.Get<SelectableInfo>().Priority : 0)
				.OrderByDescending(g => g.Key)
				.Select( g => g.AsEnumerable() )
				.DefaultIfEmpty( new Actor[] {} )
				.FirstOrDefault();
		}
	}
}