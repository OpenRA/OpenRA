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
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Orders
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
					var eva = world.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
					Sound.Play(eva.BuildingCannotPlaceAudio);
					yield break;
				}
				
				if (Rules.Info[ Building ].Traits.Contains<LineBuildInfo>())
					yield return new Order("LineBuild", Producer.Owner.PlayerActor, topLeft, Building);
				else
					yield return new Order("PlaceBuilding", Producer.Owner.PlayerActor, topLeft, Building);
			}
		}
		
		public void Tick( World world )
		{
			var producing = Producer.traits.Get<Traits.ProductionQueue>().CurrentItem( Rules.Info[ Building ].Category );
			if (producing == null || producing.Item != Building || producing.RemainingTime != 0)
				Game.controller.CancelInputMode();
		}

		public void RenderAfterWorld( World world ) {}

		public void RenderBeforeWorld(World world)
		{
			world.WorldRenderer.uiOverlay.DrawBuildingGrid(world, Building, BuildingInfo);
		}

		public string GetCursor(World world, int2 xy, MouseInput mi) { return "default"; }
	}
}
