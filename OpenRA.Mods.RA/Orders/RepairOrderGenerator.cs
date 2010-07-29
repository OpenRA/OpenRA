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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
{
	class RepairOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			return OrderInner(world, xy, mi);
		}

		IEnumerable<Order> OrderInner(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var underCursor = world.FindUnitsAtMouse(mi.Location)
					.Where(a => a.Owner == world.LocalPlayer
						&& a.traits.Contains<Building>()
						&& a.traits.Contains<Selectable>()).FirstOrDefault();

				var building = underCursor != null ? underCursor.Info.Traits.Get<BuildingInfo>() : null;
				var health = underCursor != null ? underCursor.traits.GetOrDefault<Health>() : null;
				
				if (building != null && building.Repairable && health != null && health.HPFraction < 1f)
					yield return new Order("Repair", underCursor);
			}
		}

		public void Tick( World world )
		{
			if( !PlayerIsAllowedToRepair( world ) )
				world.CancelInputMode();
		}

		public static bool PlayerIsAllowedToRepair( World world )
		{
			return Game.world.Queries.OwnedBy[ Game.world.LocalPlayer ]
				.WithTrait<Production>().Where( x => x.Actor.Info.Traits.Get<ProductionInfo>().Produces.Contains( "Building" ) )
				.Any();
		}

		public void RenderAfterWorld( World world ) {}
		public void RenderBeforeWorld(World world) { }

		public string GetCursor(World world, int2 xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, xy, mi).Any() 
				? "repair" : "repair-blocked";
		}
	}
}
