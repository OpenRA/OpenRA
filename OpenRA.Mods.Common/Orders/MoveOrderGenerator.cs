#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.Orders
{
	// A marker subclass of the default contextual click handler. It does not override any
	// targeting/cursor logic - a plain click through this generator behaves in every way
	// like a normal default click (Attack still wins on an enemy unit, Enter still wins on
	// a garrisonable building, etc).
	//
	// The only purpose of having a distinct type is so that AttackMoveOrderTargeter
	// (see AttackMove.cs) can recognise "we are currently in this mode" and bail out, letting
	// a plain terrain click fall through to Move - exactly as if
	// Game.Settings.Game.AttackMoveIsDefault were turned off, without actually touching that
	// (persisted) setting.
	public class MoveOrderGenerator : UnitOrderGenerator
	{
		public MoveOrderGenerator(World world)
			: base(world) { }

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			// base.OrderInner is a lazily-evaluated iterator (it uses yield internally), so it
			// does not actually run - and does not call AttackMoveOrderTargeter.CanTarget - until
			// the returned sequence is enumerated. It must be materialised here, before
			// CancelInputMode() below replaces world.OrderGenerator, otherwise CanTarget's check
			// for "are we still in this mode" would already see the reverted generator and let
			// attack-move win again.
			var orders = base.OrderInner(world, cell, worldPixel, mi).ToList();

			// Revert to the plain default generator after a single click, unless the player
			// is queueing multiple waypoints (Shift) - matches AttackMoveOrderGenerator.
			if (!mi.Modifiers.HasModifier(Modifiers.Shift))
				world.CancelInputMode();

			return orders;
		}
	}
}
