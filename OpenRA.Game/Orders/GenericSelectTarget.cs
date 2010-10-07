#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Orders
{
	public class GenericSelectTarget : IOrderGenerator
	{
		readonly IEnumerable<Actor> subjects;
		readonly string order;
		readonly string cursor;

		public GenericSelectTarget(IEnumerable<Actor> subjects, string order, string cursor)
		{
			this.subjects = subjects;
			this.order = order;
			this.cursor = cursor;
		}

		public GenericSelectTarget(Actor subject, string order, string cursor)
		{
			this.subjects = new Actor[] { subject };
			this.order = order;
			this.cursor = cursor;
		}

		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();
			return OrderInner(world, xy, mi);
		}

		IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left && world.Map.IsInMap(xy))
			{
				world.CancelInputMode();
				foreach (var subject in subjects)
					yield return new Order(order, subject, xy);
			}
		}

		public virtual void Tick(World world) { }

		public void RenderBeforeWorld(WorldRenderer wr, World world)
		{
			foreach (var a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPreRenderSelection>())
						t.RenderBeforeWorld(wr, a);

			Game.Renderer.Flush();
		}

		public void RenderAfterWorld(WorldRenderer wr, World world)
		{
			foreach (var a in world.Selection.Actors)
				if (!a.Destroyed)
					foreach (var t in a.TraitsImplementing<IPostRenderSelection>())
						t.RenderAfterWorld(wr, a);

			Game.Renderer.Flush();
		}

		public string GetCursor(World world, int2 xy, MouseInput mi) { return world.Map.IsInMap(xy) ? cursor : "generic-blocked"; }
	}

	// variant that requires a tag trait (T) to be present on some actor owned
	// by the activating player
	public class GenericSelectTargetWithBuilding<T> : GenericSelectTarget
	{
		public GenericSelectTargetWithBuilding(Actor subject, string order, string cursor)
			: base(subject, order, cursor) { }

		public override void Tick(World world)
		{
			var hasStructure = world.Queries.OwnedBy[world.LocalPlayer]
					.WithTrait<T>()
					.Any();

			if (!hasStructure)
				world.CancelInputMode();
		}
	}
}
