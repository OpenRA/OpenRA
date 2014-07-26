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
using OpenRA.Graphics;

namespace OpenRA.Orders
{
	public class GenericSelectTarget : IOrderGenerator
	{
		readonly IEnumerable<Actor> subjects;
		readonly string order;
		readonly string cursor;
		readonly MouseButton expectedButton;

		public GenericSelectTarget(IEnumerable<Actor> subjects, string order, string cursor, MouseButton button)
		{
			this.subjects = subjects;
			this.order = order;
			this.cursor = cursor;
			expectedButton = button;
		}

		public GenericSelectTarget(IEnumerable<Actor> subjects, string order, string cursor)
			: this(subjects, order, cursor, MouseButton.Left)
		{

		}

		public GenericSelectTarget(Actor subject, string order, string cursor)
			: this(new Actor[] { subject }, order, cursor)
		{

		}

		public GenericSelectTarget(Actor subject, string order, string cursor, MouseButton button)
			: this(new Actor[] { subject }, order, cursor, button)
		{

		}

		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button != expectedButton)
				world.CancelInputMode();
			return OrderInner(world, xy, mi);
		}

		IEnumerable<Order> OrderInner(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == expectedButton && world.Map.Contains(xy))
			{
				world.CancelInputMode();
				foreach (var subject in subjects)
					yield return new Order(order, subject, false) { TargetLocation = xy };
			}
		}

		public virtual void Tick(World world) { }
		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world) { yield break; }
		public string GetCursor(World world, CPos xy, MouseInput mi) { return world.Map.Contains(xy) ? cursor : "generic-blocked"; }
	}
}
