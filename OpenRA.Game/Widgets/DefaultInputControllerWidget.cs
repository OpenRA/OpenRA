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
using OpenRA.Orders;

namespace OpenRA.Widgets
{
	class DefaultInputControllerWidget : Widget
	{
		public DefaultInputControllerWidget() : base()	{}
		protected DefaultInputControllerWidget(DefaultInputControllerWidget widget) : base(widget) {}
		public override void DrawInner( World world ) { }

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
					Modifiers = Game.controller.GetModifiers()
				};

				return Game.controller.orderGenerator.GetCursor( world, Game.viewport.ViewToWorld(mi).ToInt2(), mi );
			}
			finally
			{
				if( sync != world.SyncHash() )
					throw new InvalidOperationException( "Desync in InputControllerWidget.GetCursor" );
			}
		}

		public override Widget Clone() { return new DefaultInputControllerWidget(this); }
	}
}