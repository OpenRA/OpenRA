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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
{
	public class PlaceBuildingOrderGenerator : IOrderGenerator
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
				world.CancelInputMode();

			var ret = InnerOrder( world, xy, mi ).ToList();
			if (ret.Count > 0)
				world.CancelInputMode();

			return ret;
		}

		IEnumerable<Order> InnerOrder(World world, int2 xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var topLeft = xy - FootprintUtils.AdjustForBuildingSize( BuildingInfo );
				if (!world.CanPlaceBuilding( Building, BuildingInfo, topLeft, null)
					|| !BuildingInfo.IsCloseEnoughToBase(world, Producer.Owner, Building, topLeft))
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
			// Find the queue with the target actor
			//var queue = Producer.TraitsImplementing<ProductionQueue>()
			//	.Where(p => p.CurrentItem() != null && 
		    //             	p.CurrentItem().Item == Building && 
		    //             	p.CurrentItem().RemainingTime == 0)
			//	.FirstOrDefault();
		}

		public void RenderAfterWorld( WorldRenderer wr, World world ) { }

		public void RenderBeforeWorld( WorldRenderer wr, World world )
		{
			BuildingInfo.DrawBuildingGrid( wr, world, Building );
		}

		public string GetCursor(World world, int2 xy, MouseInput mi) { return "default"; }
	}
}
