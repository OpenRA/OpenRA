#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Orders;
using OpenRA.Traits;

namespace OpenRA
{
	public class Controller : IHandleInput
	{
		public IOrderGenerator orderGenerator = new UnitOrderGenerator();
		public Selection selection = new Selection();

		public void CancelInputMode() { orderGenerator = new UnitOrderGenerator(); }

		public bool ToggleInputMode<T>() where T : IOrderGenerator, new()
		{
			if (orderGenerator is T)
			{
				CancelInputMode();
				return false;
			}
			else
			{
				orderGenerator = new T();
				return true;
			}
		}

		void ApplyOrders(World world, float2 xy, MouseInput mi)
		{
			if (orderGenerator == null) return;

			var orders = orderGenerator.Order(world, xy.ToInt2(), mi).ToArray();
			Game.orderManager.IssueOrders( orders );

			var voicedActor = orders.Select(o => o.Subject)
				.FirstOrDefault(a => a.Owner == world.LocalPlayer && a.traits.Contains<Unit>());

			var isMove = orders.Any(o => o.OrderString == "Move");
			var isAttack = orders.Any( o => o.OrderString == "Attack" );

			if (voicedActor != null)
			{
				
				if(voicedActor.traits.GetOrDefault<IMovement>().CanEnterCell(xy.ToInt2()))
					Sound.PlayVoice(isAttack ? "Attack" : "Move", voicedActor);

				if (isMove)
					world.Add(new Effects.MoveFlash(world, Game.CellSize * xy));
			}
		}

		float2 dragStart, dragEnd;
		public bool HandleInput(World world, MouseInput mi)
		{
			var xy = Game.viewport.ViewToWorld(mi);

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!(orderGenerator is PlaceBuildingOrderGenerator))
					dragStart = dragEnd = xy;
				ApplyOrders(world, xy, mi);
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Move)
				dragEnd = xy;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (orderGenerator is UnitOrderGenerator)
				{
					var newSelection = world.SelectActorsInBox(Game.CellSize * dragStart, Game.CellSize * xy);
					selection.Combine(world, newSelection, mi.Modifiers.HasModifier(Modifiers.Shift), dragStart == xy);
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

		public float2 MousePosition { get { return dragEnd; } }
		Modifiers modifiers;

		public string ChooseCursor( World world )
		{
			int sync = world.SyncHash();

			try
			{
				if (!world.GameHasStarted)
					return "default";

				var mi = new MouseInput
				{
					Location = ( Game.CellSize * MousePosition - Game.viewport.Location ).ToInt2(),
					Button = MouseButton.Right,
					Modifiers = modifiers
				};

				return orderGenerator.GetCursor( world, MousePosition.ToInt2(), mi );
			}
			finally
			{
				if( sync != world.SyncHash() )
					throw new InvalidOperationException( "Desync in Controller.ChooseCursor" );
			}
		}

		public void SetModifiers(Modifiers mods) { modifiers = mods; }
		public Modifiers GetModifiers() { return modifiers; }
	}
}
