#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;

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

		public override IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button != ExpectedButton)
				world.CancelInputMode();
			return OrderInner(world, xy, mi);
		}

		protected virtual IEnumerable<Order> OrderInner(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == ExpectedButton && world.Map.Contains(xy))
			{
				world.CancelInputMode();
				foreach (var subject in Subjects)
					yield return new Order(OrderName, subject, false) { TargetLocation = xy };
			}
		}

		public override string GetCursor(World world, CPos xy, MouseInput mi) { return world.Map.Contains(xy) ? Cursor : "generic-blocked"; }
	}
}
