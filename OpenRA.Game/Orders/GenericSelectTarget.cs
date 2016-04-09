#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Orders
{
	public class GenericSelectTarget : UnitOrderGenerator
	{
		protected readonly IEnumerable<Actor> Subjects;
		protected readonly string OrderName;
		protected readonly string Cursor;
		protected readonly MouseButton ExpectedButton;

		public GenericSelectTarget(IEnumerable<Actor> subjects, string order, string cursor, MouseButton button)
		{
			Subjects = subjects;
			OrderName = order;
			Cursor = cursor;
			ExpectedButton = button;
		}

		public GenericSelectTarget(IEnumerable<Actor> subjects, string order, string cursor)
			: this(subjects, order, cursor, MouseButton.Left) { }

		public GenericSelectTarget(Actor subject, string order, string cursor)
			: this(new Actor[] { subject }, order, cursor) { }

		public GenericSelectTarget(Actor subject, string order, string cursor, MouseButton button)
			: this(new Actor[] { subject }, order, cursor, button) { }

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button != ExpectedButton)
				world.CancelInputMode();
			return OrderInner(world, cell, mi);
		}

		protected virtual IEnumerable<Order> OrderInner(World world, CPos cell, MouseInput mi)
		{
			if (mi.Button == ExpectedButton && world.Map.Contains(cell))
			{
				world.CancelInputMode();
				foreach (var subject in Subjects)
					yield return new Order(OrderName, subject, false) { TargetLocation = cell };
			}
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? Cursor : "generic-blocked";
		}

		public override bool InputOverridesSelection(World world, int2 xy, MouseInput mi)
		{
			// Custom order generators always override selection
			return true;
		}
	}
}
