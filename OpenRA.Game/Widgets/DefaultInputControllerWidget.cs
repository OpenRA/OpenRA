#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Orders;
using OpenRA.Traits;
using OpenRA.FileFormats;

namespace OpenRA.Widgets
{
	class DefaultInputControllerWidget : Widget
	{
		public DefaultInputControllerWidget() : base()	{}
		protected DefaultInputControllerWidget(DefaultInputControllerWidget widget) : base(widget) {}
		
		public override void DrawInner( World world )
		{
			var selbox = SelectionBox;
			if (selbox == null) return;

			var a = selbox.Value.First;
			var b = new float2(selbox.Value.Second.X - a.X, 0);
			var c = new float2(0, selbox.Value.Second.Y - a.Y);

			Game.Renderer.LineRenderer.DrawLine(a, a + b, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(a + b, a + b + c, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(a + b + c, a + c, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(a, a + c, Color.White, Color.White);

			foreach (var u in world.SelectActorsInBox(selbox.Value.First, selbox.Value.Second))
				world.WorldRenderer.DrawSelectionBox(u, Color.Yellow);
			
			Game.Renderer.LineRenderer.Flush();
		}
		
		float2 dragStart, dragEnd;
		public override bool HandleInputInner(MouseInput mi)
		{			
			var xy = Game.viewport.ViewToWorld(mi);
			var world = Game.world;
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
					var newSelection = Game.world.SelectActorsInBox(Game.CellSize * dragStart, Game.CellSize * xy);
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
			Game.orderManager.IssueOrders( orders );
			
			// Find an actor with a phrase to say
			var done = false;
			foreach (var o in orders)
			{
				foreach (var v in o.Subject.traits.WithInterface<IOrderVoice>())
				{
					if (Sound.PlayVoice(v.VoicePhraseForOrder(o.Subject, o), o.Subject))
					{
						done = true;
						break;
					}
				}
				if (done) break;
			}
		}
		
		public override string GetCursor(int2 pos)
		{
			var world = Game.world;
			int sync = world.SyncHash();
			try
			{
				if (!world.GameHasStarted)
					return "default";

				var mi = new MouseInput
				{
					Location = pos,
					Button = MouseButton.Right,
					Modifiers = Game.GetModifierKeys()
				};

				return Game.world.OrderGenerator.GetCursor( world, Game.viewport.ViewToWorld(mi).ToInt2(), mi );
			}
			finally
			{
				if( sync != world.SyncHash() )
					throw new InvalidOperationException( "Desync in InputControllerWidget.GetCursor" );
			}
		}

		public override bool HandleKeyPressInner(KeyInput e)
		{
			if (e.Event == KeyInputEvent.Down)
			{
				if (e.KeyName.Length == 1 && char.IsDigit(e.KeyName[0]))
				{
					Game.world.Selection.DoControlGroup(Game.world, e.KeyName[0] - '0', e.Modifiers);
					return true;
				}

				if (e.KeyChar == '\b')
				{
					GotoNextBase();
					return true;
				}
			}
			return false;
		}
		
		public void GotoNextBase()
		{
			var world = Game.world;
			var bases = world.Queries.OwnedBy[world.LocalPlayer].WithTrait<BaseBuilding>().ToArray();
			if (!bases.Any()) return;

			var next = bases
				.Select( b => b.Actor )
				.SkipWhile(b => world.Selection.Actors.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = bases.Select(b => b.Actor).First();

			world.Selection.Combine(world, new Actor[] { next }, false, true);
			Game.viewport.Center(world.Selection.Actors);
		}
		
		public override Widget Clone() { return new DefaultInputControllerWidget(this); }
	}
}