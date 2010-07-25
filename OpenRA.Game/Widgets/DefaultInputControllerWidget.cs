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
using System.Linq;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA.Widgets
{
	class DefaultInputControllerWidget : Widget
	{
		public DefaultInputControllerWidget() : base()	{}
		protected DefaultInputControllerWidget(DefaultInputControllerWidget widget) : base(widget) {}
		public override void DrawInner( World world ) { }
		
		static internal bool scrollUp = false;
		static internal bool scrollDown = false;
		static internal bool scrollLeft = false;
		static internal bool scrollRight = false;
		
		// TODO: need a mechanism to say "i'll only handle this info if NOTHING else has"
		// For now, ensure that this widget recieves the input last or it will eat it
		float2 dragStart, dragEnd;
		public override bool HandleInputInner(MouseInput mi)
		{			
			var xy = Game.viewport.ViewToWorld(mi);
			var world = Game.world;
			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				dragStart = dragEnd = xy;
				Game.controller.ApplyOrders(world, xy, mi);
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Move)
				dragEnd = xy;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (Game.controller.orderGenerator is UnitOrderGenerator)
				{
					var newSelection = Game.world.SelectActorsInBox(Game.CellSize * dragStart, Game.CellSize * xy);
					Game.controller.selection.Combine(world, newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == xy);
				}

				dragStart = dragEnd = xy;
			}
			
			if (mi.Event == MouseInputEvent.Move &&
				(mi.Button == MouseButton.Middle || mi.Button == (MouseButton.Left | MouseButton.Right)))
				Game.viewport.Scroll(Widget.LastMousePos - mi.Location);
			

			if (mi.Button == MouseButton.None && mi.Event == MouseInputEvent.Move)
				dragStart = dragEnd = xy;

			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Down)
				Game.controller.ApplyOrders(world, xy, mi);

			Game.controller.dragStart = dragStart;
			Game.controller.dragEnd = dragEnd;
			return true;
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

				return Game.controller.orderGenerator.GetCursor( world, Game.viewport.ViewToWorld(mi).ToInt2(), mi );
			}
			finally
			{
				if( sync != world.SyncHash() )
					throw new InvalidOperationException( "Desync in InputControllerWidget.GetCursor" );
			}
		}

		public override bool LoseFocus (MouseInput mi)
		{
			scrollUp = scrollDown = scrollLeft = scrollRight = false;
			return base.LoseFocus(mi);
		}
		
		public override bool HandleKeyPressInner(KeyInput e)
		{
			// Take the input if *nothing* else is focused
			if (!Focused && Widget.SelectedWidget != null)
				return false;

			if (e.Event == KeyInputEvent.Down)
			{
				switch (e.KeyName)
				{
					case "up": scrollUp = true; break;
					case "down": scrollDown = true; break;
					case "left": scrollLeft = true; break;
					case "right": scrollRight = true; break;
				}
	
				if (e.KeyName.Length == 1 && char.IsDigit(e.KeyName[0]))
					Game.controller.selection.DoControlGroup(Game.world, e.KeyName[0] - '0', e.Modifiers);
				
				if (e.KeyChar == 08)
					GotoNextBase();
			}
			else
			{
				switch (e.KeyName)
				{
					case "up": scrollUp = false; break;
					case "down": scrollDown = false; break;
					case "left": scrollLeft = false; break;
					case "right": scrollRight = false; break;
				}
			}
			
			return true;
		}
		
		public override void Tick(World world)
		{
			
			if (scrollUp == true)
				Game.viewport.Scroll(new float2(0, -10));
			if (scrollRight == true)
				Game.viewport.Scroll(new float2(10, 0));
			if (scrollDown == true)
				Game.viewport.Scroll(new float2(0, 10));
			if (scrollLeft == true)
				Game.viewport.Scroll(new float2(-10, 0));
		}
		
		public void GotoNextBase()
		{
			var bases = Game.world.Queries.OwnedBy[Game.world.LocalPlayer].WithTrait<BaseBuilding>().ToArray();
			if (!bases.Any()) return;

			var next = bases
				.Select( b => b.Actor )
				.SkipWhile(b => Game.controller.selection.Actors.Contains(b))
				.Skip(1)
				.FirstOrDefault();

			if (next == null)
				next = bases.Select(b => b.Actor).First();

			Game.controller.selection.Combine(Game.world, new Actor[] { next }, false, true);
			Game.viewport.Center(Game.controller.selection.Actors);
		}
		
		public override Widget Clone() { return new DefaultInputControllerWidget(this); }
	}
}