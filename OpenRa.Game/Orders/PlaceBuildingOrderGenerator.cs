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

using System.Collections.Generic;
using OpenRa.GameRules;
using OpenRa.Traits;

namespace OpenRa.Orders
{
	class PlaceBuildingOrderGenerator : IOrderGenerator
	{
		readonly Actor Producer;
		readonly string Building;
		BuildingInfo BuildingInfo { get { return Rules.Info[ Building ].Traits.Get<BuildingInfo>(); } }

		public PlaceBuildingOrderGenerator(Actor producer, string name)
		{
			Producer = producer;
			Building = name;
		}

		public IEnumerable<Order> Order(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				Game.controller.CancelInputMode();

			return InnerOrder(world, xy, mi);
		}

		IEnumerable<Order> InnerOrder(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var topLeft = xy - Footprint.AdjustForBuildingSize( BuildingInfo );
				if (!world.CanPlaceBuilding( Building, BuildingInfo, topLeft, null)
					|| !world.IsCloseEnoughToBase(Producer.Owner, Building, BuildingInfo, topLeft))
				{
					var queueInfo = world.LocalPlayer.PlayerActor.Info.Traits.Get<ProductionQueueInfo>();
					Sound.Play(queueInfo.BuildingCannotPlaceAudio);
					yield break;
				}

				yield return new Order("PlaceBuilding", Producer.Owner.PlayerActor, topLeft, Building);
			}
		}

		public void Tick( World world )
		{
			var producing = Producer.traits.Get<Traits.ProductionQueue>().CurrentItem( Rules.Info[ Building ].Category );
			if (producing == null || producing.Item != Building || producing.RemainingTime != 0)
				Game.controller.CancelInputMode();
		}

		public void Render( World world )
		{
			world.WorldRenderer.uiOverlay.DrawBuildingGrid( world, Building, BuildingInfo );
		}

		public string GetCursor(World world, int2 xy, MouseInput mi) { return "default"; }
	}
}
